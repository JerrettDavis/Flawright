using System.Drawing;
using JerrettDavis.Flawright.Backends;
using JerrettDavis.Flawright.InputModes;
using JerrettDavis.Flawright.UnitTests.Fakes;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.InputModes;

/// <summary>
/// Unit tests for <see cref="RealInputMode"/>.
///
/// Verifies that each method delegates to the element backend or input backend
/// correctly, with no filtering or modification.
/// </summary>
public sealed class RealInputModeTests
{
    private static FakeElementBackend MakeElement(Rectangle? rect = null)
        => new(
            name: "TestElement",
            controlTypeName: "Button",
            boundingRectangle: rect ?? new Rectangle(10, 20, 100, 50));

    // ── Click ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Click_CallsElementClick()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Click(element, input);

        Assert.Equal(1, element.ClickCount);
    }

    [Fact]
    public void Click_DoesNotTouchInputBackend()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Click(element, input);

        Assert.Empty(input.MouseClicks);
        Assert.Empty(input.MouseMoves);
    }

    // ── DoubleClick ───────────────────────────────────────────────────────────

    [Fact]
    public void DoubleClick_CallsElementDoubleClick()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.DoubleClick(element, input);

        Assert.Equal(1, element.DoubleClickCount);
    }

    // ── Hover ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Hover_MovesMouseToCentroid()
    {
        // Bounding rectangle: X=10, Y=20, Width=100, Height=50
        // Expected centroid: (10 + 100/2, 20 + 50/2) = (60, 45)
        var element = MakeElement(new Rectangle(10, 20, 100, 50));
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Hover(element, input);

        Assert.Single(input.MouseMoves);
        var move = input.MouseMoves[0];
        Assert.Equal(60, move.X);
        Assert.Equal(45, move.Y);
        Assert.Equal(0, move.Steps);
    }

    // ── DragTo ────────────────────────────────────────────────────────────────

    [Fact]
    public void DragTo_PerformsMouseDownMoveUp_BetweenCentroids()
    {
        // Source: X=0, Y=0, W=10, H=10 → centroid (5, 5)
        // Target: X=100, Y=200, W=20, H=20 → centroid (110, 210)
        var source = MakeElement(new Rectangle(0, 0, 10, 10));
        var target = MakeElement(new Rectangle(100, 200, 20, 20));
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.DragTo(source, target, input);

        // Should: move to source, press, move to target, release
        Assert.Equal(2, input.MouseMoves.Count);
        Assert.Single(input.MouseDowns);
        Assert.Single(input.MouseUps);

        Assert.Equal(5, input.MouseMoves[0].X);
        Assert.Equal(5, input.MouseMoves[0].Y);
        Assert.Equal(110, input.MouseMoves[1].X);
        Assert.Equal(210, input.MouseMoves[1].Y);

        Assert.Equal(MouseButton.Left, input.MouseDowns[0]);
        Assert.Equal(MouseButton.Left, input.MouseUps[0]);
    }

    // ── Type ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Type_FocusesElement_ThenTypesText()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Type(element, "hello", input);

        Assert.Equal(1, element.FocusCount);
        Assert.Contains("hello", input.TypedTexts);
    }

    [Fact]
    public void Type_ForwardsTextExactly()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Type(element, "Hello World!", input);

        Assert.Equal("Hello World!", input.TypedTexts[0]);
    }

    // ── Press ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Press_FocusesElement_ThenTapsKey()
    {
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Press(element, "Enter", input);

        Assert.Equal(1, element.FocusCount);
        Assert.NotEmpty(input.KeyTaps);
    }

    [Fact]
    public void Press_ChordSuffix_TapsMainKey()
    {
        // "Ctrl+S" should tap S (the part after the last '+')
        var element = MakeElement();
        var input = new FakeInputBackend();
        var mode = new RealInputMode();

        mode.Press(element, "Ctrl+S", input);

        Assert.Equal(1, element.FocusCount);
        Assert.NotEmpty(input.KeyTaps);
    }
}
