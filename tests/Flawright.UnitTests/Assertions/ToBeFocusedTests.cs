using Flawright.Assertions;
using Flawright.UnitTests.Fakes;
using Flawright.UnitTests.Locator;
using Xunit;

namespace Flawright.UnitTests.Assertions;

/// <summary>
/// Unit tests for <see cref="IFlawrightAssertions.ToBeFocusedAsync"/> and
/// <see cref="IFlawrightNotAssertions.ToBeFocusedAsync"/> using real
/// <see cref="FlawrightLocator"/> + <see cref="FakeElementBackend"/> so the
/// <c>HasKeyboardFocus</c> code path is fully exercised.
/// </summary>
public sealed class ToBeFocusedTests
{
    private static readonly FlawrightOptions QuickOptions = new()
    {
        DefaultTimeout = TimeSpan.FromMilliseconds(200),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(10),
    };

    // ── ToBeFocusedAsync passes when HasKeyboardFocus is true ─────────────────

    [Fact]
    public async Task ToBeFocusedAsync_Passes_WhenElementHasFocus()
    {
        var btn = new FakeElementBackend(
            name: "FocusTarget",
            controlTypeName: "Button",
            boundingRectangle: new System.Drawing.Rectangle(0, 0, 100, 30))
        {
            HasKeyboardFocus = true,
        };
        var root = UiaTree.Window("App").Build();
        root.AddChild(btn);

        var locator = LocatorTestBase.CreateLocator("controltype:Button", root, options: QuickOptions);

        // Should pass without throwing.
        await locator.Expect().ToBeFocusedAsync();
    }

    // ── ToBeFocusedAsync waits and times out when HasKeyboardFocus remains false

    [Fact]
    public async Task ToBeFocusedAsync_TimesOut_WhenElementNeverFocused()
    {
        var btn = new FakeElementBackend(
            name: "Unfocused",
            controlTypeName: "Button",
            boundingRectangle: new System.Drawing.Rectangle(0, 0, 100, 30))
        {
            HasKeyboardFocus = false,
        };
        var root = UiaTree.Window("App").Build();
        root.AddChild(btn);

        var locator = LocatorTestBase.CreateLocator("controltype:Button", root, options: QuickOptions);

        await Assert.ThrowsAsync<AssertionException>(
            () => locator.Expect().ToBeFocusedAsync(
                new AssertionsToBeFocusedOptions { Timeout = TimeSpan.FromMilliseconds(80) }));
    }

    // ── Not.ToBeFocusedAsync passes when HasKeyboardFocus is false ────────────

    [Fact]
    public async Task Not_ToBeFocusedAsync_Passes_WhenElementNotFocused()
    {
        var btn = new FakeElementBackend(
            name: "Unfocused",
            controlTypeName: "Button",
            boundingRectangle: new System.Drawing.Rectangle(0, 0, 100, 30))
        {
            HasKeyboardFocus = false,
        };
        var root = UiaTree.Window("App").Build();
        root.AddChild(btn);

        var locator = LocatorTestBase.CreateLocator("controltype:Button", root, options: QuickOptions);

        // Should pass without throwing.
        await locator.Expect().Not.ToBeFocusedAsync();
    }

    // ── Not.ToBeFocusedAsync fails when element IS focused ────────────────────

    [Fact]
    public async Task Not_ToBeFocusedAsync_Fails_WhenElementIsFocused()
    {
        var btn = new FakeElementBackend(
            name: "FocusTarget",
            controlTypeName: "Button",
            boundingRectangle: new System.Drawing.Rectangle(0, 0, 100, 30))
        {
            HasKeyboardFocus = true,
        };
        var root = UiaTree.Window("App").Build();
        root.AddChild(btn);

        var locator = LocatorTestBase.CreateLocator("controltype:Button", root, options: QuickOptions);

        await Assert.ThrowsAsync<AssertionException>(
            () => locator.Expect().Not.ToBeFocusedAsync(
                new AssertionsToBeFocusedOptions { Timeout = TimeSpan.FromMilliseconds(80) }));
    }
}
