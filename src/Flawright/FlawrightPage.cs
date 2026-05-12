using Flawright.Backends;
using Flawright.Locator;
using Flawright.Page;
using Flawright.Selectors;

namespace Flawright;

/// <summary>
/// Represents a window or form within a desktop application.
/// Obtain instances via <see cref="IFlawrightBrowser.NewPageAsync"/> or
/// <see cref="IFlawrightBrowser.GetAllPagesAsync"/>.
/// </summary>
/// <remarks>
/// Wave D.2 rewrite: all methods delegate to <see cref="FlawrightLocator"/> via
/// <see cref="Locator(string)"/>.  The page accepts an <see cref="IConditionTranslator"/>
/// so tests can inject a <c>FakeConditionTranslator</c> without any FlaUI dependency.
/// </remarks>
/// <example>
/// <code>
/// var page = await fw.Browser.NewPageAsync();
/// await page.FillAsync("controltype:Edit", "hello world");
/// await page.PressAsync("controltype:Edit", "Ctrl+S");
/// var title = await page.TitleAsync();
/// </code>
/// </example>
internal sealed class FlawrightPage : IFlawrightPage
{
    private readonly IElementBackend _windowBackend;
    private readonly IInputBackend _input;
    private readonly IConditionTranslator _translator;
    private readonly Lazy<IFlawrightMouse> _mouse;
    private readonly Lazy<IFlawrightKeyboard> _keyboard;

    // Browser reference is optional: present when this page was created by
    // FlawrightBrowser, null when constructed directly in tests.
    private readonly FlawrightBrowser? _browser;
    private readonly IApplicationHandle? _app;

    // Deduplicates DialogOpened events across repeated calls to GetOwnedWindowsAsync, GetModalWindowsAsync, and WaitForDialogAsync.
    private readonly HashSet<nint> _raisedDialogHandles = new();

    internal FlawrightPage(
        IElementBackend windowBackend,
        IInputBackend input,
        FlawrightOptions options,
        IConditionTranslator translator,
        FlawrightBrowser? browser = null,
        IApplicationHandle? app = null)
    {
        _windowBackend = windowBackend;
        _input = input;
        Options = options;
        _translator = translator;
        _browser = browser;
        _app = app;
        _mouse = new Lazy<IFlawrightMouse>(() => new FlawrightMouse(input));
        _keyboard = new Lazy<IFlawrightKeyboard>(() => new FlawrightKeyboard(input));
    }

    // ── IFlawrightPage: Identity ──────────────────────────────────────────────

    /// <inheritdoc/>
    public FlawrightOptions Options { get; }

    /// <inheritdoc/>
    public Task<string> TitleAsync(CancellationToken ct = default)
        => Task.FromResult(_windowBackend.Name ?? string.Empty);

