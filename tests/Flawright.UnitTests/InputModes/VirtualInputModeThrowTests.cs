using Flawright.Backends;
using Flawright.InputModes;
using Flawright.Locator;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.InputModes;

/// <summary>
/// Tests for <see cref="VirtualInputMode"/> covering the early-throw paths
/// when Button, Position, or Modifiers are specified (neither UIA Click nor
/// UIA DoubleClick supports those).
/// </summary>
public sealed class VirtualInputModeThrowTests
{
    private static FakeElementBackend MakeButton()
        => new(name: "OK", controlTypeName: "Button");

    // ── Click — throws when non-Left button ──────────────────────────────────

    [Fact]
    public void Click_WithRightButton_ThrowsNotSupportedException()
    {
        var mode = new VirtualInputMode();

        var ex = Assert.Throws<NotSupportedException>(
            () => mode.Click(MakeButton(), new FakeInputBackend(), button: MouseButton.Right));

        Assert.Contains("Button", ex.Message);
    }

    // ── Click — throws when position is specified ─────────────────────────────

    [Fact]
    public void Click_WithPosition_ThrowsNotSupportedException()
    {
        var mode = new VirtualInputMode();

        var ex = Assert.Throws<NotSupportedException>(
            () => mode.Click(MakeButton(), new FakeInputBackend(), position: new BoundingBox(5, 5, 10, 10)));

        Assert.Contains("Position", ex.Message);
    }

    // ── Click — throws when modifiers are specified ───────────────────────────

    [Fact]
    public void Click_WithModifiers_ThrowsNotSupportedException()
    {
        var mode = new VirtualInputMode();

        var ex = Assert.Throws<NotSupportedException>(
            () => mode.Click(MakeButton(), new FakeInputBackend(), modifiers: KeyModifiers.Control));

        Assert.Contains("Modifiers", ex.Message);
    }

    // ── DoubleClick — throws when non-Left button ─────────────────────────────

    [Fact]
    public void DoubleClick_WithRightButton_ThrowsNotSupportedException()
    {
        var mode = new VirtualInputMode();

        var ex = Assert.Throws<NotSupportedException>(
            () => mode.DoubleClick(MakeButton(), new FakeInputBackend(), button: MouseButton.Right));

        Assert.Contains("Button", ex.Message);
    }

    // ── DoubleClick — throws when position is specified ───────────────────────

    [Fact]
    public void DoubleClick_WithPosition_ThrowsNotSupportedException()
    {
        var mode = new VirtualInputMode();

        var ex = Assert.Throws<NotSupportedException>(
            () => mode.DoubleClick(MakeButton(), new FakeInputBackend(), position: new BoundingBox(5, 5, 10, 10)));

        Assert.Contains("Position", ex.Message);
    }

    // ── DoubleClick — throws when modifiers are specified ─────────────────────

    [Fact]
    public void DoubleClick_WithModifiers_ThrowsNotSupportedException()
    {
        var mode = new VirtualInputMode();

        var ex = Assert.Throws<NotSupportedException>(
            () => mode.DoubleClick(MakeButton(), new FakeInputBackend(), modifiers: KeyModifiers.Shift));

        Assert.Contains("Modifiers", ex.Message);
    }
}
