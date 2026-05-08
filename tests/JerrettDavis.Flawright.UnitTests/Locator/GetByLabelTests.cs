using JerrettDavis.Flawright.Locator;
using JerrettDavis.Flawright.UnitTests.Fakes;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Locator;

/// <summary>
/// Tests for <see cref="IFlawrightLocator.GetByLabel"/> — selector generation
/// and element resolution using Name property matching.
/// </summary>
public sealed class GetByLabelTests
{
    // ── Selector string ───────────────────────────────────────────────name────

    [Fact]
    public void GetByLabel_ContainsLabelTextInSelector()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        var labelLocator = locator.GetByLabel("Username");
        Assert.Contains("Username", labelLocator.Selector);
    }

    [Fact]
    public void GetByLabel_Exact_UsesEqualsOp()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        var labelLocator = locator.GetByLabel("Username", new LocatorGetByLabelOptions { Exact = true });
        Assert.DoesNotContain("*=", labelLocator.Selector);
        Assert.Contains("Username", labelLocator.Selector);
    }

    [Fact]
    public void GetByLabel_NotExact_UsesContainsOp()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        var labelLocator = locator.GetByLabel("Username", new LocatorGetByLabelOptions { Exact = false });
        Assert.Contains("*=", labelLocator.Selector);
        Assert.Contains("Username", labelLocator.Selector);
    }

    [Fact]
    public void GetByLabel_ThrowsArgumentNullException_WhenTextIsNull()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        Assert.Throws<ArgumentNullException>(() => locator.GetByLabel(null!));
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByLabel_Exact_FindsExactNameMatch()
    {
        // Form > Username edit, Username Extended edit
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Form")
                .WithChild(UiaTree.Edit("Username").WithValue(""))
                .WithChild(UiaTree.Edit("Username Extended").WithValue("")))
            .Build();

        var form = LocatorTestBase.CreateLocator("name:Form", root);
        var found = form.GetByLabel("Username", new LocatorGetByLabelOptions { Exact = true });
        var count = await found.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByLabel_NotExact_FindsPartialMatches()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Form")
                .WithChild(UiaTree.Edit("Username").WithValue(""))
                .WithChild(UiaTree.Edit("Username Extended").WithValue(""))
                .WithChild(UiaTree.Edit("Password").WithValue("")))
            .Build();

        var form = LocatorTestBase.CreateLocator("name:Form", root);
        var found = form.GetByLabel("Username", new LocatorGetByLabelOptions { Exact = false });
        var count = await found.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public void GetByLabel_ReturnsNewLocator()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);
        var labelLocator = locator.GetByLabel("Username");
        Assert.NotSame(locator, labelLocator);
    }
}