    /// <inheritdoc/>
    public Task BringToFrontAsync(CancellationToken ct = default)
    {
        // UIA has no direct "bring to front" API; focus the root element instead.
        _windowBackend.Focus();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task WaitForTimeoutAsync(double milliseconds, CancellationToken ct = default)
        => Task.Delay(TimeSpan.FromMilliseconds(milliseconds), ct);

    // ── IFlawrightPage: Owned-window / dialog discovery ──────────────────────

    /// <inheritdoc/>
    public Task<IReadOnlyList<IFlawrightPage>> GetOwnedWindowsAsync(CancellationToken ct = default)
    {
        if (_app == null)
            return Task.FromResult<IReadOnlyList<IFlawrightPage>>(Array.Empty<IFlawrightPage>());

        var ownerHwnd = _windowBackend.NativeWindowHandle;
        var ownedWindows = _app.GetOwnedWindows(ownerHwnd);
        var pages = new List<IFlawrightPage>(ownedWindows.Count);
        var seenHandles = new HashSet<nint>();

        foreach (var backend in ownedWindows)
        {
            var dialogHwnd = backend.NativeWindowHandle;
            if (!seenHandles.Add(dialogHwnd))
                continue;

            pages.Add(new FlawrightPage(backend, _input, Options, _translator, _browser, _app));

            // Always raise DialogOpened for each new dialog discovered, regardless of EnableWindowEvents.
            // EnableWindowEvents gates only the noisy WindowDetected firehose, not this focused event.
            if (_browser != null && _raisedDialogHandles.Add(dialogHwnd))
            {
                _browser.RaiseDialogOpened(new DialogOpenedEventArgs(
                    parentProcessId: _app.ProcessId,
                    parentWindowHandle: ownerHwnd,
                    dialogWindowHandle: dialogHwnd,
                    dialogTitle: backend.Name,
                    isModal: false));
            }
        }

        return Task.FromResult<IReadOnlyList<IFlawrightPage>>(pages.AsReadOnly());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<IFlawrightPage>> GetModalWindowsAsync(CancellationToken ct = default)
    {
        var modals = _windowBackend.GetModalWindows();
        if (modals.Count == 0)
            return Task.FromResult<IReadOnlyList<IFlawrightPage>>(Array.Empty<IFlawrightPage>());

        var pages = new List<IFlawrightPage>(modals.Count);
        var ownerHwnd = _windowBackend.NativeWindowHandle;

        foreach (var modal in modals)
        {
            pages.Add(new FlawrightPage(modal, _input, Options, _translator, _browser, _app));

            // Raise DialogOpened for each modal window, with IsModal = true.
            if (_browser != null && _app != null)
            {
                var modalHwnd = modal.NativeWindowHandle;
                if (_raisedDialogHandles.Add(modalHwnd))
                {
                    _browser.RaiseDialogOpened(new DialogOpenedEventArgs(
                        parentProcessId: _app.ProcessId,
                        parentWindowHandle: ownerHwnd,
                        dialogWindowHandle: modalHwnd,
                        dialogTitle: modal.Name,
                        isModal: true));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<IFlawrightPage>>(pages.AsReadOnly());
    }

    /// <inheritdoc/>
    public async Task<IFlawrightPage> WaitForDialogAsync(
        string? titlePattern = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        if (_app == null)
            throw new FlawrightTimeoutException(
                $"dialog (titlePattern='{titlePattern ?? "*"}')",
                timeout ?? Options.DefaultTimeout);

        var ownerHwnd = _windowBackend.NativeWindowHandle;
        var effectiveTimeout = timeout ?? Options.DefaultTimeout;

        var matchedBackend = await Internals.AutoWait.UntilAsync(
            _ =>
            {
                var ownedWindows = _app.GetOwnedWindows(ownerHwnd);
                var match = ownedWindows.FirstOrDefault(w =>
                    titlePattern == null ||
                    w.Name?.Contains(titlePattern, StringComparison.OrdinalIgnoreCase) == true);
                return Task.FromResult<IElementBackend?>(match);
            },
            $"dialog (titlePattern='{titlePattern ?? "*"}')",
            effectiveTimeout,
            Options.DefaultRetryInterval,
            ct).ConfigureAwait(false);

        var dialogHwnd = matchedBackend.NativeWindowHandle;
        if (_browser != null && _raisedDialogHandles.Add(dialogHwnd))
        {
            _browser.RaiseDialogOpened(new DialogOpenedEventArgs(
                parentProcessId: _app.ProcessId,
                parentWindowHandle: ownerHwnd,
                dialogWindowHandle: dialogHwnd,
                dialogTitle: matchedBackend.Name,
                isModal: false));
        }

        return new FlawrightPage(matchedBackend, _input, Options, _translator, _browser, _app);
    }

    // ── IFlawrightPage: Sub-APIs ──────────────────────────────────────────────

    /// <inheritdoc/>
    public IFlawrightMouse Mouse => _mouse.Value;

    /// <inheritdoc/>
    public IFlawrightKeyboard Keyboard => _keyboard.Value;

    // ── IFlawrightPage: Locator factory ───────────────────────────────────────

    /// <inheritdoc/>
    public IFlawrightLocator Locator(string selector)
    {
        ArgumentException.ThrowIfNullOrEmpty(selector);
        var ast = SelectorParser.Parse(selector);
        var pipeline = _translator.Translate(ast);
        var ctx = new LocatorContext
        {
            Root = _windowBackend,
            Input = _input,
            InputMode = Options.InputMode,
            Translator = _translator,
            Selector = selector,
            Pipeline = pipeline,
            Options = Options,
        };
        return new FlawrightLocator(ctx);
    }

    /// <inheritdoc/>
    public IFlawrightLocator GetByRole(AriaRole role, LocatorGetByRoleOptions? options = null)
        => RootLocator().GetByRole(role, options);

    /// <inheritdoc/>
    public IFlawrightLocator GetByLabel(string text, LocatorGetByLabelOptions? options = null)
        => RootLocator().GetByLabel(text, options);

    /// <inheritdoc/>
    public IFlawrightLocator GetByText(string text, LocatorGetByTextOptions? options = null)
        => RootLocator().GetByText(text, options);

    /// <inheritdoc/>
    public IFlawrightLocator GetByTestId(string testId)
        => RootLocator().GetByTestId(testId);

    /// <inheritdoc/>
    public IFlawrightLocator GetByPlaceholder(string text, LocatorGetByPlaceholderOptions? options = null)
        => RootLocator().GetByPlaceholder(text, options);

    /// <inheritdoc/>
    public IFlawrightLocator GetByTitle(string text, LocatorGetByTitleOptions? options = null)
        => RootLocator().GetByTitle(text, options);

    // ── IFlawrightPage: Convenience action methods ────────────────────────────

    /// <inheritdoc/>
    public async Task ClickAsync(string selector, LocatorClickOptions? options = null, CancellationToken ct = default)
        => await Locator(selector).ClickAsync(options, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task DoubleClickAsync(string selector, LocatorDoubleClickOptions? options = null, CancellationToken ct = default)
        => await Locator(selector).DoubleClickAsync(options, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task FillAsync(string selector, string value, LocatorFillOptions? options = null, CancellationToken ct = default)
        => await Locator(selector).FillAsync(value, options, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task TypeAsync(string selector, string text, LocatorTypeOptions? options = null, CancellationToken ct = default)
        => await Locator(selector).TypeAsync(text, options, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task PressAsync(string selector, string key, LocatorPressOptions? options = null, CancellationToken ct = default)
        => await Locator(selector).PressAsync(key, options, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task CheckAsync(string selector, LocatorCheckOptions? options = null, CancellationToken ct = default)
        => await Locator(selector).CheckAsync(options, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task UncheckAsync(string selector, LocatorUncheckOptions? options = null, CancellationToken ct = default)
        => await Locator(selector).UncheckAsync(options, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task SetCheckedAsync(string selector, bool @checked, LocatorSetCheckedOptions? options = null, CancellationToken ct = default)
        => await Locator(selector).SetCheckedAsync(@checked, options, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task SelectOptionAsync(string selector, string value, LocatorSelectOptionOptions? options = null, CancellationToken ct = default)
        => await Locator(selector).SelectOptionAsync(value, options, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task HoverAsync(string selector, LocatorHoverOptions? options = null, CancellationToken ct = default)
        => await Locator(selector).HoverAsync(options, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task FocusAsync(string selector, CancellationToken ct = default)
        => await Locator(selector).FocusAsync(ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task DragAndDropAsync(string source, string target, LocatorDragToOptions? options = null, CancellationToken ct = default)
        => await Locator(source).DragToAsync(Locator(target), options, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IFlawrightElement> WaitForSelectorAsync(string selector, LocatorWaitForOptions? options = null, CancellationToken ct = default)
    {
        await Locator(selector).WaitForAsync(options, ct).ConfigureAwait(false);
#pragma warning disable CS0618
        return await Locator(selector).ElementHandleAsync(options?.Timeout, ct).ConfigureAwait(false);
#pragma warning restore CS0618
    }

    // ── IFlawrightPage: Screenshot ────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Captures the window using <c>PrintWindow</c> with <c>PW_RENDERFULLCONTENT</c>
    /// so that DirectComposition / WinUI content is included.  This approach works on
    /// Windows Server CI runners where a plain GDI <c>BitBlt</c> from the screen
    /// returns a blank bitmap because the desktop session is not composited.
    /// </remarks>
    public async Task<byte[]> ScreenshotAsync(LocatorScreenshotOptions? options = null, CancellationToken ct = default)
    {
        var bytes = _windowBackend.CaptureScreenshot();

        var path = ResolveScreenshotPath(options?.Path, Options.ScreenshotDirectory, options?.Type ?? ScreenshotType.Png);
        if (path != null)
        {
            var directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                System.IO.Directory.CreateDirectory(directory);
            await System.IO.File.WriteAllBytesAsync(path, bytes, ct).ConfigureAwait(false);
        }

        return bytes;
    }

    /// <summary>
    /// Resolves the on-disk path a screenshot should be written to.
    /// </summary>
    /// <param name="explicitPath">Path supplied by the caller via options. When set, used verbatim.</param>
    /// <param name="directory">Configured <see cref="FlawrightOptions.ScreenshotDirectory"/>.</param>
    /// <param name="type">Image format used to derive the file extension when generating a filename.</param>
    /// <returns>
    /// The path to write to, or <see langword="null"/> when neither an explicit path
    /// nor a directory is configured (in-memory only).
    /// </returns>
    internal static string? ResolveScreenshotPath(string? explicitPath, string? directory, ScreenshotType type)
    {
        if (!string.IsNullOrEmpty(explicitPath))
            return explicitPath;
        if (string.IsNullOrEmpty(directory))
            return null;

        var ext = type == ScreenshotType.Jpeg ? "jpg" : "png";
        var fileName = $"screenshot-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.{ext}";
        return System.IO.Path.Combine(directory, fileName);
    }

    /// <inheritdoc/>
    public Task<byte[]> ScreenshotAsync(string path, CancellationToken ct = default)
        => ScreenshotAsync(new LocatorScreenshotOptions { Path = path }, ct);

    // ── IAsyncDisposable ──────────────────────────────────────────────────────

    /// <summary>
    /// Releases the page.  The underlying window object does not require
    /// explicit disposal — the owning <see cref="FlawrightBrowser"/> manages
    /// the application lifecycle.
    /// </summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="FlawrightLocator"/> rooted at the window backend with
    /// an empty pipeline (match-all from root). Used as the base for GetBy* methods.
    /// </summary>
    private FlawrightLocator RootLocator()
    {
        var ctx = new LocatorContext
        {
            Root = _windowBackend,
            Input = _input,
            InputMode = Options.InputMode,
            Translator = _translator,
            Selector = string.Empty,
            Pipeline = new SelectorPipeline(Array.Empty<IElementCondition>()),
            Options = Options,
        };
        return new FlawrightLocator(ctx);
    }
}
