using System.Drawing;
using JerrettDavis.Flawright.Backends;
using JerrettDavis.Flawright.Backends.Uia;
using JerrettDavis.Flawright.Locator;

namespace JerrettDavis.Flawright;

/// <summary>
/// A resolved UI element with async action methods.  Instances are produced by
/// <see cref="IFlawrightLocator.First"/>,
/// <see cref="IFlawrightLocator.Nth"/>, and
/// <see cref="IFlawrightLocator.AllAsync"/>.
///
/// <para>
/// All platform operations are delegated to <see cref="IElementBackend"/> so the
/// class is fully testable without a live UIA tree.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var element = await page.Locator("#editor").First.ElementHandleAsync();
/// await element.FillAsync("hello world");
/// var text = await element.InnerTextAsync();
/// </code>
/// </example>
internal sealed class FlawrightElement : IFlawrightElement
{
    private readonly IElementBackend _backend;
    private readonly IInputBackend _input;

    // ── Constructors ──────────────────────────────────────────────────────────

    /// <summary>
    /// New constructor — used by Wave C locator and all unit tests.
    /// </summary>
    internal FlawrightElement(IElementBackend backend, IInputBackend input)
    {
        _backend = backend;
        _input = input;
    }

    /// <summary>
    /// Legacy constructor — kept so the existing <see cref="FlawrightLocator"/>
    /// still compiles until Wave C rewrites it.  REMOVE in Wave C.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Legacy shim")]
    internal FlawrightElement(FlaUI.Core.AutomationElements.AutomationElement element, IFlawrightLocator locator)
        : this(new UiaElementBackend(element), new FlaUiInputBackend())
    {
        // Stash the locator for the legacy Locator property.
        _legacyLocator = locator;
    }

    // Populated only when the legacy constructor is used.
    private readonly IFlawrightLocator? _legacyLocator;

    // ── Internal backend access (used by FlawrightLocator.DragToAsync) ────────

    /// <summary>
    /// Exposes the underlying backend for internal callers (e.g. DragToAsync).
    /// </summary>
    internal IElementBackend InternalBackend => _backend;

    // ── Legacy internal property — REMOVE in Wave C ──────────────────────────

    /// <summary>
    /// Exposes the underlying FlaUI <see cref="FlaUI.Core.AutomationElements.AutomationElement"/> for
    /// callers that still depend on the raw element (FlawrightAssertions, FlawrightPage).
    /// <b>REMOVE in Wave C</b> when those callers are updated to use IElementBackend.
    /// </summary>
    internal FlaUI.Core.AutomationElements.AutomationElement AutomationElement
    {
        get
        {
            if (_backend is UiaElementBackend uia)
                return uia.Element;

            throw new InvalidOperationException(
                "AutomationElement is only accessible when backed by UiaElementBackend.  " +
                "This element uses a fake or custom backend.");
        }
    }

    // ── IFlawrightElement: Legacy surface — explicit implementations (REMOVE in Wave C) ─────────────────
    //
    // Using explicit interface implementation prevents C# overload-resolution ambiguity between
    // e.g. ClickAsync(CancellationToken) and ClickAsync(LocatorClickOptions?, CancellationToken).
    // FlawrightLocator and FlawrightAssertions use IFlawrightElement references so they still
    // resolve these correctly.

    /// <inheritdoc/>
    IFlawrightLocator IFlawrightElement.Locator =>
        _legacyLocator
            ?? throw new InvalidOperationException(
                "This FlawrightElement was created via the new IElementBackend constructor " +
                "and does not have an associated locator.  " +
                "Use IFlawrightLocator.ElementHandleAsync() for handle-based access.");

    /// <inheritdoc/>
    Task<string> IFlawrightElement.TextAsync(CancellationToken ct)
        => InnerTextAsync(ct);

    // ── IFlawrightElement: Identity properties ────────────────────────────────

    /// <inheritdoc/>
    public string? AutomationId => _backend.AutomationId;

    /// <inheritdoc/>
    public string? Name => _backend.Name;

    /// <inheritdoc/>
    public string? ClassName => _backend.ClassName;

    /// <inheritdoc/>
    public string ControlTypeName => _backend.ControlTypeName;

    // ── IFlawrightElement: Read methods ───────────────────────────────────────

    /// <inheritdoc/>
    public Task<BoundingBox?> BoundingBoxAsync(CancellationToken ct = default)
    {
        var rect = _backend.BoundingRectangle;
        if (rect == Rectangle.Empty || (rect.Width == 0 && rect.Height == 0))
            return Task.FromResult<BoundingBox?>(null);

        return Task.FromResult<BoundingBox?>(
            new BoundingBox(rect.X, rect.Y, rect.Width, rect.Height));
    }

    /// <inheritdoc/>
    public Task<string> InnerTextAsync(CancellationToken ct = default)
    {
        // Resolution order: ValuePattern → TextPattern → Name
        var value = _backend.TryGetValue();
        if (value != null)
            return Task.FromResult(value);

        var doc = _backend.TryGetDocumentText();
        if (doc != null)
            return Task.FromResult(doc);

        return Task.FromResult(_backend.Name ?? string.Empty);
    }

    /// <inheritdoc/>
    public Task<string?> TextContentAsync(CancellationToken ct = default)
    {
        // Same resolution order as InnerTextAsync, but returns null instead of empty.
        var value = _backend.TryGetValue();
        if (value != null)
            return Task.FromResult<string?>(value);

        var doc = _backend.TryGetDocumentText();
        if (doc != null)
            return Task.FromResult<string?>(doc);

        return Task.FromResult(_backend.Name);
    }

