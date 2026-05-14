using System.Drawing;
using Flawright.Backends.Uia;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.InputModes;

/// <summary>
/// Unit tests for the quadratic Bézier mouse-motion implementation in
/// <see cref="FlaUiInputBackend"/>.
///
/// All tests use <see cref="FakeCursorController"/> so no real OS cursor is
/// moved.  The <see cref="FlaUiInputBackend"/> is constructed with the
/// internal overload that accepts an <c>ICursorController</c>.
/// </summary>
public sealed class BezierMouseMoveTests
{
    // ── Helper ────────────────────────────────────────────────────────────────

    private static (FlaUiInputBackend backend, FakeCursorController cursor)
        Build(int startX = 0, int startY = 0)
    {
        var cursor = new FakeCursorController(startX, startY);
        var backend = new FlaUiInputBackend(cursor);
        return (backend, cursor);
    }

    // ── No-op when start == end ───────────────────────────────────────────────

    [Fact]
    public void MouseMove_SameStartAndEnd_IsNoOp()
    {
        var (backend, cursor) = Build(100, 200);

        backend.MouseMove(100, 200, steps: 0);

        Assert.Empty(cursor.Positions);
    }

    // ── Step count ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]   // auto-compute
    [InlineData(20)]  // explicit hint → clamped to [12, 80]
    [InlineData(1)]   // below minimum → clamped to 12
    public void MouseMove_ProducesEffectivePlusOneCalls(int stepsHint)
    {
        // Distance = 400 px → auto steps = clamp(400/4, 12, 80) = 80
        // The loop emits effectiveSteps intermediate points + 1 final snap.
        var (backend, cursor) = Build(0, 0);

        backend.MouseMove(400, 0, steps: stepsHint);

        // At minimum 13 calls (12 steps + 1 final), at most 81 (80 steps + 1 final).
        var count = cursor.Positions.Count;
        Assert.InRange(count, 13, 81);
    }

    // ── Final position is exactly the target ─────────────────────────────────

    [Fact]
    public void MouseMove_FinalPosition_IsExactTarget()
    {
        const int TargetX = 357;
        const int TargetY = 812;
        var (backend, cursor) = Build(50, 50);

        backend.MouseMove(TargetX, TargetY, steps: 0);

        var last = cursor.Positions[^1];
        Assert.Equal(TargetX, last.X);
        Assert.Equal(TargetY, last.Y);
    }

    // ── Path is curved (not a straight line) ─────────────────────────────────

    [Fact]
    public void MouseMove_IntermediatePoints_AreNotAllOnStraightLine()
    {
        // Start (0,0) → end (400,0): a straight-line interpolation would
        // keep Y = 0 for every point.  With a Bézier control point offset
        // perpendicularly and random jitter, at least some Y values differ.
        var (backend, cursor) = Build(0, 0);

        backend.MouseMove(400, 0, steps: 0);

        // Exclude the final snap (last element) which is exactly on-target.
        var intermediate = cursor.Positions.Take(cursor.Positions.Count - 1).ToList();

        // At least one intermediate point must have Y ≠ 0 (curve or jitter).
        Assert.Contains(intermediate, p => p.Y != 0);
    }

    // ── First and last intermediate are not equal ────────────────────────────

    [Fact]
    public void MouseMove_FirstAndLastIntermediatePositions_Differ()
    {
        var (backend, cursor) = Build(0, 0);

        backend.MouseMove(300, 300, steps: 0);

        Assert.True(cursor.Positions.Count >= 2,
            "Expected at least two SetPosition calls.");

        var first = cursor.Positions[0];
        var last = cursor.Positions[^1];

        // The final snap is at the target; the first intermediate is near the start.
        Assert.False(first.X == last.X && first.Y == last.Y,
            "First and last SetPosition calls should not be at the same point.");
    }

    // ── Path starts near source, ends exactly at target ──────────────────────

    [Fact]
    public void MouseMove_PathStartsNearSource_EndsAtTarget()
    {
        const int StartX = 50;
        const int StartY = 50;
        const int TargetX = 250;
        const int TargetY = 350;
        var (backend, cursor) = Build(StartX, StartY);

        backend.MouseMove(TargetX, TargetY, steps: 0);

        var positions = cursor.Positions;
        Assert.NotEmpty(positions);

        // First intermediate should be closer to start than to target.
        var first = positions[0];
        var distToStart = Math.Sqrt(Math.Pow(first.X - StartX, 2) + Math.Pow(first.Y - StartY, 2));
        var distToTarget = Math.Sqrt(Math.Pow(first.X - TargetX, 2) + Math.Pow(first.Y - TargetY, 2));
        Assert.True(distToStart < distToTarget,
            $"First intermediate ({first.X},{first.Y}) should be closer to start than target.");

        // Last position is exactly the target.
        var last = positions[^1];
        Assert.Equal(TargetX, last.X);
        Assert.Equal(TargetY, last.Y);
    }

    // ── Minimum step clamp ───────────────────────────────────────────────────

    [Fact]
    public void MouseMove_VeryShortDistance_UsesMinimumSteps()
    {
        // Distance ~14 px → distance/4 = 3.5, clamped to 12 → 13 total calls.
        var (backend, cursor) = Build(0, 0);

        backend.MouseMove(10, 10, steps: 0);

        // Should have at least 13 calls (12 steps + final snap).
        Assert.True(cursor.Positions.Count >= 13,
            $"Expected ≥ 13 SetPosition calls; got {cursor.Positions.Count}.");
    }

    // ── Maximum step clamp ───────────────────────────────────────────────────

    [Fact]
    public void MouseMove_VeryLongDistance_CapsAtMaximumSteps()
    {
        // Distance ~7071 px (5000,5000) → distance/4 = 1767, clamped to 80.
        // Total calls = 80 + 1 (final snap) = 81.
        var (backend, cursor) = Build(0, 0);

        backend.MouseMove(5000, 5000, steps: 0);

        Assert.Equal(81, cursor.Positions.Count);
    }

    // ── Explicit steps hint is respected (within clamp) ──────────────────────

    [Fact]
    public void MouseMove_ExplicitStepsHint_IsRespected()
    {
        // Hint 20, within [12,80], distance large enough that auto would be 80.
        var (backend, cursor) = Build(0, 0);

        backend.MouseMove(1000, 0, steps: 20);

        // 20 intermediate steps + 1 final snap = 21 calls.
        Assert.Equal(21, cursor.Positions.Count);
    }
}
