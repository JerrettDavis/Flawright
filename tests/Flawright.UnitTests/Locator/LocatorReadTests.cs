using System.Drawing;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Locator;

/// <summary>
/// Tests for async read methods on <see cref="FlawrightLocator"/>:
/// IsEnabled, IsDisabled, IsChecked, IsEditable, InnerText, TextContent,
/// InputValue, GetAttribute, BoundingBox.
/// </summary>
public sealed class LocatorReadTests
{
    // ── IsEnabledAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task IsEnabledAsync_ReturnsTrue_WhenEnabled()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var enabled = await locator.IsEnabledAsync();
        Assert.True(enabled);
    }

    [Fact]
    public async Task IsEnabledAsync_ReturnsFalse_WhenDisabled()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK").AsDisabled())
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var enabled = await locator.IsEnabledAsync();
        Assert.False(enabled);
    }

    [Fact]
    public async Task IsEnabledAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.IsEnabledAsync());
    }

    // ── IsDisabledAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task IsDisabledAsync_ReturnsFalse_WhenEnabled()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var disabled = await locator.IsDisabledAsync();
        Assert.False(disabled);
    }

    [Fact]
    public async Task IsDisabledAsync_ReturnsTrue_WhenDisabled()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK").AsDisabled())
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var disabled = await locator.IsDisabledAsync();
        Assert.True(disabled);
    }

    [Fact]
    public async Task IsDisabledAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.IsDisabledAsync());
    }

    // ── IsCheckedAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task IsCheckedAsync_ReturnsTrue_WhenChecked()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.CheckBox("Accept", initialState: true))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:CheckBox", root);

        var isChecked = await locator.IsCheckedAsync();
        Assert.True(isChecked);
    }

    [Fact]
    public async Task IsCheckedAsync_ReturnsFalse_WhenUnchecked()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.CheckBox("Accept", initialState: false))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:CheckBox", root);

        var isChecked = await locator.IsCheckedAsync();
        Assert.False(isChecked);
    }

    [Fact]
    public async Task IsCheckedAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:CheckBox", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.IsCheckedAsync());
    }

    // ── IsEditableAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task IsEditableAsync_ReturnsTrue_WhenEditableAndEnabled()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Edit("Name").WithValue(""))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        var editable = await locator.IsEditableAsync();
        Assert.True(editable);
    }

    [Fact]
    public async Task IsEditableAsync_ReturnsFalse_WhenDisabled()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Edit("Name").WithValue("").AsDisabled())
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        var editable = await locator.IsEditableAsync();
        Assert.False(editable);
    }

    [Fact]
    public async Task IsEditableAsync_ReturnsFalse_WhenNoValuePattern()
    {
        // A Button does not support ValuePattern in fake (no initial value set)
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var editable = await locator.IsEditableAsync();
        Assert.False(editable);
    }

    [Fact]
    public async Task IsEditableAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.IsEditableAsync());
    }

    // ── InnerTextAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task InnerTextAsync_ReturnsNameOfElement()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Save"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var text = await locator.InnerTextAsync();
        Assert.Equal("Save", text);
    }

    [Fact]
    public async Task InnerTextAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.InnerTextAsync());
    }

    // ── TextContentAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task TextContentAsync_ReturnsText_WhenFound()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("Cancel"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var text = await locator.TextContentAsync();
        Assert.NotNull(text);
    }

    [Fact]
    public async Task TextContentAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.TextContentAsync());
    }

    // ── InputValueAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task InputValueAsync_ReturnsCurrentValue()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Edit("Username").WithValue("jdoe"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        var value = await locator.InputValueAsync();
        Assert.Equal("jdoe", value);
    }

    [Fact]
    public async Task InputValueAsync_ThrowsInvalidOperationException_WhenNoValuePattern()
    {
        // Button has no ValuePattern — FlawrightElement throws for non-input elements
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<InvalidOperationException>(() => locator.InputValueAsync());
    }

    [Fact]
    public async Task InputValueAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Edit", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.InputValueAsync());
    }

    // ── GetAttributeAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAttributeAsync_AutomationId_ReturnsValue()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK").WithAutomationId("btn_ok"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var attr = await locator.GetAttributeAsync("AutomationId");
        Assert.Equal("btn_ok", attr);
    }

    [Fact]
    public async Task GetAttributeAsync_UnknownAttribute_ReturnsNull()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var attr = await locator.GetAttributeAsync("NonExistentAttribute");
        Assert.Null(attr);
    }

    [Fact]
    public async Task GetAttributeAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.GetAttributeAsync("AutomationId"));
    }

    // ── BoundingBoxAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task BoundingBoxAsync_ReturnsCorrectBounds()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK").WithBounds(10, 20, 100, 50))
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var box = await locator.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.Equal(10, box.X);
        Assert.Equal(20, box.Y);
        Assert.Equal(100, box.Width);
        Assert.Equal(50, box.Height);
    }

    [Fact]
    public async Task BoundingBoxAsync_ReturnsNull_WhenNoBounds()
    {
        // Default bounds are 0,0,0,0 — treated as null by FlawrightElement
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK")) // No bounds set
            .Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        var box = await locator.BoundingBoxAsync();
        Assert.Null(box);
    }

    [Fact]
    public async Task BoundingBoxAsync_ThrowsTimeout_WhenNoElement()
    {
        var root = UiaTree.Window("App").Build();
        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(() => locator.BoundingBoxAsync());
    }
}
