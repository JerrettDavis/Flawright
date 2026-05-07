using JerrettDavis.Flawright.Backends;
using JerrettDavis.Flawright.Locator;
using JerrettDavis.Flawright.Page;
using JerrettDavis.Flawright.Selectors;

namespace JerrettDavis.Flawright;

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

    internal FlawrightPage(
        IElementBackend windowBackend,
        IInputBackend input,
        FlawrightOptions options,
        IConditionTranslator translator)
    {
        _windowBackend = windowBackend;
        _input = input;
        Options = options;
        _translator = translator;
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
    /// Stub in Wave D.2. Returns an empty byte array. A real implementation
    /// would capture the window via GDI BitBlt.
    /// </remarks>
    public Task<byte[]> ScreenshotAsync(LocatorScreenshotOptions? options = null, CancellationToken ct = default)
    {
        if (options?.Path != null)
        {
            System.IO.File.WriteAllBytes(options.Path, []);
        }
        return Task.FromResult(Array.Empty<byte>());
    }

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
            Translator = _translator,
            Selector = string.Empty,
            Pipeline = new SelectorPipeline(Array.Empty<IElementCondition>()),
            Options = Options,
        };
        return new FlawrightLocator(ctx);
    }
}
