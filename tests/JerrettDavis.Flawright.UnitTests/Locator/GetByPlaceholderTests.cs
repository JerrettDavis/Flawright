using JerrettDavis.Flawright.Locator;
using JerrettDavis.Flawright.UnitTests.Fakes;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Locator;

/// <summary>
/// Tests for <see cref="IFlawrightLocator.GetByPlaceholder"/> — selector generation
/// and element resolution using Name property matching (desktop UIA placeholder mapping).
/// </summary>
public sealed class GetByPlaceholderTests
{
    // ── Selector string ───────────────────────────────────────────────────────

    [Fact]
    public void GetByPlaceholder_ContainsTextInSelector()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        var placeholderLocator = locator.GetByPlaceholder("Enter name");
        Assert.Contains("Enter name", placeholderLocator.Selector);
    }

    [Fact]
    public void GetByPlaceholder_Exact_UsesEqualsOp()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        var placeholderLocator = locator.GetByPlaceholder("Search", new LocatorGetByPlaceholderOptions { Exact = true });
        Assert.DoesNotContain("*=", placeholderLocator.Selector);
        Assert.Contains("Search", placeholderLocator.Selector);
    }

    [Fact]
    public void GetByPlaceholder_NotExact_UsesContainsOp()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        var placeholderLocator = locator.GetByPlaceholder("Search", new LocatorGetByPlaceholderOptions { Exact = false });
        Assert.Contains("*=", placeholderLocator.Selector);
    }

    [Fact]
    public void GetByPlaceholder_ThrowsArgumentNullException_WhenTextIsNull()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        Assert.Throws<ArgumentNullException>(() => locator.GetByPlaceholder(null!));
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPlaceholder_Exact_FindsElementByName()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("SearchPanel")
                .WithChild(UiaTree.Edit("Search here").WithValue(""))
                .WithChild(UiaTree.Edit("Search more").WithValue("")))
            .Build();

        var panel = LocatorTestBase.CreateLocator("name:SearchPanel", root);
        var found = panel.GetByPlaceholder("Search here", new LocatorGetByPlaceholderOptions { Exact = true });
        var count = await found.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByPlaceholder_NotExact_FindsPartialMatches()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("SearchPanel")
                .WithChild(UiaTree.Edit("Search here").WithValue(""))
                .WithChild(UiaTree.Edit("Search more").WithValue(""))
                .WithChild(UiaTree.Edit("Enter value").WithValue("")))
            .Build();

        var panel = LocatorTestBase.CreateLocator("name:SearchPanel", root);
        var found = panel.GetByPlaceholder("Search", new LocatorGetByPlaceholderOptions { Exact = false });
        var count = await found.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public void GetByPlaceholder_ReturnsNewLocator()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);
        var placeholderLocator = locator.GetByPlaceholder("hint");
        Assert.NotSame(locator, placeholderLocator);
    }
}
