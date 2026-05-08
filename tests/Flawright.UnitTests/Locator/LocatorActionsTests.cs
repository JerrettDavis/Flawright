using FlaUI.Core.WindowsAPI;
using Flawright.Locator;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Locator;

/// <summary>
/// Tests for async action methods on <see cref="FlawrightLocator"/>:
/// Click, DoubleClick, Fill, Clear, Type, PressSequentially, Press,
/// Check, Uncheck, SetChecked, SelectOption, Hover, Focus, Blur,
/// ScrollIntoViewIfNeeded, DragTo.
///
/// All tests verify that the correct backend / input backend method is
/// invoked after successful element resolution.
/// </summary>
public sealed class LocatorActionsTests
{
    // ── ClickAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ClickAsync_ClicksBackendElement()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await locator.ClickAsync();

        var button = (FakeElementBackend)root.Children[0];
        Assert.Equal(1, button.ClickCount);
    }

    [Fact]
    public async Task ClickAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.ClickAsync());
    }

    // ── DoubleClickAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DoubleClickAsync_DoubleClicksBackendElement()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await locator.DoubleClickAsync();

        var button = (FakeElementBackend)root.Children[0];
        Assert.Equal(1, button.DoubleClickCount);
    }

    [Fact]
    public async Task DoubleClickAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.DoubleClickAsync());
    }

    // ── FillAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task FillAsync_SetsValueOnBackend()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Edit("Name").WithValue(""))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        await locator.FillAsync("Hello World");

        var edit = (FakeElementBackend)root.Children[0];
        Assert.Contains("Hello World", edit.Inputs);
    }

    [Fact]
    public async Task FillAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.FillAsync("text"));
    }

    // ── ClearAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ClearAsync_SetsEmptyValue()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Edit("Name").WithValue("existing text"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        await locator.ClearAsync();

        var edit = (FakeElementBackend)root.Children[0];
        Assert.Contains(string.Empty, edit.Inputs);
    }

    [Fact]
    public async Task ClearAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.ClearAsync());
    }

    // ── TypeAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task TypeAsync_FocusesElement_AndTypesText()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Edit("Name").WithValue(""))
            .Build();
        var input = new FakeInputBackend();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root, input: input);

        await locator.TypeAsync("Hello");

        var edit = (FakeElementBackend)root.Children[0];
        Assert.Equal(1, edit.FocusCount);
        Assert.Contains("Hello", input.TypedTexts);
    }

    [Fact]
    public async Task TypeAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.TypeAsync("text"));
    }

    // ── PressSequentiallyAsync ────────────────────────────────────────────────

    [Fact]
    public async Task PressSequentiallyAsync_FocusesElement_AndTypesText()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Edit("Name").WithValue(""))
            .Build();
        var input = new FakeInputBackend();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root, input: input);

        await locator.PressSequentiallyAsync("ABC");

        var edit = (FakeElementBackend)root.Children[0];
        Assert.Equal(1, edit.FocusCount);
        Assert.Contains("ABC", input.TypedTexts);
    }

    // ── PressAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PressAsync_FocusesElement_AndTapsKey()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Edit("Name").WithValue(""))
            .Build();
        var input = new FakeInputBackend();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root, input: input);

        await locator.PressAsync("Enter");

        var edit = (FakeElementBackend)root.Children[0];
        Assert.Equal(1, edit.FocusCount);
        Assert.NotEmpty(input.KeyTaps);
    }

    [Fact]
    public async Task PressAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.PressAsync("Enter"));
    }

    // ── CheckAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckAsync_TogglesElementOn()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.CheckBox("Accept", initialState: false))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:CheckBox", root);

        await locator.CheckAsync();

        var cb = (FakeElementBackend)root.Children[0];
        Assert.Equal(true, cb.GetToggleState());
    }

    [Fact]
    public async Task CheckAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:CheckBox", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.CheckAsync());
    }

    // ── UncheckAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UncheckAsync_TogglesElementOff()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.CheckBox("Accept", initialState: true))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:CheckBox", root);

        await locator.UncheckAsync();

        var cb = (FakeElementBackend)root.Children[0];
        Assert.Equal(false, cb.GetToggleState());
    }

    [Fact]
    public async Task UncheckAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:CheckBox", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.UncheckAsync());
    }

    // ── SetCheckedAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task SetCheckedAsync_True_TogglesOn()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.CheckBox("Accept", initialState: false))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:CheckBox", root);

        await locator.SetCheckedAsync(true);

        var cb = (FakeElementBackend)root.Children[0];
        Assert.Equal(true, cb.GetToggleState());
    }

    [Fact]
    public async Task SetCheckedAsync_False_TogglesOff()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.CheckBox("Accept", initialState: true))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:CheckBox", root);

        await locator.SetCheckedAsync(false);

        var cb = (FakeElementBackend)root.Children[0];
        Assert.Equal(false, cb.GetToggleState());
    }

    // ── SelectOptionAsync (string) ────────────────────────────────────────────

    [Fact]
    public async Task SelectOptionAsync_String_SelectsItemByName()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.List("Colors")
                .WithChild(UiaTree.ListItem("Red"))
                .WithChild(UiaTree.ListItem("Green"))
                .WithChild(UiaTree.ListItem("Blue")))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:List", root);

        await locator.SelectOptionAsync("Red");

        var list = (FakeElementBackend)root.Children[0];
        Assert.Equal("Red", list.LastSelectedItem);
    }

    [Fact]
    public async Task SelectOptionAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:List", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.SelectOptionAsync("item"));
    }

    // ── SelectOptionAsync (SelectOptionValue with Label) ──────────────────────

    [Fact]
    public async Task SelectOptionAsync_SelectOptionValue_Label_SelectsItem()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.List("Colors")
                .WithChild(UiaTree.ListItem("Blue")))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:List", root);

        await locator.SelectOptionAsync(new SelectOptionValue { Label = "Blue" });

        var list = (FakeElementBackend)root.Children[0];
        Assert.Equal("Blue", list.LastSelectedItem);
    }

    [Fact]
    public async Task SelectOptionAsync_SelectOptionValue_Value_SelectsItem()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.List("Sizes")
                .WithChild(UiaTree.ListItem("Large")))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:List", root);

        await locator.SelectOptionAsync(new SelectOptionValue { Value = "Large" });

        var list = (FakeElementBackend)root.Children[0];
        Assert.Equal("Large", list.LastSelectedItem);
    }

    [Fact]
    public async Task SelectOptionAsync_SelectOptionValue_NoFields_ThrowsArgumentException()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.List("Sizes"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:List", root);

        await Assert.ThrowsAsync<ArgumentException>(
            () => locator.SelectOptionAsync(new SelectOptionValue()));
    }

    [Fact]
    public async Task SelectOptionAsync_SelectOptionValue_Null_ThrowsArgumentNullException()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.List("Sizes"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:List", root);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => locator.SelectOptionAsync((SelectOptionValue)null!));
    }

    // ── HoverAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task HoverAsync_MovesMouseToElementCenter()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK").WithBounds(10, 20, 80, 30))
            .Build();
        var input = new FakeInputBackend();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root, input: input);

        await locator.HoverAsync();

        // Should have recorded a mouse move to element center
        Assert.NotEmpty(input.MouseMoves);
        var move = input.MouseMoves[0];
        Assert.Equal(50, move.X); // 10 + 80/2
        Assert.Equal(35, move.Y); // 20 + 30/2
    }

    [Fact]
    public async Task HoverAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.HoverAsync());
    }

    // ── FocusAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task FocusAsync_CallsFocusOnBackend()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Edit("Name").WithValue(""))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        await locator.FocusAsync();

        var edit = (FakeElementBackend)root.Children[0];
        Assert.Equal(1, edit.FocusCount);
    }

    [Fact]
    public async Task FocusAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.FocusAsync());
    }

    // ── BlurAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task BlurAsync_DoesNotThrow_WhenElementFound()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Edit("Name").WithValue(""))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        // Blur is a stub; should complete without throwing.
        await locator.BlurAsync();
    }

    [Fact]
    public async Task BlurAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.BlurAsync());
    }

    // ── ScrollIntoViewIfNeededAsync ───────────────────────────────────────────

    [Fact]
    public async Task ScrollIntoViewIfNeededAsync_ScrollsElement()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await locator.ScrollIntoViewIfNeededAsync();

        var button = (FakeElementBackend)root.Children[0];
        Assert.True(button.ScrolledIntoView);
    }

    [Fact]
    public async Task ScrollIntoViewIfNeededAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.ScrollIntoViewIfNeededAsync());
    }

    // ── DragToAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DragToAsync_RecordsMouseMoveDownMoveUp()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Source").WithBounds(0, 0, 20, 20))
            .WithChild(UiaTree.Button("Target").WithBounds(100, 100, 20, 20))
            .Build();
        var input = new FakeInputBackend();

        var sourceLocator = LocatorTestBase.CreateLocator("name:Source", root, input: input);
        var targetLocator = LocatorTestBase.CreateLocator("name:Target", root, input: input);

        await sourceLocator.DragToAsync(targetLocator);

        Assert.Equal(2, input.MouseMoves.Count);  // move to src, move to dst
        Assert.Single(input.MouseDowns);
        Assert.Single(input.MouseUps);
    }

    [Fact]
    public async Task DragToAsync_ThrowsArgumentNullException_WhenTargetIsNull()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Source"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<ArgumentNullException>(() => locator.DragToAsync(null!));
    }
}
