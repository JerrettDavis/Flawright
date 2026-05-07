using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;

namespace JerrettDavis.Flawright;

/// <summary>
/// A resolved UI element with async action methods.  Instances are produced
/// by <see cref="IFlawrightLocator.FirstAsync"/>,
/// <see cref="IFlawrightLocator.NthAsync"/>, and
/// <see cref="IFlawrightLocator.AllAsync"/>.
/// </summary>
/// <example>
/// <code>
/// var element = await page.Locator("#editor").FirstAsync();
/// await element.FillAsync("hello world");
/// var text = await element.TextAsync();
/// </code>
/// </example>
public sealed class FlawrightElement : IFlawrightElement
{
    private readonly AutomationElement _element;

    internal FlawrightElement(AutomationElement element, IFlawrightLocator locator)
    {
        _element = element;
        Locator = locator;
    }

    /// <inheritdoc/>
    public IFlawrightLocator Locator { get; }

    // ── Actions ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task ClickAsync(CancellationToken ct = default)
        => Task.Run(() => _element.Click(), ct);

    /// <inheritdoc/>
    public Task DoubleClickAsync(CancellationToken ct = default)
        => Task.Run(() => _element.DoubleClick(), ct);

    /// <inheritdoc/>
    /// <remarks>
    /// Sets the element value in one shot via <c>ValuePattern.SetValue</c>.
    /// If the element does not support <c>ValuePattern</c>, the method falls
    /// back to treating the element as a text-box via <c>AsTextBox().Text</c>.
    /// </remarks>
    public Task FillAsync(string text, CancellationToken ct = default)
        => Task.Run(() =>
        {
            // Try ValuePattern first (most edit controls)
            var vp = _element.Patterns.Value;
            if (vp.IsSupported)
            {
                vp.Pattern.SetValue(text);
                return;
            }

            // Fall back to TextBox abstraction
            var tb = _element.AsTextBox();
            if (tb != null)
            {
                tb.Text = text;
                return;
            }

            throw new InvalidOperationException(
                $"Element '{_element.AutomationId ?? _element.Name}' does not support filling.");
        }, ct);

    /// <inheritdoc/>
    /// <remarks>
    /// Text resolution order:
    /// <list type="number">
    ///   <item><description>
    ///     <c>ValuePattern.Value</c> — for edit/input controls.
    ///   </description></item>
    ///   <item><description>
    ///     <c>TextPattern.DocumentRange.GetText(-1)</c> — for document and
    ///     rich-text controls.
    ///   </description></item>
    ///   <item><description>
    ///     <c>AutomationElement.Name</c> — fallback for labels, buttons, etc.
    ///   </description></item>
    /// </list>
    /// </remarks>
    public Task<string> TextAsync(CancellationToken ct = default)
        => Task.Run(() =>
        {
            // 1. ValuePattern (edit controls, text-boxes)
            var vp = _element.Patterns.Value;
            if (vp.IsSupported)
                return vp.Pattern.Value.Value ?? string.Empty;

            // 2. TextPattern (documents, rich-text)
            var tp = _element.Patterns.Text;
            if (tp.IsSupported)
            {
                try
                {
                    return tp.Pattern.DocumentRange.GetText(-1) ?? string.Empty;
                }
#pragma warning disable CA1031 // Fall through to Name fallback if TextPattern fails
                catch (Exception)
#pragma warning restore CA1031
                {
                    // fall through to Name
                }
            }

            // 3. Name property (labels, buttons, static text)
            return _element.Name ?? string.Empty;
        }, ct);

    /// <inheritdoc/>
    public Task<bool> IsVisibleAsync(CancellationToken ct = default)
        => Task.Run(() => !_element.IsOffscreen, ct);

    /// <inheritdoc/>
    public Task<bool> IsEnabledAsync(CancellationToken ct = default)
        => Task.Run(() => _element.IsEnabled, ct);

    /// <inheritdoc/>
    /// <remarks>
    /// Uses <c>TogglePattern.ToggleState</c>.  Returns <see langword="false"/>
    /// if the element does not support <c>TogglePattern</c>.
    /// </remarks>
    public Task<bool> IsCheckedAsync(CancellationToken ct = default)
        => Task.Run(() =>
        {
            var tp = _element.Patterns.Toggle;
            if (!tp.IsSupported)
                return false;
            return tp.Pattern.ToggleState.Value == ToggleState.On;
        }, ct);

    /// <inheritdoc/>
    public Task HoverAsync(CancellationToken ct = default)
        => Task.Run(() =>
        {
            var rect = _element.BoundingRectangle;
            var cx = rect.Left + rect.Width / 2;
            var cy = rect.Top + rect.Height / 2;
            Mouse.MoveTo(cx, cy);
        }, ct);

    /// <inheritdoc/>
    public Task FocusAsync(CancellationToken ct = default)
        => Task.Run(() => _element.Focus(), ct);

    /// <inheritdoc/>
    /// <remarks>
    /// Uses <c>ScrollItemPattern.ScrollIntoView</c> if available.
    /// No exception is thrown when the pattern is not supported.
    /// </remarks>
    public Task ScrollIntoViewIfNeededAsync(CancellationToken ct = default)
        => Task.Run(() =>
        {
            var sp = _element.Patterns.ScrollItem;
            if (sp.IsSupported)
                sp.Pattern.ScrollIntoView();
        }, ct);

    /// <inheritdoc/>
    /// <remarks>
    /// Supported attribute names: <c>"AutomationId"</c>, <c>"Name"</c>,
    /// <c>"ClassName"</c>, <c>"ControlType"</c>, <c>"Value"</c>.
    /// Any other name returns <see langword="null"/>.
    /// </remarks>
    public Task<string?> GetAttributeAsync(string name, CancellationToken ct = default)
        => Task.Run<string?>(() =>
        {
            return name.ToUpperInvariant() switch
            {
                "AUTOMATIONID" => _element.AutomationId,
                "NAME" => _element.Name,
                "CLASSNAME" => _element.ClassName,
                "CONTROLTYPE" => _element.ControlType.ToString(),
                "VALUE" => GetValueSafe(),
                _ => null
            };
        }, ct);

    // ── Private helpers ──────────────────────────────────────────────────────

    private string? GetValueSafe()
    {
        try
        {
            var vp = _element.Patterns.Value;
            return vp.IsSupported ? vp.Pattern.Value.Value : null;
        }
#pragma warning disable CA1031 // Return null for any error reading value attribute
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    /// <summary>
    /// Exposes the underlying FlaUI <see cref="AutomationElement"/> for
    /// advanced scenarios.  Use sparingly; prefer the typed API.
    /// </summary>
    internal AutomationElement AutomationElement => _element;
}
