using JerrettDavis.Flawright.CloseBehaviors;
using JerrettDavis.Flawright.UnitTests.Fakes;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.CloseBehaviors;

/// <summary>
/// Unit tests for <see cref="KillCloseBehavior"/>.
/// </summary>
public sealed class KillCloseBehaviorTests
{
    [Fact]
    public async Task CloseAsync_CallsKill()
    {
        var ctx = new FakeCloseContext();
        var behavior = new KillCloseBehavior();

        await behavior.CloseAsync(ctx);

        Assert.Equal(1, ctx.KillCount);
    }

    [Fact]
    public async Task CloseAsync_AlwaysReturnsTrue()
    {
        var ctx = new FakeCloseContext();
        var behavior = new KillCloseBehavior();

        var result = await behavior.CloseAsync(ctx);

        Assert.True(result);
    }

    [Fact]
    public async Task CloseAsync_DoesNotSendCloseSignal()
    {
        var ctx = new FakeCloseContext();
        var behavior = new KillCloseBehavior();

        await behavior.CloseAsync(ctx);

        Assert.Equal(0, ctx.SendCloseSignalCount);
    }
}
