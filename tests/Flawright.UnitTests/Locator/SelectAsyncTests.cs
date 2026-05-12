using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Locator;

/// <summary>
/// Unit tests for <see cref="IFlawrightLocator.SelectAsync"/>.
/// </summary>
public sealed class SelectAsyncTests
{
    // ── Routes to TrySelect on backend ────────────────────────────────────────

    [Fact]
    public async Task SelectAsync_routes_to_TrySelect_on_backend()
    {
        // Arrange: a TabItem-like element that supports SelectionItemPattern
        var tabItem = new FakeElementBackend(
            name: "Selection",
            automationId: "tabSelection",
            controlTypeName: "TabItem",
            boundingRectangle: new System.Drawing.Rectangle(0, 0, 80, 25))
        {
            TrySelectResult = true,
        };

        var root = UiaTree.Window("App").Build();
        root.AddChild(tabItem);

        var locator = LocatorTestBase.CreateLocator("#tabSelection", root);

        // Act
        await locator.SelectAsync();

        // Assert: TrySelect was called and recorded
        Assert.True(tabItem.WasSelected, "TrySelect should have been called on the backend.");
    }

    // ── Throws NotSupportedException when TrySelect returns false ─────────────

    [Fact]
    public async Task SelectAsync_throws_when_backend_returns_false()
    {
        // Arrange: a Button that does NOT support SelectionItemPattern
        var button = UiaTree.Button("NoSelect")
            .WithAutomationId("btnNoSelect")
            .WithBounds(0, 0, 100, 30)
            .Build();

        var root = UiaTree.Window("App").Build();
        root.AddChild(button);

        var locator = LocatorTestBase.CreateLocator("#btnNoSelect", root);

        // Act & Assert: TrySelect returns false → NotSupportedException
        await Assert.ThrowsAsync<NotSupportedException>(() => locator.SelectAsync());
    }

    // ── Auto-waits until the element resolves ─────────────────────────────────

    [Fact]
    public async Task SelectAsync_auto_waits_until_element_resolves()
    {
        // Arrange: the element is initially absent; add it mid-wait
        var root = UiaTree.Window("App").Build();

        var locator = LocatorTestBase.CreateLocator("#tabLateAppear", root);

        // Start the SelectAsync in the background; it should auto-wait
        var selectTask = locator.SelectAsync();

        // Verify it hasn't completed immediately (element not yet present)
        await Task.Delay(20);
        Assert.False(selectTask.IsCompleted, "SelectAsync should be waiting for the element.");

        // Now add the element
        var tabItem = new FakeElementBackend(
            name: "LateAppear",
            automationId: "tabLateAppear",
            controlTypeName: "TabItem",
            boundingRectangle: new System.Drawing.Rectangle(0, 0, 80, 25))
        {
            TrySelectResult = true,
        };
        root.AddChild(tabItem);

        // SelectAsync should now complete
        await selectTask;

        Assert.True(tabItem.WasSelected, "TrySelect should have been called after the element appeared.");
    }
}
