using JerrettDavis.Flawright.UnitTests.Fakes;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Locator;

/// <summary>
/// Tests for basic <see cref="FlawrightLocator"/> construction and identity.
/// No I/O paths — pure synchronous selector/property verification.
/// </summary>
public sealed class FlawrightLocatorTests
{
    // ── Selector property ─────────────────────────────────────────────────────

    [Fact]
    public void Selector_ReturnsExactString()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("#btn_ok", root);
        Assert.Equal("#btn_ok", locator.Selector);
    }

    [Fact]
    public void Selector_WithComplexSelector_ReturnsExact()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("[role=Button]", root);
        Assert.Equal("[role=Button]", locator.Selector);
    }

    // ── CountAsync without auto-wait ──────────────────────────────────────────

    [Fact]
    public async Task CountAsync_ReturnsZero_WhenNoMatch()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("NoSuchElement", root);
        var count = await locator.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task CountAsync_ReturnsFour_WhenFourButtonsExist()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("B1"))
            .WithChild(UiaTree.Button("B2"))
            .WithChild(UiaTree.Button("B3"))
            .WithChild(UiaTree.Button("B4"))
            .Build();

        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        var count = await locator.CountAsync();
        Assert.Equal(4, count);
    }

    [Fact]
    public async Task CountAsync_DoesNotThrow_WhenSelectorMatchesNothing()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("#doesNotExist", root);
        // CountAsync per Playwright: returns 0 immediately without throwing
        var count = await locator.CountAsync();
        Assert.Equal(0, count);
    }

    // ── Expect returns assertions ─────────────────────────────────────────────

    [Fact]
    public void Expect_ReturnsNonNull_IFlawrightAssertions()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("#ok", root);
        var assertions = locator.Expect();
        Assert.NotNull(assertions);
    }

    [Fact]
    public void Expect_Not_ReturnsNonNull()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("#ok", root);
        var notAssertions = locator.Expect().Not;
        Assert.NotNull(notAssertions);
    }

    // ── AllInnerTextsAsync / AllTextContentsAsync ─────────────────────────────

    [Fact]
    public async Task AllInnerTextsAsync_ReturnsTextsForAllMatches()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Alpha"))
            .WithChild(UiaTree.Button("Beta"))
            .Build();

        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        var texts = await locator.AllInnerTextsAsync();

        Assert.Equal(2, texts.Count);
        Assert.Contains("Alpha", texts);
        Assert.Contains("Beta", texts);
    }

    [Fact]
    public async Task AllTextContentsAsync_ReturnsContentForAllMatches()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Alpha"))
            .WithChild(UiaTree.Button("Beta"))
            .Build();

        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        var texts = await locator.AllTextContentsAsync();

        Assert.Equal(2, texts.Count);
    }

    // ── IsVisible / IsHidden fast-path ────────────────────────────────────────

    [Fact]
    public async Task IsVisibleAsync_ReturnsFalse_WhenNotFound()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("NoSuchElement", root);
        // Should not throw; should return false
        var visible = await locator.IsVisibleAsync();
        Assert.False(visible);
    }

    [Fact]
    public async Task IsHiddenAsync_ReturnsTrue_WhenNotFound()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("NoSuchElement", root);
        var hidden = await locator.IsHiddenAsync();
        Assert.True(hidden);
    }

    [Fact]
    public async Task IsVisibleAsync_ReturnsTrue_WhenElementOnscreen()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        var visible = await locator.IsVisibleAsync();
        Assert.True(visible);
    }

    [Fact]
    public async Task IsHiddenAsync_ReturnsTrue_WhenElementOffscreen()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK").AsOffscreen())
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        var hidden = await locator.IsHiddenAsync();
        Assert.True(hidden);
    }

    // ── ScreenshotAsync / HighlightAsync stubs ────────────────────────────────

    [Fact]
    public async Task ScreenshotAsync_ReturnsEmptyArray_Stub()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        // No element — but stub returns empty before even trying to resolve
        var result = await locator.ScreenshotAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task HighlightAsync_DoesNotThrow_Stub()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("#anything", root);
        // Stub — should complete without throwing
        await locator.HighlightAsync();
    }

    // ── ElementHandleAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ElementHandleAsync_ReturnsElement_WhenFound()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
#pragma warning disable CS0618
        var handle = await locator.ElementHandleAsync();
#pragma warning restore CS0618
        Assert.NotNull(handle);
        Assert.Equal("Button", handle.ControlTypeName);
    }

    [Fact]
    public async Task ElementHandleAsync_ThrowsTimeout_WhenNotFound()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
#pragma warning disable CS0618
        await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => locator.ElementHandleAsync());
#pragma warning restore CS0618
    }
}
