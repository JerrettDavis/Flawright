using Flawright.Backends;
using Flawright.Page;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Page;

/// <summary>
/// Unit tests for <see cref="FlawrightMouse"/>.
/// </summary>
public sealed class FlawrightMouseTests
{
    private static (FlawrightMouse Mouse, FakeInputBackend Input) Make()
    {
        var input = new FakeInputBackend();
        return (new FlawrightMouse(input), input);
    }

    // ── ClickAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ClickAsync_MovesToCoordinates()
    {
        var (mouse, input) = Make();
        await mouse.ClickAsync(100, 200);
        Assert.Contains(input.MouseMoves, m => m.X == 100 && m.Y == 200);
    }

    [Fact]
    public async Task ClickAsync_ClicksAtCoordinates()
    {
        var (mouse, input) = Make();
        await mouse.ClickAsync(100, 200);
        Assert.Contains(input.MouseClicks, c => c.X == 100 && c.Y == 200 && c.Button == MouseButton.Left);
    }

    [Fact]
    public async Task ClickAsync_DefaultsToLeftButton()
    {
        var (mouse, input) = Make();
        await mouse.ClickAsync(10, 20);
        Assert.All(input.MouseClicks, c => Assert.Equal(MouseButton.Left, c.Button));
    }

    [Fact]
    public async Task ClickAsync_UsesRightButton_WhenSpecified()
    {
        var (mouse, input) = Make();
        await mouse.ClickAsync(10, 20, new MouseClickOptions { Button = MouseButton.Right });
        Assert.Contains(input.MouseClicks, c => c.Button == MouseButton.Right);
    }

    [Fact]
    public async Task ClickAsync_RespectsClickCount()
    {
        var (mouse, input) = Make();
        await mouse.ClickAsync(10, 20, new MouseClickOptions { ClickCount = 3 });
        Assert.Contains(input.MouseClicks, c => c.ClickCount == 3);
    }

    [Fact]
    public async Task ClickAsync_RespectsCancellation()
    {
        var (mouse, _) = Make();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mouse.ClickAsync(10, 20, ct: cts.Token));
    }

    // ── DoubleClickAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DoubleClickAsync_MovesToCoordinates()
    {
        var (mouse, input) = Make();
        await mouse.DoubleClickAsync(50, 75);
        Assert.Contains(input.MouseMoves, m => m.X == 50 && m.Y == 75);
    }

    [Fact]
    public async Task DoubleClickAsync_ClicksWithClickCount2()
    {
        var (mouse, input) = Make();
        await mouse.DoubleClickAsync(50, 75);
        Assert.Contains(input.MouseClicks, c => c.X == 50 && c.Y == 75 && c.ClickCount == 2);
    }

    [Fact]
    public async Task DoubleClickAsync_DefaultsToLeftButton()
    {
        var (mouse, input) = Make();
        await mouse.DoubleClickAsync(50, 75);
        Assert.All(input.MouseClicks, c => Assert.Equal(MouseButton.Left, c.Button));
    }

    [Fact]
    public async Task DoubleClickAsync_UsesSpecifiedButton()
    {
        var (mouse, input) = Make();
        await mouse.DoubleClickAsync(50, 75, new MouseDoubleClickOptions { Button = MouseButton.Middle });
        Assert.Contains(input.MouseClicks, c => c.Button == MouseButton.Middle);
    }

    [Fact]
    public async Task DoubleClickAsync_RespectsCancellation()
    {
        var (mouse, _) = Make();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mouse.DoubleClickAsync(10, 10, ct: cts.Token));
    }

    // ── DownAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DownAsync_PressesLeftButtonByDefault()
    {
        var (mouse, input) = Make();
        await mouse.DownAsync();
        Assert.Contains(MouseButton.Left, input.MouseDowns);
    }

    [Fact]
    public async Task DownAsync_PressesSpecifiedButton()
    {
        var (mouse, input) = Make();
        await mouse.DownAsync(new MouseDownOptions { Button = MouseButton.Right });
        Assert.Contains(MouseButton.Right, input.MouseDowns);
    }

    [Fact]
    public async Task DownAsync_RespectsCancellation()
    {
        var (mouse, _) = Make();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mouse.DownAsync(ct: cts.Token));
    }

    // ── UpAsync ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpAsync_ReleasesLeftButtonByDefault()
    {
        var (mouse, input) = Make();
        await mouse.UpAsync();
        Assert.Contains(MouseButton.Left, input.MouseUps);
    }

    [Fact]
    public async Task UpAsync_ReleasesSpecifiedButton()
    {
        var (mouse, input) = Make();
        await mouse.UpAsync(new MouseUpOptions { Button = MouseButton.Middle });
        Assert.Contains(MouseButton.Middle, input.MouseUps);
    }

    [Fact]
    public async Task UpAsync_RespectsCancellation()
    {
        var (mouse, _) = Make();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mouse.UpAsync(ct: cts.Token));
    }

    // ── MoveAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task MoveAsync_MovesToCoordinates()
    {
        var (mouse, input) = Make();
        await mouse.MoveAsync(300, 400);
        Assert.Contains(input.MouseMoves, m => m.X == 300 && m.Y == 400);
    }

    [Fact]
    public async Task MoveAsync_DefaultStepsIsOne()
    {
        var (mouse, input) = Make();
        await mouse.MoveAsync(10, 20);
        Assert.Contains(input.MouseMoves, m => m.Steps == 1);
    }

    [Fact]
    public async Task MoveAsync_RespectsCustomSteps()
    {
        var (mouse, input) = Make();
        await mouse.MoveAsync(10, 20, new MouseMoveOptions { Steps = 5 });
        Assert.Contains(input.MouseMoves, m => m.Steps == 5);
    }

    [Fact]
    public async Task MoveAsync_RespectsCancellation()
    {
        var (mouse, _) = Make();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mouse.MoveAsync(10, 10, ct: cts.Token));
    }

    // ── WheelAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task WheelAsync_DispatchesWheel()
    {
        var (mouse, input) = Make();
        await mouse.WheelAsync(0, 120);
        Assert.Contains((0, 120), input.MouseWheels);
    }

    [Fact]
    public async Task WheelAsync_DispatchesHorizontalWheel()
    {
        var (mouse, input) = Make();
        await mouse.WheelAsync(-120, 0);
        Assert.Contains((-120, 0), input.MouseWheels);
    }

    [Fact]
    public async Task WheelAsync_RespectsCancellation()
    {
        var (mouse, _) = Make();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mouse.WheelAsync(0, 100, cts.Token));
    }

    // ── Coordinate truncation ─────────────────────────────────────────────────

    [Fact]
    public async Task ClickAsync_TruncatesDoubleToInt()
    {
        var (mouse, input) = Make();
        await mouse.ClickAsync(100.9, 200.1);
        // Should record 100, 200 (truncated via (int) cast)
        Assert.Contains(input.MouseClicks, c => c.X == 100 && c.Y == 200);
    }
}
