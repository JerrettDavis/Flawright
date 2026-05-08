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
}
