using System.Drawing;
using FlaUI.Core.WindowsAPI;
using Flawright.Backends;
using Flawright.InputModes;
using Flawright.Locator;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.InputModes;

/// <summary>
/// Tests for <see cref="RealInputMode"/> covering keyboard modifier key
/// press/release paths and the delay-based click path.
/// </summary>
public sealed class RealInputModeModifierTests
{
    private static FakeElementBackend MakeElement(Rectangle? rect = null)
        => new(
            name: "TestElement",
            controlTypeName: "Button",
            boundingRectangle: rect ?? new Rectangle(10, 20, 100, 50));

    // ── Click with delay ──────────────────────────────────────────────────────

    [Fact]
    public void Click_WithDelay_CallsMouseDownAndMouseUp()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Click(element, input, delay: TimeSpan.FromMilliseconds(1));

        Assert.Single(input.MouseDowns);
        Assert.Single(input.MouseUps);
        Assert.Empty(input.MouseClicks); // delay path uses down/up instead of click
    }

    // ── DoubleClick with delay ────────────────────────────────────────────────

    [Fact]
    public void DoubleClick_WithDelay_StillCallsMouseClickWith2()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.DoubleClick(element, input, delay: TimeSpan.FromMilliseconds(1));

        // DoubleClick always calls MouseClick(x, y, button, 2) regardless of delay
        Assert.Single(input.MouseClicks);
        Assert.Equal(2, input.MouseClicks[0].ClickCount);
    }

    // ── Click with Control modifier ───────────────────────────────────────────

    [Fact]
    public void Click_WithControlModifier_PressesAndReleasesControl()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Click(element, input, modifiers: KeyModifiers.Control);

        Assert.Contains(VirtualKeyShort.CONTROL, input.KeyPresses);
        Assert.Contains(VirtualKeyShort.CONTROL, input.KeyReleases);
    }

    [Fact]
    public void Click_WithShiftModifier_PressesAndReleasesShift()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Click(element, input, modifiers: KeyModifiers.Shift);

        Assert.Contains(VirtualKeyShort.SHIFT, input.KeyPresses);
        Assert.Contains(VirtualKeyShort.SHIFT, input.KeyReleases);
    }

    [Fact]
    public void Click_WithAltModifier_PressesAndReleasesAlt()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Click(element, input, modifiers: KeyModifiers.Alt);

        Assert.Contains(VirtualKeyShort.ALT, input.KeyPresses);
        Assert.Contains(VirtualKeyShort.ALT, input.KeyReleases);
    }

    [Fact]
    public void Click_WithMetaModifier_PressesAndReleasesLWin()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Click(element, input, modifiers: KeyModifiers.Meta);

        Assert.Contains(VirtualKeyShort.LWIN, input.KeyPresses);
        Assert.Contains(VirtualKeyShort.LWIN, input.KeyReleases);
    }

    [Fact]
    public void Click_WithAllModifiers_PressesAndReleasesAll()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        var all = KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Meta;
        mode.Click(element, input, modifiers: all);

        Assert.Contains(VirtualKeyShort.CONTROL, input.KeyPresses);
        Assert.Contains(VirtualKeyShort.SHIFT, input.KeyPresses);
        Assert.Contains(VirtualKeyShort.ALT, input.KeyPresses);
        Assert.Contains(VirtualKeyShort.LWIN, input.KeyPresses);

        Assert.Contains(VirtualKeyShort.CONTROL, input.KeyReleases);
        Assert.Contains(VirtualKeyShort.SHIFT, input.KeyReleases);
        Assert.Contains(VirtualKeyShort.ALT, input.KeyReleases);
        Assert.Contains(VirtualKeyShort.LWIN, input.KeyReleases);
    }

    // ── Click with RightButton ────────────────────────────────────────────────

    [Fact]
    public void Click_WithRightButton_PassesRightButtonToInputBackend()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Click(element, input, button: MouseButton.Right);

        Assert.Single(input.MouseClicks);
        Assert.Equal(MouseButton.Right, input.MouseClicks[0].Button);
    }

    // ── Click with position offset ────────────────────────────────────────────

    [Fact]
    public void Click_WithPositionOverride_ClicksAtOffsetFromOrigin()
    {
        // Rect: X=0, Y=0, W=100, H=100
        // Position: X=10, Y=20 → expected click at (0+10, 0+20) = (10, 20)
        var element = MakeElement(new Rectangle(0, 0, 100, 100));
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Click(element, input, position: new BoundingBox(10, 20, 100, 100));

        Assert.Single(input.MouseClicks);
        Assert.Equal(10, input.MouseClicks[0].X);
        Assert.Equal(20, input.MouseClicks[0].Y);
    }

    // ── Click with clickCount ─────────────────────────────────────────────────

    [Fact]
    public void Click_WithClickCountTwo_PassesTwoToInputBackend()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Click(element, input, clickCount: 2);

        Assert.Single(input.MouseClicks);
        Assert.Equal(2, input.MouseClicks[0].ClickCount);
    }
}
