using System.Drawing;
using Flawright.Backends;

namespace Flawright.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="IElementBackend"/> for unit tests.
///
/// Construct a tree using <see cref="UiaTreeBuilder"/> or manually. Tracks
/// all interactions (clicks, value sets, etc.) so tests can assert behavior
/// without a live UIA tree.
/// </summary>
internal sealed class FakeElementBackend : IElementBackend
{
    private readonly List<FakeElementBackend> _children;
    private string? _value;
    private bool? _toggleState;    // null = no toggle pattern; false = off; true = on
    private bool? _selectionState; // null = no SelectionItemPattern

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises a new fake element with the given properties.
    /// </summary>
    /// <param name="name">UIA Name property.</param>
    /// <param name="automationId">UIA AutomationId property.</param>
    /// <param name="className">UIA ClassName property.</param>
    /// <param name="controlTypeName">String name of the UIA ControlType.</param>
    /// <param name="isEnabled">Whether the element is enabled. Default <see langword="true"/>.</param>
    /// <param name="isOffscreen">Whether the element is off-screen. Default <see langword="false"/>.</param>
    /// <param name="boundingRectangle">Bounding rectangle in screen coords.</param>
    /// <param name="children">Child elements for the fake tree.</param>
    /// <param name="initialValue">Initial ValuePattern value. <see langword="null"/> = no ValuePattern.</param>
    /// <param name="supportsToggle">Whether the element supports TogglePattern.</param>
    /// <param name="initialToggleState">Initial toggle state (<see langword="true"/> = on, <see langword="false"/> = off).</param>
    /// <param name="supportsSelection">Whether the element supports SelectionItemPattern.</param>
    /// <param name="initialSelectionState">Initial selection state (<see langword="true"/> = selected, <see langword="false"/> = not selected).</param>
    public FakeElementBackend(
        string? name = null,
        string? automationId = null,
        string? className = null,
        string controlTypeName = "Pane",
        bool isEnabled = true,
        bool isOffscreen = false,
        Rectangle boundingRectangle = default,
        IEnumerable<FakeElementBackend>? children = null,
        string? initialValue = null,
        bool supportsToggle = false,
        bool initialToggleState = false,
        bool supportsSelection = false,
        bool initialSelectionState = false)
    {
        Name = name;
        AutomationId = automationId;
        ClassName = className;
        ControlTypeName = controlTypeName;
        IsEnabled = isEnabled;
        IsOffscreen = isOffscreen;
        BoundingRectangle = boundingRectangle;
        _children = children?.ToList() ?? [];
        _value = initialValue;
        _toggleState = supportsToggle ? initialToggleState : null;
        _selectionState = supportsSelection ? initialSelectionState : null;
    }

    // ── Interaction recording ─────────────────────────────────────────────────

    /// <summary>Number of single-clicks recorded on this element.</summary>
    public int ClickCount { get; private set; }

    /// <summary>Number of double-clicks recorded on this element.</summary>
    public int DoubleClickCount { get; private set; }

    /// <summary>Number of <see cref="TryInvoke"/> calls recorded on this element.</summary>
    public int InvokeCount { get; private set; }

    /// <summary>
    /// Controls the return value of <see cref="TryInvoke"/>.
    /// Defaults to <see langword="true"/> (invoke succeeds).
    /// </summary>
    public bool TryInvokeResult { get; set; } = true;

    /// <summary>
    /// Controls the return value of <see cref="TrySelect"/>.
    /// Defaults to <see langword="false"/> (SelectionItemPattern not supported).
    /// Set to <see langword="true"/> to simulate a RadioButton or other selection element.
    /// </summary>
    public bool TrySelectResult { get; set; }

    /// <summary>Whether <see cref="TrySelect"/> was called.</summary>
    public bool WasSelected { get; private set; }

    /// <summary>Number of focus operations recorded on this element.</summary>
    public int FocusCount { get; private set; }

    /// <summary>All values passed to <see cref="TrySetValue"/>, in order.</summary>
    public IReadOnlyList<string> Inputs => _inputs.AsReadOnly();
    private readonly List<string> _inputs = [];

    /// <summary>Whether <see cref="TryScrollIntoView"/> was called.</summary>
    public bool ScrolledIntoView { get; private set; }

    /// <summary>The last value passed to <see cref="TrySelectItem"/>, or <see langword="null"/>.</summary>
    public string? LastSelectedItem { get; private set; }

    /// <summary>
    /// Controls the return value of <see cref="TryExpand"/>.
    /// Defaults to <see langword="false"/> (ExpandCollapsePattern not supported).
    /// Set to <see langword="true"/> to simulate a ComboBox or other collapsible element.
    /// </summary>
    public bool TryExpandResult { get; set; }

    /// <summary>Whether <see cref="TryExpand"/> was called.</summary>
    public bool WasExpanded { get; private set; }

    /// <summary>
    /// Controls the return value of <see cref="GetExpandCollapseState"/>.
    /// <see langword="null"/> (the default) means ExpandCollapsePattern not supported.
    /// Set to <see langword="true"/> to simulate expanded, <see langword="false"/> for collapsed.
    /// </summary>
    public bool? ExpandCollapseState { get; set; }

    // ── IElementBackend: Identity ─────────────────────────────────────────────

    /// <inheritdoc/>
    public string? AutomationId { get; set; }

    /// <inheritdoc/>
    public string? Name { get; set; }

    /// <inheritdoc/>
    public string? ClassName { get; set; }

    /// <inheritdoc/>
    public string ControlTypeName { get; set; }

