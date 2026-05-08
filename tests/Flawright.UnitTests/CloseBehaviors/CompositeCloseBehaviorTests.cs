using Flawright.CloseBehaviors;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.CloseBehaviors;

/// <summary>
/// Unit tests for <see cref="CompositeCloseBehavior"/>.
/// </summary>
public sealed class CompositeCloseBehaviorTests
{
    // ── Short-circuit on first true ───────────────────────────────────────────

    [Fact]
    public async Task CloseAsync_StopsAtFirstTrueBehavior()
    {
        var counter = new CallCountBehavior(returnsTrue: true);
        var shouldNotRun = new CallCountBehavior(returnsTrue: true);

        var composite = new CompositeCloseBehavior(counter, shouldNotRun);
        var ctx = new FakeCloseContext();

        var result = await composite.CloseAsync(ctx);

        Assert.True(result);
        Assert.Equal(1, counter.CallCount);
        Assert.Equal(0, shouldNotRun.CallCount); // short-circuited
    }

    [Fact]
    public async Task CloseAsync_RunsAllBehaviorsWhenAllReturnFalse()
    {
        var first = new CallCountBehavior(returnsTrue: false);
        var second = new CallCountBehavior(returnsTrue: false);

        var composite = new CompositeCloseBehavior(first, second);
        var ctx = new FakeCloseContext();

        var result = await composite.CloseAsync(ctx);

        Assert.False(result);
        Assert.Equal(1, first.CallCount);
        Assert.Equal(1, second.CallCount);
    }

    [Fact]
    public async Task CloseAsync_ReturnsFalseWhenAllBehaviorsReturnFalse()
    {
        var composite = new CompositeCloseBehavior(
            new CallCountBehavior(returnsTrue: false),
            new CallCountBehavior(returnsTrue: false));
        var ctx = new FakeCloseContext();

        var result = await composite.CloseAsync(ctx);

        Assert.False(result);
    }

    [Fact]
    public async Task CloseAsync_ReturnsFalseWithNoBehaviors()
    {
        var composite = new CompositeCloseBehavior();
        var ctx = new FakeCloseContext();

        var result = await composite.CloseAsync(ctx);

        Assert.False(result);
    }

    // ── Order ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CloseAsync_RunsBehaviorsInOrder()
    {
        var order = new List<int>();
        var first = new OrderTrackingBehavior(1, order, returnsTrue: false);
        var second = new OrderTrackingBehavior(2, order, returnsTrue: true);
        var third = new OrderTrackingBehavior(3, order, returnsTrue: true); // should not run

        var composite = new CompositeCloseBehavior(first, second, third);
        await composite.CloseAsync(new FakeCloseContext());

        Assert.Collection(order,
            n => Assert.Equal(1, n),
            n => Assert.Equal(2, n));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class CallCountBehavior(bool returnsTrue) : ICloseBehavior
    {
        public int CallCount { get; private set; }

        public Task<bool> CloseAsync(ICloseContext context)
        {
            CallCount++;
            return Task.FromResult(returnsTrue);
        }
    }

    private sealed class OrderTrackingBehavior(int id, List<int> order, bool returnsTrue) : ICloseBehavior
    {
        public Task<bool> CloseAsync(ICloseContext context)
        {
            order.Add(id);
            return Task.FromResult(returnsTrue);
        }
    }
}
