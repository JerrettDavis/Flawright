using System.Text.RegularExpressions;
using Flawright.Locator;
using Flawright.Selectors;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Locator;

/// <summary>
/// Tests for <see cref="IFlawrightLocator.GetByRole"/> — selector generation
/// and element resolution via the fake translator.
/// </summary>
public sealed class GetByRoleTests
{
    // ── Selector string ───────────────────────────────────────────────────────

    [Fact]
    public void GetByRole_Button_SelectorContainsButtonControlType()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var roleLocator = locator.GetByRole(AriaRole.Button);
        Assert.Contains("Button", roleLocator.Selector);
    }

    [Fact]
    public void GetByRole_Checkbox_SelectorContainsCheckBox()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:CheckBox", root);

        var roleLocator = locator.GetByRole(AriaRole.Checkbox);
        Assert.Contains("CheckBox", roleLocator.Selector);
    }

    [Fact]
    public void GetByRole_WithName_SelectorContainsNameFilter()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var roleLocator = locator.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "OK" });
        Assert.Contains("OK", roleLocator.Selector);
    }

    [Fact]
    public void GetByRole_UnsupportedRole_ThrowsNotSupportedException()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        // AriaRole.Banner is a web-only role with no UIA equivalent
        Assert.Throws<NotSupportedException>(() => locator.GetByRole(AriaRole.Banner));
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByRole_Button_FindsButtonElements()
    {
        // Structure: root Window > Toolbar > Button1, Button2
        // GetByRole(Button) off the Toolbar locator finds buttons within Toolbar.
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Toolbar")
                .WithChild(UiaTree.Button("OK"))
                .WithChild(UiaTree.Button("Cancel"))
                .WithChild(UiaTree.Edit("Name").WithValue("")))
            .Build();

        // Locator resolves to the Toolbar pane, then GetByRole finds buttons within it.
        var toolbar = LocatorTestBase.CreateLocator("name:Toolbar", root);
        var buttons = toolbar.GetByRole(AriaRole.Button);
        var count = await buttons.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetByRole_Button_WithName_Exact_FindsExactMatch()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.Button("Save"))
                .WithChild(UiaTree.Button("Save Draft")))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var buttons = container.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Save", Exact = true });
        var count = await buttons.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByRole_Button_WithName_NotExact_FindsPartialMatches()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.Button("Save"))
                .WithChild(UiaTree.Button("Save Draft"))
                .WithChild(UiaTree.Button("Delete")))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var buttons = container.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Save", Exact = false });
        var count = await buttons.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public void GetByRole_ReturnsNewLocator()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        var roleLocator = locator.GetByRole(AriaRole.Button);
        Assert.NotSame(locator, roleLocator);
    }

    // ── NameRegex option ──────────────────────────────────────────────────────

    [Fact]
    public void GetByRole_WithNameRegex_SelectorContainsRegex()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var regex = new Regex("^Save", RegexOptions.None, TimeSpan.FromSeconds(1));
        var roleLocator = locator.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameRegex = regex });
        // Selector should encode the regex pattern for diagnostics
        Assert.Contains("Save", roleLocator.Selector);
    }

    [Fact]
    public async Task GetByRole_WithNameRegex_FiltersElementsByRegex()
    {
        // Structure: root > Container > Save, Save Draft, Delete
        // Regex "^Save" should match "Save" and "Save Draft" only.
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.Button("Save"))
                .WithChild(UiaTree.Button("Save Draft"))
                .WithChild(UiaTree.Button("Delete")))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var regex = new Regex(@"^Save", RegexOptions.None, TimeSpan.FromSeconds(1));
        var buttons = container.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { NameRegex = regex });

        var count = await buttons.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetByRole_NameRegex_TakesPrecedenceOverName_WhenBothSet()
    {
        // Name="Save" and NameRegex="^Save Draft$" are both set.
        // NameRegex should win, matching only "Save Draft".
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.Button("Save"))
                .WithChild(UiaTree.Button("Save Draft"))
                .WithChild(UiaTree.Button("Delete")))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var regex = new Regex(@"^Save Draft$", RegexOptions.None, TimeSpan.FromSeconds(1));
        var buttons = container.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Save", NameRegex = regex });

        var count = await buttons.CountAsync();
        // NameRegex=^Save Draft$ wins → only "Save Draft" matches
        Assert.Equal(1, count);
        var text = await buttons.First.InnerTextAsync();
        Assert.Equal("Save Draft", text);
    }

    [Fact]
    public async Task GetByRole_WithNameRegex_NoMatch_ReturnsZero()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.Button("OK"))
                .WithChild(UiaTree.Button("Cancel")))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var regex = new Regex(@"^NonExistent$", RegexOptions.None, TimeSpan.FromSeconds(1));
        var buttons = container.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { NameRegex = regex });

        var count = await buttons.CountAsync();
        Assert.Equal(0, count);
    }

    // ── Non-Button controls: Name vs Value disambiguation ─────────────────────
    // These tests guard against the regression where GetByRole matched against
    // GetElementText (which falls back to Value for Edit controls) instead of the
    // accessible Name property directly.

    [Fact]
    public async Task GetByRole_Edit_WithName_MatchesAccessibleNameNotValue()
    {
        // "Username" is the accessible Name; "admin" is the Value.
        // NameRegex "Username" should find the edit, but "admin" should not.
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Form")
                .WithChild(UiaTree.Edit("Username").WithValue("admin"))
                .WithChild(UiaTree.Edit("Password").WithValue("secret")))
            .Build();

        var form = LocatorTestBase.CreateLocator("name:Form", root);

        // Matching by accessible name "Username" must find exactly one edit.
        var byName = form.GetByRole(
            AriaRole.Textbox,
            new LocatorGetByRoleOptions { Name = "Username" });
        var countByName = await byName.CountAsync();
        Assert.Equal(1, countByName);

        // Matching by the VALUE "admin" must NOT find anything — values are not names.
        var byValue = form.GetByRole(
            AriaRole.Textbox,
            new LocatorGetByRoleOptions { Name = "admin" });
        var countByValue = await byValue.CountAsync();
        Assert.Equal(0, countByValue);
    }

    [Fact]
    public async Task GetByRole_Edit_WithNameRegex_MatchesAccessibleNameNotValue()
    {
        // "SearchBox" is the accessible Name; "Enter search terms" is the Value.
        // The regex /^Search/ should match the Name, not the Value.
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Toolbar")
                .WithChild(UiaTree.Edit("SearchBox").WithValue("Enter search terms"))
                .WithChild(UiaTree.Edit("FilterBox").WithValue("Enter filter")))
            .Build();

        var toolbar = LocatorTestBase.CreateLocator("name:Toolbar", root);
        var regex = new Regex(@"^Search", RegexOptions.None, TimeSpan.FromSeconds(1));

        // Should match by Name "SearchBox", not by the Value content.
        var byNameRegex = toolbar.GetByRole(
            AriaRole.Textbox,
            new LocatorGetByRoleOptions { NameRegex = regex });
        var countByNameRegex = await byNameRegex.CountAsync();
        Assert.Equal(1, countByNameRegex);

        // Confirm: a regex that would only match the Value does NOT find anything.
        var valueRegex = new Regex(@"^Enter", RegexOptions.None, TimeSpan.FromSeconds(1));
        var byValueRegex = toolbar.GetByRole(
            AriaRole.Textbox,
            new LocatorGetByRoleOptions { NameRegex = valueRegex });
        var countByValueRegex = await byValueRegex.CountAsync();
        Assert.Equal(0, countByValueRegex);
    }
}