    // ── IElementBackend: State ────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool IsEnabled { get; set; }

    /// <inheritdoc/>
    public bool IsOffscreen { get; set; }

    /// <inheritdoc/>
    public Rectangle BoundingRectangle { get; set; }

    // ── IElementBackend: Actions ──────────────────────────────────────────────

    /// <inheritdoc/>
    public void Click() => ClickCount++;

    /// <inheritdoc/>
    public void DoubleClick() => DoubleClickCount++;

    /// <inheritdoc/>
    public void Focus() => FocusCount++;

    /// <inheritdoc/>
    public bool TryInvoke()
    {
        InvokeCount++;
        return TryInvokeResult;
    }

    // ── IElementBackend: Pattern operations ───────────────────────────────────

    /// <inheritdoc/>
    public bool TrySetValue(string text)
    {
        _inputs.Add(text);
        _value = text;
        return true;
    }

    /// <inheritdoc/>
    public string? TryGetValue() => _value;

    /// <summary>
    /// Document text is not supported by the fake (returns <see langword="null"/>).
    /// Override in a subclass if needed.
    /// </summary>
    public string? TryGetDocumentText() => null;

    /// <inheritdoc/>
    public bool TrySelect()
    {
        if (!TrySelectResult)
            return false;
        WasSelected = true;
        // If this fake element supports SelectionItemPattern, update its state.
        if (_selectionState.HasValue)
            _selectionState = true;
        return true;
    }

    /// <inheritdoc/>
    public bool TryToggleOn()
    {
        if (_toggleState == null)
            return false;
        _toggleState = true;
        return true;
    }

    /// <inheritdoc/>
    public bool TryToggleOff()
    {
        if (_toggleState == null)
            return false;
        _toggleState = false;
        return true;
    }

    /// <inheritdoc/>
    public bool? GetToggleState() => _toggleState;

    /// <inheritdoc/>
    public bool? GetSelectionState() => _selectionState;

    /// <inheritdoc/>
    public string? GetSelectedText()
    {
        // Return the Name of the first child marked as selected (simulates SelectionPattern).
        foreach (var child in _children)
        {
            if (child._selectionState == true)
                return child.Name;
        }

        // Fallback: ValuePattern (editable combo).
        return _value;
    }

    /// <inheritdoc/>
    public bool TryScrollIntoView()
    {
        ScrolledIntoView = true;
        return true;
    }

    /// <inheritdoc/>
    public bool TryExpand()
    {
        WasExpanded = true;
        return TryExpandResult;
    }

    /// <inheritdoc/>
    public bool? GetExpandCollapseState() => ExpandCollapseState;

    /// <inheritdoc/>
    public bool TrySelectItem(string nameOrId)
    {
        LastSelectedItem = nameOrId;
        // Simulate finding the child
        var found = FindDescendantByNameOrId(this, nameOrId);
        return found != null;
    }

    // ── IElementBackend: Screenshot ───────────────────────────────────────────

    /// <summary>
    /// Returns <see cref="ScreenshotBytes"/> if non-<see langword="null"/>;
    /// otherwise returns a minimal 1×1 white PNG so that unit-test assertions
    /// on <c>screenshot.Length &gt; 0</c> pass without requiring a real window.
    /// </summary>
    public byte[] CaptureScreenshot() => ScreenshotBytes ?? FakeOnePxPng;

    /// <summary>
    /// Configurable screenshot bytes returned by <see cref="CaptureScreenshot"/>.
    /// <see langword="null"/> (the default) causes a 1×1 white PNG to be returned.
    /// </summary>
    public byte[]? ScreenshotBytes { get; set; }

    // Minimal valid 1×1 white PNG (67 bytes).
    private static readonly byte[] FakeOnePxPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwADhQGAWjR9awAAAABJRU5ErkJggg==");

    // ── IElementBackend: Tree traversal ───────────────────────────────────────

    /// <inheritdoc/>
    public IEnumerable<IElementBackend> FindAll(IElementCondition condition)
    {
        if (condition is FakeElementCondition fake)
        {
            return EnumerateDescendants().Where(d => fake.Matches(d)).Cast<IElementBackend>();
        }

        // Fallback: treat any unknown condition as "match all descendants"
        return EnumerateDescendants().Cast<IElementBackend>();
    }

    /// <inheritdoc/>
    public IElementBackend? FindFirst(IElementCondition condition) => FindAll(condition).FirstOrDefault();

    // ── Tree helpers ──────────────────────────────────────────────────────────

    /// <summary>Gets the direct children of this element.</summary>
    public IReadOnlyList<FakeElementBackend> Children => _children.AsReadOnly();

    /// <summary>Adds a child to this element's tree.</summary>
    public FakeElementBackend AddChild(FakeElementBackend child)
    {
        _children.Add(child);
        return this;
    }

    private IEnumerable<FakeElementBackend> EnumerateDescendants()
    {
        foreach (var child in _children)
        {
            yield return child;
            foreach (var grandchild in child.EnumerateDescendants())
                yield return grandchild;
        }
    }

    private static FakeElementBackend? FindDescendantByNameOrId(FakeElementBackend root, string nameOrId)
    {
        foreach (var child in root._children)
        {
            if (string.Equals(child.Name, nameOrId, StringComparison.OrdinalIgnoreCase)
             || string.Equals(child.AutomationId, nameOrId, StringComparison.OrdinalIgnoreCase))
                return child;

            var found = FindDescendantByNameOrId(child, nameOrId);
            if (found != null)
                return found;
        }

        return null;
    }

}
