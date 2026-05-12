using Flawright.Backends;
using Flawright.InputModes;
using Flawright.Locator;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Locator;

/// <summary>
/// Unit tests for <see cref="IFlawrightLocator.RightClickAsync"/>.
/// </summary>
public sealed class RightClickTests
{
    // ── RightClickAsync routes to MouseClick with MouseButton.Right ───────────

    [Fact]
    public async Task RightClickAsync_RoutesToMouseClickWithRightButton()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Target").WithBounds(100, 200, 80, 30))
            .Build();
        var input = new FakeInputBackend();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root, input);

        await locator.RightClickAsync();

        Assert.Single(input.MouseClicks);
        Assert.Equal(MouseButton.Right, input.MouseClicks[0].Button);
    }

    [Fact]
    public async Task RightClickAsync_DefaultOptions_UsesElementCenter()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Target").WithBounds(100, 200, 80, 30))
            .Build();
        var input = new FakeInputBackend();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root, input);

        await locator.RightClickAsync();

        // Center = (100 + 80/2, 200 + 30/2) = (140, 215)
        Assert.Equal(140, input.MouseClicks[0].X);
        Assert.Equal(215, input.MouseClicks[0].Y);
    }

    // ── RightClickAsync with Modifiers presses+releases them ─────────────────

    [Fact]
    public async Task RightClickAsync_WithModifiers_PressesAndReleasesModifiers()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Target").WithBounds(0, 0, 100, 30))
            .Build();
        var input = new FakeInputBackend();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root, input);

        await locator.RightClickAsync(new LocatorClickOptions { Modifiers = KeyModifiers.Shift });

        // Shift must be pressed before the click and released after.
        Assert.Contains(FlaUI.Core.WindowsAPI.VirtualKeyShort.SHIFT, input.KeyPresses);
        Assert.Contains(FlaUI.Core.WindowsAPI.VirtualKeyShort.SHIFT, input.KeyReleases);
        // Click must be recorded with the Right button.
        Assert.Single(input.MouseClicks);
        Assert.Equal(MouseButton.Right, input.MouseClicks[0].Button);
    }

    // ── RightClickAsync with Position offset uses computed coords ─────────────

    [Fact]
    public async Task RightClickAsync_WithPosition_UsesOffsetCoords()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Target").WithBounds(50, 60, 200, 100))
            .Build();
        var input = new FakeInputBackend();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root, input);

        // Position(10, 20) relative to bounding box (50,60) → absolute (60, 80)
        await locator.RightClickAsync(new LocatorClickOptions { Position = new BoundingBox(10, 20, 0, 0) });

        Assert.Single(input.MouseClicks);
        Assert.Equal(60, input.MouseClicks[0].X);
        Assert.Equal(80, input.MouseClicks[0].Y);
        Assert.Equal(MouseButton.Right, input.MouseClicks[0].Button);
    }

    // ── VirtualInputMode throws NotSupportedException ─────────────────────────

    [Fact]
    public async Task RightClickAsync_VirtualInputMode_ThrowsNotSupportedException()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Target").WithBounds(0, 0, 100, 30))
            .Build();
        var input = new FakeInputBackend();
        var locator = LocatorTestBase.CreateLocator(
            "controltype:Button",
            root,
            input,
            inputMode: new VirtualInputMode());

        // VirtualInputMode.Click throws when button != Left.
        await Assert.ThrowsAsync<NotSupportedException>(() => locator.RightClickAsync());
    }
}
