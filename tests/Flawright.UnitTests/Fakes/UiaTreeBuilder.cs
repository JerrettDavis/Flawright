using System.Drawing;

namespace Flawright.UnitTests.Fakes;

/// <summary>
/// Fluent DSL for building fake UIA trees in unit tests.
/// </summary>
/// <example>
/// <code>
/// var root = UiaTree.Window("MyApp")
///     .WithChild(UiaTree.Button("OK").WithAutomationId("ok-btn"))
///     .WithChild(UiaTree.Edit("Content").WithValue("Hello"))
///     .Build();
/// </code>
/// </example>
internal sealed class UiaTreeBuilder
{
    private string? _name;
    private string? _automationId;
    private string? _className;
    private string _controlTypeName = "Pane";
    private bool _isEnabled = true;
    private bool _isOffscreen;
    private Rectangle _bounds;
    private string? _value;
    private bool _supportsToggle;
    private bool _initialToggleState;
    private readonly List<UiaTreeBuilder> _children = [];

    private UiaTreeBuilder() { }

    // ── Factory shortcuts ─────────────────────────────────────────────────────

    /// <summary>Creates a builder for a Window element.</summary>
    public static UiaTreeBuilder Window(string name) => new UiaTreeBuilder().WithControlType("Window").WithName(name);

    /// <summary>Creates a builder for a Button element.</summary>
    public static UiaTreeBuilder Button(string name) => new UiaTreeBuilder().WithControlType("Button").WithName(name);

    /// <summary>Creates a builder for an Edit (text input) element.</summary>
    public static UiaTreeBuilder Edit(string name) => new UiaTreeBuilder().WithControlType("Edit").WithName(name);

    /// <summary>Creates a builder for a CheckBox element with toggle support.</summary>
    public static UiaTreeBuilder CheckBox(string name, bool initialState = false)
        => new UiaTreeBuilder().WithControlType("CheckBox").WithName(name).WithToggle(initialState);

    /// <summary>Creates a builder for a generic Pane element.</summary>
    public static UiaTreeBuilder Pane(string? name = null) => new UiaTreeBuilder().WithControlType("Pane").WithName(name ?? string.Empty);

    /// <summary>Creates a builder for a List element.</summary>
    public static UiaTreeBuilder List(string name) => new UiaTreeBuilder().WithControlType("List").WithName(name);

    /// <summary>Creates a builder for a ListItem element.</summary>
    public static UiaTreeBuilder ListItem(string name) => new UiaTreeBuilder().WithControlType("ListItem").WithName(name);

    // ── Fluent configuration ──────────────────────────────────────────────────

    /// <summary>Sets the element's Name property.</summary>
    public UiaTreeBuilder WithName(string? name) { _name = name; return this; }

    /// <summary>Sets the element's AutomationId property.</summary>
    public UiaTreeBuilder WithAutomationId(string automationId) { _automationId = automationId; return this; }

    /// <summary>Sets the element's ClassName property.</summary>
    public UiaTreeBuilder WithClassName(string className) { _className = className; return this; }

    /// <summary>Sets the element's ControlTypeName.</summary>
    public UiaTreeBuilder WithControlType(string typeName) { _controlTypeName = typeName; return this; }

    /// <summary>Sets the element's initial value (simulates ValuePattern).</summary>
    public UiaTreeBuilder WithValue(string value) { _value = value; return this; }

    /// <summary>Marks the element as disabled.</summary>
    public UiaTreeBuilder AsDisabled() { _isEnabled = false; return this; }

    /// <summary>Marks the element as off-screen (hidden).</summary>
    public UiaTreeBuilder AsOffscreen() { _isOffscreen = true; return this; }

    /// <summary>Sets the bounding rectangle.</summary>
    public UiaTreeBuilder WithBounds(int x, int y, int width, int height)
    {
        _bounds = new Rectangle(x, y, width, height);
        return this;
    }

    /// <summary>Enables toggle support and sets the initial state.</summary>
    public UiaTreeBuilder WithToggle(bool initialState = false)
    {
        _supportsToggle = true;
        _initialToggleState = initialState;
        return this;
    }

    /// <summary>Adds a child element to the tree.</summary>
    public UiaTreeBuilder WithChild(UiaTreeBuilder childBuilder)
    {
        _children.Add(childBuilder);
        return this;
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Constructs the <see cref="FakeElementBackend"/> tree rooted at this element.
    /// </summary>
    public FakeElementBackend Build()
    {
        var children = _children.Select(c => c.Build());
        return new FakeElementBackend(
            name: _name,
            automationId: _automationId,
            className: _className,
            controlTypeName: _controlTypeName,
            isEnabled: _isEnabled,
            isOffscreen: _isOffscreen,
            boundingRectangle: _bounds,
            children: children,
            initialValue: _value,
            supportsToggle: _supportsToggle,
            initialToggleState: _initialToggleState);
    }
}

/// <summary>
/// Static entry-point alias for <see cref="UiaTreeBuilder"/> factory methods.
/// </summary>
internal static class UiaTree
{
    /// <inheritdoc cref="UiaTreeBuilder.Window"/>
    public static UiaTreeBuilder Window(string name) => UiaTreeBuilder.Window(name);

    /// <inheritdoc cref="UiaTreeBuilder.Button"/>
    public static UiaTreeBuilder Button(string name) => UiaTreeBuilder.Button(name);

    /// <inheritdoc cref="UiaTreeBuilder.Edit"/>
    public static UiaTreeBuilder Edit(string name) => UiaTreeBuilder.Edit(name);

    /// <inheritdoc cref="UiaTreeBuilder.CheckBox"/>
    public static UiaTreeBuilder CheckBox(string name, bool initialState = false)
        => UiaTreeBuilder.CheckBox(name, initialState);

    /// <inheritdoc cref="UiaTreeBuilder.Pane"/>
    public static UiaTreeBuilder Pane(string? name = null) => UiaTreeBuilder.Pane(name);

    /// <inheritdoc cref="UiaTreeBuilder.List"/>
    public static UiaTreeBuilder List(string name) => UiaTreeBuilder.List(name);

    /// <inheritdoc cref="UiaTreeBuilder.ListItem"/>
    public static UiaTreeBuilder ListItem(string name) => UiaTreeBuilder.ListItem(name);
}
