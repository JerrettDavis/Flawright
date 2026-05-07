using JerrettDavis.Flawright.UnitTests.Fakes;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Locator;

/// <summary>
/// Tests for <see cref="IFlawrightLocator.GetByTestId"/> — selector generation
/// and element resolution using AutomationId matching.
/// </summary>
public sealed class GetByTestIdTests
{
    // ── Selector string ───────────────────────────────────────────────────────

    [Fact]
    public void GetByTestId_SelectorContainsTestId()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var testIdLocator = locator.GetByTestId("submit-btn");
        Assert.Contains("submit-btn", testIdLocator.Selector);
    }

    [Fact]
    public void GetByTestId_SelectorUsesHashSyntax()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var testIdLocator = locator.GetByTestId("my-button");
        // GetByTestId uses `#testId` which maps to AutomationId matching
        Assert.Contains("#my-button", testIdLocator.Selector);
    }

    [Fact]
    public void GetByTestId_ThrowsArgumentNullException_WhenTestIdIsNull()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        Assert.Throws<ArgumentNullException>(() => locator.GetByTestId(null!));
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByTestId_FindsElementByAutomationId()
    {
        // Container > Button(btn-ok), Button(btn-cancel)
        // GetByTestId("btn-ok") scoped under Container finds the right button.
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.Button("OK").WithAutomationId("btn-ok"))
                .WithChild(UiaTree.Button("Cancel").WithAutomationId("btn-cancel")))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var found = container.GetByTestId("btn-ok");
        var count = await found.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByTestId_ReturnsZero_WhenNotFound()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Pane("Container")
                .WithChild(UiaTree.Button("OK").WithAutomationId("btn-ok")))
            .Build();

        var container = LocatorTestBase.CreateLocator("name:Container", root);
        var found = container.GetByTestId("nonexistent-id");
        var count = await found.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public void GetByTestId_ReturnsNewLocator()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);
        var testIdLocator = locator.GetByTestId("some-id");
        Assert.NotSame(locator, testIdLocator);
    }
}
