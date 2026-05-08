using JerrettDavis.Flawright.CloseBehaviors;
using JerrettDavis.Flawright.UnitTests.Fakes;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.CloseBehaviors;

/// <summary>
/// Unit tests for <see cref="WindowMessageCloseBehavior"/>.
/// </summary>
public sealed class WindowMessageCloseBehaviorTests
{
    [Fact]
    public async Task CloseAsync_SendsCloseSignal()
    {
        var ctx = new FakeCloseContext(waitForExitResults: [true]);
        var behavior = new WindowMessageCloseBehavior();

        await behavior.CloseAsync(ctx);

        Assert.Equal(1, ctx.SendCloseSignalCount);
    }

    [Fact]
    public async Task CloseAsync_WaitsForExit()
    {
        var ctx = new FakeCloseContext(waitForExitResults: [true]);
        var behavior = new WindowMessageCloseBehavior();

        await behavior.CloseAsync(ctx);

        Assert.Equal(1, ctx.WaitForExitCallCount);
    }

    [Fact]
    public async Task CloseAsync_ReturnsTrue_WhenProcessExits()
    {
        var ctx = new FakeCloseContext(waitForExitResults: [true]);
        var behavior = new WindowMessageCloseBehavior();

        var result = await behavior.CloseAsync(ctx);

        Assert.True(result);
    }

    [Fact]
    public async Task CloseAsync_ReturnsFalse_WhenProcessDoesNotExit()
    {
        var ctx = new FakeCloseContext(waitForExitResults: [false]);
        var behavior = new WindowMessageCloseBehavior();

        var result = await behavior.CloseAsync(ctx);

        Assert.False(result);
    }

    [Fact]
    public async Task CloseAsync_PassesContextTimeout_ToWaitForExit()
    {
        // The behavior should pass ctx.Timeout to WaitForExitAsync — verify it
        // does not override with a hardcoded value.
        var customTimeout = TimeSpan.FromSeconds(42);
        var ctx = new FakeCloseContext(timeout: customTimeout, waitForExitResults: [true]);
        var behavior = new WindowMessageCloseBehavior();

        await behavior.CloseAsync(ctx);

        // If WaitForExitAsync was called exactly once, the timeout was forwarded.
        Assert.Equal(1, ctx.WaitForExitCallCount);
    }
}
