using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Locator;

/// <summary>
/// Tests for sync chaining: <c>First</c>, <c>Last</c>, <c>Nth</c>,
/// <c>Locator(string)</c>, <c>Locator(IFlawrightLocator)</c>,
/// <c>And</c>, <c>Or</c>.
/// All tests are pure sync (no I/O) unless they exercise resolution.
/// </summary>
public sealed class LocatorChainingTests
{
    // ── First property ────────────────────────────────────────────────────────

    [Fact]
    public void First_ReturnsNewLocator()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        var first = locator.First;
        Assert.NotSame(locator, first);
    }

    [Fact]
    public void First_SelectorContainsParent()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        var first = locator.First;
        // The selector should still be the original selector; indexing is internal.
        Assert.Equal("controltype:Button", first.Selector);
    }

    [Fact]
    public async Task First_ResolvesToFirstElement()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Alpha"))
            .WithChild(UiaTree.Button("Beta"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
#pragma warning disable CS0618
        var handle = await locator.First.ElementHandleAsync();
#pragma warning restore CS0618
        Assert.Equal("Alpha", handle.Name);
    }

    // ── Last property ─────────────────────────────────────────────────────────

    [Fact]
    public void Last_ReturnsNewLocator()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        var last = locator.Last;
        Assert.NotSame(locator, last);
    }

    [Fact]
    public async Task Last_ResolvesToLastElement()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Alpha"))
            .WithChild(UiaTree.Button("Beta"))
            .WithChild(UiaTree.Button("Gamma"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
#pragma warning disable CS0618
        var handle = await locator.Last.ElementHandleAsync();
#pragma warning restore CS0618
        Assert.Equal("Gamma", handle.Name);
    }

    // ── Nth method ────────────────────────────────────────────────────────────

    [Fact]
    public void Nth_ReturnsNewLocator()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        var nth = locator.Nth(1);
        Assert.NotSame(locator, nth);
    }

    [Fact]
    public async Task Nth_0_ResolvesToFirstElement()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Alpha"))
            .WithChild(UiaTree.Button("Beta"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
#pragma warning disable CS0618
        var handle = await locator.Nth(0).ElementHandleAsync();
#pragma warning restore CS0618
        Assert.Equal("Alpha", handle.Name);
    }

    [Fact]
    public async Task Nth_1_ResolvesToSecondElement()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Alpha"))
            .WithChild(UiaTree.Button("Beta"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
#pragma warning disable CS0618
        var handle = await locator.Nth(1).ElementHandleAsync();
#pragma warning restore CS0618
        Assert.Equal("Beta", handle.Name);
    }

    [Fact]
    public async Task Nth_OutOfRange_ThrowsTimeout()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Alpha"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        await Assert.ThrowsAsync<FlawrightTimeoutException>(
#pragma warning disable CS0618
            () => locator.Nth(99).ElementHandleAsync());
#pragma warning restore CS0618
    }

    // ── Locator(string) chaining ──────────────────────────────────────────────

    [Fact]
    public void Locator_String_ReturnsScopedLocator()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.List("Items")
                .WithChild(UiaTree.ListItem("Item A"))
                .WithChild(UiaTree.ListItem("Item B")))
            .Build();

        var listLocator = LocatorTestBase.CreateLocator("controltype:List", root);
        var itemLocator = listLocator.Locator("controltype:ListItem");

        Assert.Contains("ListItem", itemLocator.Selector);
    }

    [Fact]
    public async Task Locator_String_FindsChildElements()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.List("Items")
                .WithChild(UiaTree.ListItem("Item A"))
                .WithChild(UiaTree.ListItem("Item B")))
            .Build();

        var locator = LocatorTestBase.CreateLocator("controltype:List", root);
        var itemLocator = locator.Locator("controltype:ListItem");
        var count = await itemLocator.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public void Locator_String_ThrowsArgumentNullException_WhenSelectorIsNull()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("#ok", root);
        Assert.Throws<ArgumentNullException>(() => locator.Locator((string)null!));
    }

    // ── Locator(IFlawrightLocator) chaining ───────────────────────────────────

    [Fact]
    public void Locator_Locator_UsesSelectorFromInner()
    {
        var root = UiaTree.Window("App").Build();
        var outer = LocatorTestBase.CreateLocator("controltype:List", root);
        var inner = LocatorTestBase.CreateLocator("controltype:ListItem", root);
        var scoped = outer.Locator(inner);
        Assert.Contains("ListItem", scoped.Selector);
    }

    // ── And composition ───────────────────────────────────────────────────────

    [Fact]
    public void And_ReturnsNewLocator()
    {
        var root = UiaTree.Window("App").Build();
        var l1 = LocatorTestBase.CreateLocator("controltype:Button", root);
        var l2 = LocatorTestBase.CreateLocator("controltype:Button", root);
        var combined = l1.And(l2);
        Assert.NotSame(l1, combined);
    }

    [Fact]
    public void And_ThrowsArgumentNullException_WhenOtherIsNull()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        Assert.Throws<ArgumentNullException>(() => locator.And(null!));
    }

    // ── Or composition ────────────────────────────────────────────────────────

    [Fact]
    public void Or_ReturnsNewLocator()
    {
        var root = UiaTree.Window("App").Build();
        var l1 = LocatorTestBase.CreateLocator("controltype:Button", root);
        var l2 = LocatorTestBase.CreateLocator("controltype:Edit", root);
        var combined = l1.Or(l2);
        Assert.NotSame(l1, combined);
    }

    [Fact]
    public void Or_ThrowsArgumentNullException_WhenOtherIsNull()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        Assert.Throws<ArgumentNullException>(() => locator.Or(null!));
    }
}
