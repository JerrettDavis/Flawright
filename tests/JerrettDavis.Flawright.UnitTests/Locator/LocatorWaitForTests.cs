using JerrettDavis.Flawright.Locator;
using JerrettDavis.Flawright.UnitTests.Fakes;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Locator;

/// <summary>
/// Tests for <see cref="IFlawrightLocator.WaitForAsync"/> covering all four
/// <see cref="WaitForState"/> values: Visible, Hidden, Attached, Detached.
/// </summary>
public sealed class LocatorWaitForTests
{
    // ── WaitForState.Visible ──────────────────────────────────────────────────

    [Fact]
    public async Task WaitForAsync_Visible_CompletesImmediately_WhenElementOnScreen()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        // Should not throw or timeout
        await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForState.Visible });
    }

    [Fact]
    public async Task WaitForAsync_Visible_ThrowsTimeout_WhenElementOffscreen()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK").AsOffscreen())
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForState.Visible }));
    }

    [Fact]
    public async Task WaitForAsync_Visible_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForState.Visible }));
    }

    // ── WaitForState.Hidden ───────────────────────────────────────────────────

    [Fact]
    public async Task WaitForAsync_Hidden_CompletesImmediately_WhenElementOffscreen()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK").AsOffscreen())
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        // Should not throw or timeout
        await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForState.Hidden });
    }

    [Fact]
    public async Task WaitForAsync_Hidden_CompletesImmediately_WhenNoElement()
    {
        // If no element is found at all, it is "hidden"
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        // Should not throw or timeout
        await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForState.Hidden });
    }

    [Fact]
    public async Task WaitForAsync_Hidden_ThrowsTimeout_WhenElementOnScreen()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForState.Hidden }));
    }

    // ── WaitForState.Attached ─────────────────────────────────────────────────

    [Fact]
    public async Task WaitForAsync_Attached_CompletesImmediately_WhenElementExists()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        // Should not throw — element is in the tree
        await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForState.Attached });
    }

    [Fact]
    public async Task WaitForAsync_Attached_CompletesImmediately_WhenOffscreenElement()
    {
        // Offscreen elements are still "attached" (in the tree)
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK").AsOffscreen())
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForState.Attached });
    }

    [Fact]
    public async Task WaitForAsync_Attached_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForState.Attached }));
    }

    // ── WaitForState.Detached ─────────────────────────────────────────────────

    [Fact]
    public async Task WaitForAsync_Detached_CompletesImmediately_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        // No button in tree — immediately detached
        await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForState.Detached });
    }

    [Fact]
    public async Task WaitForAsync_Detached_ThrowsTimeout_WhenElementExists()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForState.Detached }));
    }

    // ── Default state (no options) ────────────────────────────────────────────

    [Fact]
    public async Task WaitForAsync_DefaultState_IsVisible()
    {
        // Default WaitForState is Visible
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        // Should not throw — default state is Visible, element is on-screen
        await locator.WaitForAsync();
    }

    [Fact]
    public async Task WaitForAsync_NoOptions_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.WaitForAsync());
    }
}