    /// <inheritdoc/>
    public Task<string?> InputValueAsync(CancellationToken ct = default)
    {
        var value = _backend.TryGetValue();
        if (value != null)
            return Task.FromResult<string?>(value);

        // Check TextPattern as a secondary signal that this is a text control
        var doc = _backend.TryGetDocumentText();
        if (doc != null)
            return Task.FromResult<string?>(doc);

        throw new InvalidOperationException(
            "InputValue is only supported on elements that implement ValuePattern or TextPattern. " +
            "This element appears to be neither an input, textarea, nor select control.");
    }

    /// <inheritdoc/>
    public Task<string?> GetAttributeAsync(string name, CancellationToken ct = default)
    {
        var result = (name?.ToUpperInvariant() ?? string.Empty) switch
        {
            "ID" or "AUTOMATIONID" or "DATA-TESTID"
                => _backend.AutomationId,

            "NAME" or "ARIA-LABEL"
                => _backend.Name,

            "CLASS" or "CLASSNAME"
                => _backend.ClassName,

            "CONTROLTYPE" or "ROLE"
                => _backend.ControlTypeName,

            "VALUE"
                => _backend.TryGetValue(),

            "ENABLED"
                => _backend.IsEnabled ? "true" : "false",

            _ => null
        };

        return Task.FromResult(result);
    }

    // ── IFlawrightElement: State methods ──────────────────────────────────────

    /// <inheritdoc/>
    public Task<bool> IsVisibleAsync(CancellationToken ct = default)
        => Task.FromResult(!_backend.IsOffscreen);

    /// <inheritdoc/>
    public Task<bool> IsHiddenAsync(CancellationToken ct = default)
        => Task.FromResult(_backend.IsOffscreen);

    /// <inheritdoc/>
    public Task<bool> IsEnabledAsync(CancellationToken ct = default)
        => Task.FromResult(_backend.IsEnabled);

    /// <inheritdoc/>
    public Task<bool> IsDisabledAsync(CancellationToken ct = default)
        => Task.FromResult(!_backend.IsEnabled);

    /// <inheritdoc/>
    public Task<bool> IsCheckedAsync(CancellationToken ct = default)
        => Task.FromResult(_backend.GetToggleState() == true);

    /// <inheritdoc/>
    public Task<bool> IsEditableAsync(CancellationToken ct = default)
        // Approximate: editable = ValuePattern supported AND element is enabled.
        // A future wave can add IsReadOnly to IElementBackend if finer control is needed.
        => Task.FromResult(_backend.TryGetValue() != null && _backend.IsEnabled);

    // ── IFlawrightElement: Action methods ─────────────────────────────────────

    /// <inheritdoc/>
    public Task ClickAsync(LocatorClickOptions? options = null, CancellationToken ct = default)
    {
        // Wave D: honor options.Button, options.Position, options.Modifiers, options.Delay.
        _backend.Click();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DoubleClickAsync(LocatorDoubleClickOptions? options = null, CancellationToken ct = default)
    {
        // Wave D: honor options.
        _backend.DoubleClick();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task FillAsync(string text, LocatorFillOptions? options = null, CancellationToken ct = default)
    {
        if (!_backend.TrySetValue(text))
            throw new InvalidOperationException(
                "Element does not support text input.  " +
                "Ensure the element implements ValuePattern or is a TextBox control.");

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ClearAsync(LocatorClearOptions? options = null, CancellationToken ct = default)
    {
        _backend.TrySetValue(string.Empty);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task FocusAsync(CancellationToken ct = default)
    {
        _backend.Focus();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task HoverAsync(LocatorHoverOptions? options = null, CancellationToken ct = default)
    {
        var rect = _backend.BoundingRectangle;

        int x;
        int y;

        if (options?.Position is { } pos)
        {
            // Caller-specified offset relative to the element's top-left corner.
            x = rect.X + (int)pos.X;
            y = rect.Y + (int)pos.Y;
        }
        else
        {
            // Default: element centre.
            x = rect.X + rect.Width / 2;
            y = rect.Y + rect.Height / 2;
        }

        _input.MouseMove(x, y, steps: 0);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ScrollIntoViewIfNeededAsync(CancellationToken ct = default)
    {
        _backend.TryScrollIntoView();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task CheckAsync(LocatorCheckOptions? options = null, CancellationToken ct = default)
    {
        if (_backend.GetToggleState() == true)
            return Task.CompletedTask;  // already checked — no-op

        if (!_backend.TryToggleOn())
            throw new InvalidOperationException(
                "Element does not support toggle.  " +
                "Ensure the element implements TogglePattern.");

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UncheckAsync(LocatorUncheckOptions? options = null, CancellationToken ct = default)
    {
        if (_backend.GetToggleState() == false)
            return Task.CompletedTask;  // already unchecked — no-op

        if (!_backend.TryToggleOff())
            throw new InvalidOperationException(
                "Element does not support toggle.  " +
                "Ensure the element implements TogglePattern.");

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetCheckedAsync(bool @checked, LocatorSetCheckedOptions? options = null, CancellationToken ct = default)
        => @checked ? CheckAsync(null, ct) : UncheckAsync(null, ct);

    /// <inheritdoc/>
    public Task SelectOptionAsync(string value, LocatorSelectOptionOptions? options = null, CancellationToken ct = default)
    {
        if (!_backend.TrySelectItem(value))
            throw new InvalidOperationException(
                $"Could not find or select item '{value}'.  " +
                "Ensure the element has a descendant with matching Name or AutomationId " +
                "that implements SelectionItemPattern.");

        return Task.CompletedTask;
    }
}
