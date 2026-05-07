using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using JerrettDavis.Flawright.Input;

namespace JerrettDavis.Flawright;

/// <summary>
/// Represents a window or form within a desktop application.
/// Obtain instances via <see cref="IFlawrightBrowser.NewPageAsync"/> or
/// <see cref="IFlawrightBrowser.GetAllPagesAsync"/>.
/// </summary>
/// <example>
/// <code>
/// var page = await fw.Browser.NewPageAsync();
/// await page.FillAsync("controltype:Edit", "hello world");
/// await page.PressAsync("controltype:Edit", "Ctrl+S");
/// var title = await page.TitleAsync();
/// </code>
/// </example>
public sealed class FlawrightPage : IFlawrightPage
{
    private readonly Window _window;
    private readonly UIA3Automation _automation;
    private readonly FlawrightOptions _options;

    internal FlawrightPage(Window window, UIA3Automation automation, FlawrightOptions options)
    {
        _window = window;
        _automation = automation;
        _options = options;
    }

    // ── IFlawrightPage ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<string> TitleAsync(CancellationToken ct = default)
        => Task.Run(() => _window.Title ?? string.Empty, ct);

    /// <inheritdoc/>
    public IFlawrightLocator Locator(string selector)
    {
        ArgumentException.ThrowIfNullOrEmpty(selector);
        return new FlawrightLocator(selector, _window, _automation, _options);
    }

    /// <inheritdoc/>
    public async Task ClickAsync(
        string selector,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var element = await WaitForSelectorAsync(selector, timeout, ct).ConfigureAwait(false);
        await element.ClickAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task FillAsync(
        string selector,
        string text,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var element = await WaitForSelectorAsync(selector, timeout, ct).ConfigureAwait(false);
        await element.FillAsync(text, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Focuses the element, then types each character via
    /// <see cref="Keyboard.Type(string)"/>.  This simulates realistic
    /// key-by-key input, which is useful for controls that react to key events.
    /// Use <see cref="FillAsync"/> for fast value-setting via ValuePattern.
    /// </remarks>
    public async Task TypeAsync(
        string selector,
        string text,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var element = await WaitForSelectorAsync(selector, timeout, ct).ConfigureAwait(false);
        await element.FocusAsync(ct).ConfigureAwait(false);
        await Task.Run(() => Keyboard.Type(text), ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Focuses the element then dispatches the key via <see cref="KeyParser"/>.
    /// Supported syntax: single key names (<c>"Enter"</c>, <c>"Escape"</c>) and
    /// modifier chords (<c>"Ctrl+S"</c>, <c>"Ctrl+Shift+Z"</c>).
    /// </remarks>
    public async Task PressAsync(
        string selector,
        string key,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var element = await WaitForSelectorAsync(selector, timeout, ct).ConfigureAwait(false);
        await element.FocusAsync(ct).ConfigureAwait(false);
        await Task.Run(() => KeyParser.Send(key), ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses <c>TogglePattern</c> to set the element to the <c>On</c> state.
    /// If the element is already checked, no action is taken.
    /// </remarks>
    public async Task CheckAsync(
        string selector,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var element = await WaitForSelectorAsync(selector, timeout, ct).ConfigureAwait(false);
        var fe = (FlawrightElement)element;
        await Task.Run(() =>
        {
            var tp = fe.AutomationElement.Patterns.Toggle;
            if (!tp.IsSupported)
                throw new InvalidOperationException(
                    $"Element '{selector}' does not support TogglePattern.");
            if (tp.Pattern.ToggleState.Value != ToggleState.On)
                tp.Pattern.Toggle();
        }, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses <c>TogglePattern</c> to set the element to the <c>Off</c> state.
    /// If the element is already unchecked, no action is taken.
    /// </remarks>
    public async Task UncheckAsync(
        string selector,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var element = await WaitForSelectorAsync(selector, timeout, ct).ConfigureAwait(false);
        var fe = (FlawrightElement)element;
        await Task.Run(() =>
        {
            var tp = fe.AutomationElement.Patterns.Toggle;
            if (!tp.IsSupported)
                throw new InvalidOperationException(
                    $"Element '{selector}' does not support TogglePattern.");
            if (tp.Pattern.ToggleState.Value != ToggleState.Off)
                tp.Pattern.Toggle();
        }, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Finds child items within the container element (combobox or listbox)
    /// matching <paramref name="value"/> by name or automation ID, then calls
    /// <c>SelectionItemPattern.Select()</c>.
    /// </remarks>
    public async Task SelectOptionAsync(
        string selector,
        string value,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var container = await WaitForSelectorAsync(selector, timeout, ct).ConfigureAwait(false);
        var fe = (FlawrightElement)container;

        await Task.Run(() =>
        {
            var children = fe.AutomationElement.FindAllDescendants();
            var target = children.FirstOrDefault(
                c => string.Equals(c.Name, value, StringComparison.OrdinalIgnoreCase)
                  || string.Equals(c.AutomationId, value, StringComparison.OrdinalIgnoreCase));

            if (target == null)
                throw new InvalidOperationException(
                    $"Option '{value}' not found in '{selector}'.");

            var sip = target.Patterns.SelectionItem;
            if (!sip.IsSupported)
                throw new InvalidOperationException(
                    $"Option '{value}' does not support SelectionItemPattern.");

            sip.Pattern.Select();
        }, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IFlawrightElement> WaitForSelectorAsync(
        string selector,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        return await Locator(selector).FirstAsync(timeout, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses <see cref="Capture.Element"/> from <c>FlaUI.Core.Capturing</c>,
    /// which internally uses GDI BitBlt.  The captured bitmap is encoded as PNG
    /// via <see cref="System.Drawing.Imaging.ImageFormat.Png"/>.
    /// </remarks>
    public async Task<byte[]> ScreenshotAsync(
        string? path = null,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var capture = Capture.Element(_window);
            var bitmap = capture.Bitmap;

            string? savePath = path;
            if (savePath == null && _options.ScreenshotDirectory != null)
            {
                var fileName = $"screenshot-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.png";
                savePath = System.IO.Path.Combine(_options.ScreenshotDirectory, fileName);
            }

            if (savePath != null)
            {
                capture.ToFile(savePath);
            }

            using var ms = new System.IO.MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }, ct).ConfigureAwait(false);
    }

    // ── IAsyncDisposable ─────────────────────────────────────────────────────

    /// <summary>
    /// Releases the page.  The underlying window object does not require
    /// explicit disposal — the owning <see cref="FlawrightBrowser"/> manages
    /// the application lifecycle.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        // The underlying window object is managed by FlawrightBrowser.
        // Nothing to release here; method exists for IAsyncDisposable compliance.
        return ValueTask.CompletedTask;
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    /// <summary>Gets the underlying FlaUI window element.</summary>
    internal Window Window => _window;

    /// <summary>Gets the underlying UIA3 automation instance.</summary>
    internal UIA3Automation Automation => _automation;
}
