using Flawright.Locator;
using Flawright.Selectors;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Locator;

/// <summary>
/// Tests for <see cref="IFlawrightLocator.GetByRole"/> state-filter options:
/// Checked, Disabled, Expanded, IncludeHidden, Pressed, Selected.
///
/// Also covers the pure-BackendPredicate path (state filter without any name filter).
/// Pattern: container locator resolved via "name:Container", then GetByRole scans
/// the container's descendants — same pattern as existing GetByRoleTests.
/// </summary>
public sealed class GetByRoleFilterTests
{
    // ── Checked filter ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByRole_WithCheckedTrue_FiltersUncheckedElements()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.CheckBox("On", initialState: true))
                .WithChild(UiaTree.CheckBox("Off", initialState: false)))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var checked_ = container.GetByRole(AriaRole.Checkbox, new LocatorGetByRoleOptions { Checked = true });

        var count = await checked_.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByRole_WithCheckedFalse_FiltersCheckedElements()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.CheckBox("On", initialState: true))
                .WithChild(UiaTree.CheckBox("Off", initialState: false)))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var unchecked_ = container.GetByRole(AriaRole.Checkbox, new LocatorGetByRoleOptions { Checked = false });

        var count = await unchecked_.CountAsync();
        Assert.Equal(1, count);
    }

    // ── Disabled filter ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByRole_WithDisabledTrue_FiltersEnabledElements()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.Button("Enabled"))
                .WithChild(UiaTree.Button("Disabled").AsDisabled()))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var disabled = container.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Disabled = true });

        var count = await disabled.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByRole_WithDisabledFalse_FiltersDisabledElements()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.Button("Enabled"))
                .WithChild(UiaTree.Button("Disabled").AsDisabled()))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var enabled = container.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Disabled = false });

        var count = await enabled.CountAsync();
        Assert.Equal(1, count);
    }

    // ── Expanded filter ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByRole_WithExpandedTrue_FiltersCollapsedElements()
    {
        var expandedItem = new FakeElementBackend(
            name: "Expanded",
            controlTypeName: "ComboBox",
            isEnabled: true);
        expandedItem.ExpandCollapseState = true;

        var collapsedItem = new FakeElementBackend(
            name: "Collapsed",
            controlTypeName: "ComboBox",
            isEnabled: true);
        collapsedItem.ExpandCollapseState = false;

        var container = new FakeElementBackend(
            name: "Container",
            controlTypeName: "Pane",
            children: [expandedItem, collapsedItem]);

        var root = new FakeElementBackend(
            name: "Window",
            controlTypeName: "Window",
            children: [container]);

        var locator = LocatorTestBase.CreateLocator("name:Container", root);
        var expanded = locator.GetByRole(AriaRole.Combobox, new LocatorGetByRoleOptions { Expanded = true });

        var count = await expanded.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByRole_WithExpandedFalse_FiltersExpandedElements()
    {
        var expandedItem = new FakeElementBackend(
            name: "Expanded",
            controlTypeName: "ComboBox",
            isEnabled: true);
        expandedItem.ExpandCollapseState = true;

        var collapsedItem = new FakeElementBackend(
            name: "Collapsed",
            controlTypeName: "ComboBox",
            isEnabled: true);
        collapsedItem.ExpandCollapseState = false;

        var container = new FakeElementBackend(
            name: "Container",
            controlTypeName: "Pane",
            children: [expandedItem, collapsedItem]);

        var root = new FakeElementBackend(
            name: "Window",
            controlTypeName: "Window",
            children: [container]);

        var locator = LocatorTestBase.CreateLocator("name:Container", root);
        var collapsed = locator.GetByRole(AriaRole.Combobox, new LocatorGetByRoleOptions { Expanded = false });

        var count = await collapsed.CountAsync();
        Assert.Equal(1, count);
    }

    // ── IncludeHidden = false filter ──────────────────────────────────────────

    [Fact]
    public async Task GetByRole_WithIncludeHiddenFalse_FiltersOffscreenElements()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.Button("Visible"))
                .WithChild(UiaTree.Button("Hidden").AsOffscreen()))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var visible = container.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { IncludeHidden = false });

        var count = await visible.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByRole_WithIncludeHiddenTrue_IncludesOffscreenElements()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.Button("Visible"))
                .WithChild(UiaTree.Button("Hidden").AsOffscreen()))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        // IncludeHidden=true (default) — no offscreen filter applied, all buttons included
        var all = container.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { IncludeHidden = true });

        var count = await all.CountAsync();
        Assert.Equal(2, count);
    }

    // ── Selected filter ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByRole_WithSelectedTrue_FiltersUnselectedElements()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.RadioButton("Selected", initialState: true))
                .WithChild(UiaTree.RadioButton("NotSelected", initialState: false)))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var selected = container.GetByRole(
            AriaRole.Radio,
            new LocatorGetByRoleOptions { Selected = true });

        var count = await selected.CountAsync();
        Assert.Equal(1, count);
    }

    // ── BackendPredicate path (state filter without name filter) ──────────────

    [Fact]
    public async Task GetByRole_WithCheckedFilter_AndNoName_UsesPureBackendPredicate()
    {
        // No Name option specified — exercises the backendPredicate-only path (lines 214-218)
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.CheckBox("CB1", initialState: true))
                .WithChild(UiaTree.CheckBox("CB2", initialState: false)))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var checked_ = container.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Checked = true /* no Name */ });

        var count = await checked_.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByRole_WithDisabledFilter_AndNoName_UsesPureBackendPredicate()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.Button("B1"))
                .WithChild(UiaTree.Button("B2").AsDisabled()))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var disabled = container.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Disabled = true /* no Name */ });

        var count = await disabled.CountAsync();
        Assert.Equal(1, count);
    }
}
