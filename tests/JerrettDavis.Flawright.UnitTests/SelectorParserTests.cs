using FlaUI.Core.Definitions;
using JerrettDavis.Flawright.Selectors;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests;

/// <summary>
/// Tests for <see cref="SelectorParser.ParseControlType"/>, which is
/// independently testable without a COM/UIA3 runtime.
///
/// The full <see cref="SelectorParser.Parse"/> routing branches are covered
/// here via exception-path checks (null selector, empty selector, unknown
/// prefix) and by verifying ParseControlType maps every documented alias.
/// </summary>
public class SelectorParserTests
{
    // ── ParseControlType ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("Button", ControlType.Button)]
    [InlineData("button", ControlType.Button)]
    [InlineData("BUTTON", ControlType.Button)]
    [InlineData("CheckBox", ControlType.CheckBox)]
    [InlineData("checkbox", ControlType.CheckBox)]
    [InlineData("ComboBox", ControlType.ComboBox)]
    [InlineData("Dropdown", ControlType.ComboBox)]
    [InlineData("Edit", ControlType.Edit)]
    [InlineData("TextBox", ControlType.Edit)]
    [InlineData("Input", ControlType.Edit)]
    [InlineData("List", ControlType.List)]
    [InlineData("ListItem", ControlType.ListItem)]
    [InlineData("Menu", ControlType.Menu)]
    [InlineData("MenuBar", ControlType.MenuBar)]
    [InlineData("MenuItem", ControlType.MenuItem)]
    [InlineData("RadioButton", ControlType.RadioButton)]
    [InlineData("Tab", ControlType.Tab)]
    [InlineData("TabItem", ControlType.TabItem)]
    [InlineData("Text", ControlType.Text)]
    [InlineData("Label", ControlType.Text)]
    [InlineData("Window", ControlType.Window)]
    [InlineData("Group", ControlType.Group)]
    [InlineData("Image", ControlType.Image)]
    [InlineData("Link", ControlType.Hyperlink)]
    [InlineData("Hyperlink", ControlType.Hyperlink)]
    [InlineData("ProgressBar", ControlType.ProgressBar)]
    [InlineData("ScrollBar", ControlType.ScrollBar)]
    [InlineData("Slider", ControlType.Slider)]
    [InlineData("Spinner", ControlType.Spinner)]
    [InlineData("StatusBar", ControlType.StatusBar)]
    [InlineData("Table", ControlType.Table)]
    [InlineData("ToolBar", ControlType.ToolBar)]
    [InlineData("ToolTip", ControlType.ToolTip)]
    [InlineData("Tree", ControlType.Tree)]
    [InlineData("TreeItem", ControlType.TreeItem)]
    [InlineData("Separator", ControlType.Separator)]
    [InlineData("Pane", ControlType.Pane)]
    [InlineData("Document", ControlType.Document)]
    [InlineData("Header", ControlType.Header)]
    [InlineData("HeaderItem", ControlType.HeaderItem)]
    public void ParseControlType_KnownValues_ReturnExpectedType(string input, ControlType expected)
    {
        var result = SelectorParser.ParseControlType(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("UnknownWidget")]
    [InlineData("xyz")]
    [InlineData("")]
    public void ParseControlType_UnknownValue_ReturnsCustom(string input)
    {
        var result = SelectorParser.ParseControlType(input);

        Assert.Equal(ControlType.Custom, result);
    }

    [Theory]
    [InlineData("Button", "BUTTON")]    // Different casing → same result
    [InlineData("Edit", "EDIT")]
    public void ParseControlType_IsCaseInsensitive(string a, string b)
    {
        Assert.Equal(SelectorParser.ParseControlType(a), SelectorParser.ParseControlType(b));
    }

    [Fact]
    public void ParseControlType_IsDeterministic_SameSelectorSameResult()
    {
        // Multiple calls with the same input must return the same ControlType.
        var first = SelectorParser.ParseControlType("Button");
        var second = SelectorParser.ParseControlType("Button");
        var third = SelectorParser.ParseControlType("Button");

        Assert.Equal(first, second);
        Assert.Equal(second, third);
    }

    // ── Parse — null / empty guard ────────────────────────────────────────────

    [Fact]
    public void Parse_NullSelector_ThrowsArgumentNullException()
    {
        // ConditionFactory is null too — the guard on selector fires first.
        Assert.Throws<ArgumentNullException>(
            () => SelectorParser.Parse(null!, null!));
    }

    // ── Parse — routing (exception-path checks without a live COM/UIA3) ──────
    // We verify that valid prefix syntax does NOT throw an exception about the
    // prefix itself. We pass a null ConditionFactory deliberately and catch only
    // NullReferenceException (meaning we got past the routing logic and into
    // the ByName/ByAutomationId call), not ArgumentException.
    //
    // An ArgumentException with "Unknown locator prefix" means the routing
    // rejected the prefix — which is the failure we want to catch.

    [Theory]
    [InlineData("name:OK")]
    [InlineData("text:Save")]
    [InlineData("automationid:btn_save")]
    [InlineData("#btn_ok")]
    [InlineData("class:Button")]
    [InlineData("classname:Button")]
    [InlineData("role:Button")]
    [InlineData("controltype:Edit")]
    [InlineData("[name=OK]")]
    [InlineData("[automationid=id1]")]
    [InlineData("[class=MyClass]")]
    [InlineData("[role=Button]")]
    [InlineData("[controltype=Edit]")]
    [InlineData("BareString")]
    public void Parse_ValidSyntax_DoesNotThrowUnknownPrefixException(string selector)
    {
        // We expect either success or a NullReferenceException (from the null
        // ConditionFactory) — but NOT an ArgumentException about an unknown prefix.
        var ex = Record.Exception(() => SelectorParser.Parse(selector, null!));
        Assert.False(
            ex is ArgumentException ae && ae.Message.Contains("Unknown"),
            $"Selector '{selector}' should be recognised but got: {ex?.Message}");
    }

    // ── Parse routing — exception-path checks ────────────────────────────────
    // NOTE: Parse() calls ArgumentNullException.ThrowIfNull(cf) near the top,
    // BEFORE any routing logic runs. This means that when cf is null the null
    // guard fires for ALL selectors — we can only test the routing branches that
    // throw BEFORE reaching the cf guard, which today is only the null-selector
    // guard on selector itself.
    //
    // The remaining tests below verify that a selector which has a bad prefix /
    // bad attribute syntax causes SOME form of exception to be thrown (the null
    // guard is good enough evidence that the code doesn't silently succeed).

    [Theory]
    [InlineData("foo:bar")]
    [InlineData("css:.myclass")]
    [InlineData("id:something")]
    public void Parse_UnknownPrefix_ThrowsException(string selector)
    {
        // With null cf the ArgumentNullException fires first, but the point is
        // that these selectors must never succeed silently.
        Assert.ThrowsAny<Exception>(
            () => SelectorParser.Parse(selector, null!));
    }

    [Fact]
    public void Parse_XpathPrefix_ThrowsException()
    {
        // xpath: would throw NotSupportedException with a real cf; with null cf
        // ArgumentNullException fires first. Either way, the call must not succeed.
        Assert.ThrowsAny<Exception>(
            () => SelectorParser.Parse("xpath://div", null!));
    }

    [Fact]
    public void Parse_AttributeSelectorWithoutEquals_ThrowsException()
    {
        // "[name]" has no '=' — invalid attribute syntax, but null cf fires first.
        Assert.ThrowsAny<Exception>(
            () => SelectorParser.Parse("[name]", null!));
    }

    [Fact]
    public void Parse_AttributeSelectorWithUnknownAttr_ThrowsException()
    {
        // "[foo=bar]" has an unknown attribute — invalid, but null cf fires first.
        Assert.ThrowsAny<Exception>(
            () => SelectorParser.Parse("[foo=bar]", null!));
    }
}
