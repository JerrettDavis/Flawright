using JerrettDavis.Flawright.Locator;
using JerrettDavis.Flawright.UnitTests.Fakes;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Locator;

/// <summary>
/// Tests for <see cref="IFlawrightLocator.GetByTitle"/> — selector generation
/// and element resolution using Name property matching (UIA Title maps to Name).
/// </summary>
public sealed class GetByTitleTests
{
    // ── Selector string ───────────────────────────────────────────────────────

    [Fact]
    public void GetByTitle_ContainsTitleTextInSelector()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var titleLocator = locator.GetByTitle("Save Changes");
        Assert.Contains("Save Changes", titleLocator.Selector);
    }

    [Fact]
    public void GetByTitle_Exact_UsesEqualsOp()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var titleLocator = locator.GetByTitle("Help", new LocatorGetByTitleOptions { Exact = true });
        Assert.DoesNotContain("*=", titleLocator.Selector);
        Assert.Contains("Help", titleLocator.Selector);
    }

    [Fact]
    public void GetByTitle_NotExact_UsesContainsOp()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var titleLocator = locator.GetByTitle("Help", new LocatorGetByTitleOptions { Exact = false });
        Assert.Contains("*=", titleLocator.Selector);
        Assert.Contains("Help", titleLocator.Selector);
    }

    [Fact]
    public void GetByTitle_ThrowsArgumentNullException_WhenTextIsNull()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        Assert.Throws<ArgumentNullException>(() => locator.GetByTitle(null!));
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByTitle_Exact_FindsExactNameMatch()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Toolbar")
                .WithChild(UiaTree.Button("Help"))
                .WithChild(UiaTree.Button("Help Center")))
            .Build();

        var toolbar = LocatorTestBase.CreateLocator("name:Toolbar", root);
        var found = toolbar.GetByTitle("Help", new LocatorGetByTitleOptions { Exact = true });
        var count = await found.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByTitle_NotExact_FindsPartialMatches()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Toolbar")
                .WithChild(UiaTree.Button("Help"))
                .WithChild(UiaTree.Button("Help Center"))
                .WithChild(UiaTree.Button("About")))
            .Build();

        var toolbar = LocatorTestBase.CreateLocator("name:Toolbar", root);
        var found = toolbar.GetByTitle("Help", new LocatorGetByTitleOptions { Exact = false });
        var count = await found.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public void GetByTitle_ReturnsNewLocator()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        var titleLocator = locator.GetByTitle("About");
        Assert.NotSame(locator, titleLocator);
    }
}
