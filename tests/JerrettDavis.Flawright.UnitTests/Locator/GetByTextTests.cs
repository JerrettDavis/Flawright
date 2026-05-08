using JerrettDavis.Flawright.Locator;
using JerrettDavis.Flawright.UnitTests.Fakes;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Locator;

/// <summary>
/// Tests for <see cref="IFlawrightLocator.GetByText"/> — selector generation
/// and element resolution.
/// </summary>
public sealed class GetByTextTests
{
    // ── Selector string ───────────────────────────────────────────────────────

    [Fact]
    public void GetByText_ContainsTextInSelector()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var textLocator = locator.GetByText("Save");
        Assert.Contains("Save", textLocator.Selector);
    }

    [Fact]
    public void GetByText_Exact_UsesEqualsOp()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var textLocator = locator.GetByText("Save", new LocatorGetByTextOptions { Exact = true });
        // Exact: [name="Save"] — no star
        Assert.DoesNotContain("*=", textLocator.Selector);
        Assert.Contains("Save", textLocator.Selector);
    }

    [Fact]
    public void GetByText_NotExact_UsesContainsOp()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var textLocator = locator.GetByText("Save", new LocatorGetByTextOptions { Exact = false });
        // Not exact: [name*="Save"]
        Assert.Contains("*=", textLocator.Selector);
        Assert.Contains("Save", textLocator.Selector);
    }

    [Fact]
    public void GetByText_ThrowsArgumentNullException_WhenTextIsNull()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        Assert.Throws<ArgumentNullException>(() => locator.GetByText(null!));
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByText_Exact_FindsExactNameMatch()
    {
        // Container > Save button, Save Draft button
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.Button("Save"))
                .WithChild(UiaTree.Button("Save Draft")))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var found = container.GetByText("Save", new LocatorGetByTextOptions { Exact = true });
        var count = await found.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByText_NotExact_FindsPartialNameMatches()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.Button("Save"))
                .WithChild(UiaTree.Button("Save Draft"))
                .WithChild(UiaTree.Button("Delete")))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var found = container.GetByText("Save", new LocatorGetByTextOptions { Exact = false });
        var count = await found.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public void GetByText_ReturnsNewLocator()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        var textLocator = locator.GetByText("OK");
        Assert.NotSame(locator, textLocator);
    }
}
