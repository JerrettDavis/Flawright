using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;

namespace JerrettDavis.Flawright.Selectors;

/// <summary>
/// Parses Playwright-inspired selector strings into FlaUI
/// <see cref="ConditionBase"/> objects.
/// </summary>
/// <remarks>
/// <para>Supported selector syntax:</para>
/// <list type="table">
///   <listheader><term>Syntax</term><description>Meaning</description></listheader>
///   <item><term><c>#foo</c></term><description>AutomationId equals "foo"</description></item>
///   <item><term><c>[name=Foo]</c></term><description>Name equals "Foo"</description></item>
///   <item><term><c>name:Foo</c></term><description>Name equals "Foo"</description></item>
///   <item><term><c>text:Foo</c></term><description>Name equals "Foo" (text match)</description></item>
///   <item><term><c>automationid:Foo</c></term><description>AutomationId equals "Foo"</description></item>
///   <item><term><c>class:Foo</c> or <c>[class=Foo]</c></term><description>ClassName equals "Foo"</description></item>
///   <item><term><c>role:Button</c> or <c>[role=Button]</c></term><description>ControlType = Button (parsed from string)</description></item>
///   <item><term><c>controltype:Edit</c></term><description>ControlType = Edit (alias for role)</description></item>
///   <item><term><c>Foo</c> (bare)</term><description>Tries Name first (smart fallback)</description></item>
/// </list>
/// </remarks>
internal static class SelectorParser
{
    /// <summary>
    /// Parses a selector string and returns a <see cref="ConditionBase"/> that
    /// can be passed to FlaUI's <c>FindFirstDescendant</c> /
    /// <c>FindAllDescendants</c>.
    /// </summary>
    /// <param name="selector">The raw selector string.</param>
    /// <param name="cf">The condition factory obtained from the automation instance.</param>
    /// <returns>A <see cref="ConditionBase"/> representing the selector.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the selector uses an unsupported prefix or syntax.
    /// </exception>
    internal static ConditionBase Parse(string selector, ConditionFactory cf)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(cf);

        selector = selector.Trim();

        // CSS-style: #automationId
        if (selector.StartsWith('#'))
        {
            var id = selector[1..];
            return cf.ByAutomationId(id);
        }

        // CSS attribute-style: [name=Value]  [role=Button]  [class=MyClass]
        if (selector.StartsWith('[') && selector.EndsWith(']'))
        {
            return ParseAttributeSelector(selector[1..^1], cf);
        }

        // Colon-prefix: prefix:Value
        var colonIdx = selector.IndexOf(':');
        if (colonIdx > 0)
        {
            var prefix = selector[..colonIdx].Trim().ToUpperInvariant();
            var value = selector[(colonIdx + 1)..].Trim();
            return PrefixToCondition(prefix, value, selector, cf);
        }

        // Bare string — smart fallback: match by Name
        return cf.ByName(selector);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static PropertyCondition ParseAttributeSelector(string inner, ConditionFactory cf)
    {
        var eqIdx = inner.IndexOf('=');
        if (eqIdx <= 0)
            throw new ArgumentException(
                $"Invalid attribute selector syntax: [{inner}]",
                nameof(inner));

        var attr = inner[..eqIdx].Trim().ToUpperInvariant();
        var value = inner[(eqIdx + 1)..].Trim();

        return attr switch
        {
            "NAME" => cf.ByName(value),
            "AUTOMATIONID" => cf.ByAutomationId(value),
            "CLASS" or "CLASSNAME" => cf.ByClassName(value),
            "ROLE" or "CONTROLTYPE" => cf.ByControlType(ParseControlType(value)),
            _ => throw new ArgumentException(
                $"Unknown attribute in selector: [{inner}]",
                nameof(inner))
        };
    }

    private static PropertyCondition PrefixToCondition(
        string upperPrefix,
        string value,
        string rawSelector,
        ConditionFactory cf)
    {
        return upperPrefix switch
        {
            "NAME" => cf.ByName(value),
            "TEXT" => cf.ByName(value),
            "AUTOMATIONID" => cf.ByAutomationId(value),
            "CLASS" or "CLASSNAME" => cf.ByClassName(value),
            "ROLE" or "CONTROLTYPE" => cf.ByControlType(ParseControlType(value)),
            "XPATH" => throw new NotSupportedException("XPath locators are not yet supported."),
            _ => throw new ArgumentException(
                $"Unknown locator prefix '{upperPrefix}' in selector: {rawSelector}",
                nameof(rawSelector))
        };
    }

    /// <summary>
    /// Maps a human-readable control-type name to a FlaUI
    /// <see cref="ControlType"/> enum value.
    /// </summary>
    /// <param name="value">Case-insensitive control type name (e.g. "Button").</param>
    /// <returns>The matching <see cref="ControlType"/>.</returns>
    internal static ControlType ParseControlType(string value)
    {
        return value.ToUpperInvariant() switch
        {
            "BUTTON" => ControlType.Button,
            "CHECKBOX" => ControlType.CheckBox,
            "COMBOBOX" or "DROPDOWN" => ControlType.ComboBox,
            "EDIT" or "TEXTBOX" or "INPUT" => ControlType.Edit,
            "LIST" => ControlType.List,
            "LISTITEM" => ControlType.ListItem,
            "MENU" => ControlType.Menu,
            "MENUBAR" => ControlType.MenuBar,
            "MENUITEM" => ControlType.MenuItem,
            "RADIOBUTTON" => ControlType.RadioButton,
            "TAB" => ControlType.Tab,
            "TABITEM" => ControlType.TabItem,
            "TEXT" or "LABEL" => ControlType.Text,
            "WINDOW" => ControlType.Window,
            "GROUP" => ControlType.Group,
            "IMAGE" => ControlType.Image,
            "LINK" or "HYPERLINK" => ControlType.Hyperlink,
            "PROGRESSBAR" => ControlType.ProgressBar,
            "SCROLLBAR" => ControlType.ScrollBar,
            "SLIDER" => ControlType.Slider,
            "SPINNER" => ControlType.Spinner,
            "STATUSBAR" => ControlType.StatusBar,
            "TABLE" => ControlType.Table,
            "TOOLBAR" => ControlType.ToolBar,
            "TOOLTIP" => ControlType.ToolTip,
            "TREE" => ControlType.Tree,
            "TREEITEM" => ControlType.TreeItem,
            "SEPARATOR" => ControlType.Separator,
            "PANE" => ControlType.Pane,
            "DOCUMENT" => ControlType.Document,
            "HEADER" => ControlType.Header,
            "HEADERITEM" => ControlType.HeaderItem,
            _ => ControlType.Custom
        };
    }
}
