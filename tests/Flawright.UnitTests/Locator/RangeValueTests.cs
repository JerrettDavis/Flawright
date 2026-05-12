using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Locator;

/// <summary>
/// Unit tests for <see cref="IFlawrightLocator.GetValueAsync"/> and
/// <see cref="IFlawrightLocator.SetValueAsync"/> (RangeValuePattern surface).
/// </summary>
public sealed class RangeValueTests
{
    // ── SetValueAsync calls TrySetRangeValue on backend with the value ─────────

    [Fact]
    public async Task SetValueAsync_CallsTrySetRangeValueOnBackend()
    {
        var slider = new FakeElementBackend(
            name: "Volume",
            controlTypeName: "Slider",
            supportsRangeValue: true,
            initialRangeValue: 50.0,
            boundingRectangle: new System.Drawing.Rectangle(0, 0, 200, 20));

        var root = UiaTree.Window("App")
            .Build();
        root.AddChild(slider);

        var locator = LocatorTestBase.CreateLocator("controltype:Slider", root);

        await locator.SetValueAsync(75.0);

        Assert.Equal(75.0, slider.LastRangeValueSet);
    }

    // ── GetValueAsync returns TryGetRangeValue's value ────────────────────────

    [Fact]
    public async Task GetValueAsync_ReturnsTryGetRangeValue()
    {
        var slider = new FakeElementBackend(
            name: "Volume",
            controlTypeName: "Slider",
            supportsRangeValue: true,
            initialRangeValue: 42.0,
            boundingRectangle: new System.Drawing.Rectangle(0, 0, 200, 20));

        var root = UiaTree.Window("App")
            .Build();
        root.AddChild(slider);

        var locator = LocatorTestBase.CreateLocator("controltype:Slider", root);

        var value = await locator.GetValueAsync();

        Assert.Equal(42.0, value);
    }

    // ── TrySetRangeValue returns false when pattern is absent ─────────────────

    [Fact]
    public async Task SetValueAsync_ThrowsNotSupported_WhenRangePatternAbsent()
    {
        // supportsRangeValue is false (default) → TrySetRangeValue returns false
        var button = UiaTree.Button("NoSlider")
            .WithBounds(0, 0, 100, 30)
            .Build();
        var root = UiaTree.Window("App")
            .Build();
        root.AddChild(button);

        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<NotSupportedException>(() => locator.SetValueAsync(10.0));
    }

    // ── GetValueAsync throws when pattern is absent ───────────────────────────

    [Fact]
    public async Task GetValueAsync_ThrowsNotSupported_WhenRangePatternAbsent()
    {
        var button = UiaTree.Button("NoSlider")
            .WithBounds(0, 0, 100, 30)
            .Build();
        var root = UiaTree.Window("App")
            .Build();
        root.AddChild(button);

        var locator = LocatorTestBase.CreateLocator("controltype:Button", root);

        await Assert.ThrowsAsync<NotSupportedException>(() => locator.GetValueAsync());
    }

    // ── SetValueAsync after GetValueAsync reflects the new value ──────────────

    [Fact]
    public async Task SetValueAsync_ThenGetValueAsync_ReturnsUpdatedValue()
    {
        var slider = new FakeElementBackend(
            name: "Brightness",
            controlTypeName: "Slider",
            supportsRangeValue: true,
            initialRangeValue: 0.0,
            boundingRectangle: new System.Drawing.Rectangle(0, 0, 200, 20));

        var root = UiaTree.Window("App")
            .Build();
        root.AddChild(slider);

        var locator = LocatorTestBase.CreateLocator("controltype:Slider", root);

        await locator.SetValueAsync(99.0);
        var result = await locator.GetValueAsync();

        Assert.Equal(99.0, result);
    }
}
