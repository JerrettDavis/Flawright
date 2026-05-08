using FlaUI.Core.Definitions;
using Flawright.Selectors;
using Xunit;

namespace Flawright.UnitTests.Selectors;

/// <summary>
/// Unit tests for <see cref="ControlTypeParser"/>.
/// Covers aliases, exact enum names, case-insensitivity, and unknown-value rejection.
/// </summary>
public sealed class ControlTypeParserTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Documented aliases
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("dropdown", ControlType.ComboBox)]
    [InlineData("DROPDOWN", ControlType.ComboBox)]
    [InlineData("DropDown", ControlType.ComboBox)]
    [InlineData("textbox", ControlType.Edit)]
    [InlineData("TEXTBOX", ControlType.Edit)]
    [InlineData("TextBox", ControlType.Edit)]
    [InlineData("input", ControlType.Edit)]
    [InlineData("INPUT", ControlType.Edit)]
    [InlineData("Input", ControlType.Edit)]
    [InlineData("label", ControlType.Text)]
    [InlineData("LABEL", ControlType.Text)]
    [InlineData("Label", ControlType.Text)]
    [InlineData("hyperlink", ControlType.Hyperlink)]
    [InlineData("HYPERLINK", ControlType.Hyperlink)]
    [InlineData("HyperLink", ControlType.Hyperlink)]
    public void Parse_Alias_ReturnsMappedControlType(string input, ControlType expected)
    {
        var result = ControlTypeParser.Parse(input);

        Assert.Equal(expected, result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Exact enum names (should still work via Enum.TryParse fallback)
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Button", ControlType.Button)]
    [InlineData("button", ControlType.Button)]
    [InlineData("BUTTON", ControlType.Button)]
    [InlineData("Edit", ControlType.Edit)]
    [InlineData("edit", ControlType.Edit)]
    [InlineData("EDIT", ControlType.Edit)]
    [InlineData("Document", ControlType.Document)]
    [InlineData("document", ControlType.Document)]
    [InlineData("ComboBox", ControlType.ComboBox)]
    [InlineData("combobox", ControlType.ComboBox)]
    [InlineData("Text", ControlType.Text)]
    [InlineData("text", ControlType.Text)]
    [InlineData("Hyperlink", ControlType.Hyperlink)]
    [InlineData("hyperlink", ControlType.Hyperlink)]
    [InlineData("MenuBar", ControlType.MenuBar)]
    [InlineData("MenuItem", ControlType.MenuItem)]
    [InlineData("List", ControlType.List)]
    [InlineData("ListItem", ControlType.ListItem)]
    [InlineData("Window", ControlType.Window)]
    [InlineData("Unknown", ControlType.Unknown)]   // real enum member, not "unknown value"
    [InlineData("UNKNOWN", ControlType.Unknown)]
    public void Parse_ExactEnumName_ReturnsCorrectControlType(string input, ControlType expected)
    {
        var result = ControlTypeParser.Parse(input);

        Assert.Equal(expected, result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Unknown values must throw ArgumentException — no Custom fallback
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("bogus")]
    [InlineData("textfield")]  // not a documented alias
    [InlineData("select")]     // not a documented alias
    [InlineData("")]
    public void Parse_UnknownValue_ThrowsArgumentException(string input)
    {
        var ex = Assert.Throws<ArgumentException>(() => ControlTypeParser.Parse(input));

        // Message should mention the bad value
        Assert.Contains(input, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_UnknownValue_MessageMentionsValidOptions()
    {
        var ex = Assert.Throws<ArgumentException>(() => ControlTypeParser.Parse("notacontroltype"));

        // Should hint at valid options so the developer knows what to do
        Assert.Contains("ControlType", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
