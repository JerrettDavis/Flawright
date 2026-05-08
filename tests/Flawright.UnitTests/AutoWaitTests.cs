using Flawright.Internals;
using Xunit;

namespace Flawright.UnitTests;

/// <summary>
/// Tests for the internal <see cref="AutoWait"/> polling loop.
///
/// Tests use a 1 ms retry interval so they complete nearly instantly.
/// </summary>
public class AutoWaitTests
{
    private static readonly TimeSpan FastInterval = TimeSpan.FromMilliseconds(1);
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(200);

    // ── UntilAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UntilAsync_PredicateTrueOnFirstTry_ReturnsImmediately()
    {
        var callCount = 0;

        var result = await AutoWait.UntilAsync(
            () =>
            {
                callCount++;
                return "found";
            },
            selector: "#foo",
            timeout: ShortTimeout,
            retryInterval: FastInterval,
            ct: CancellationToken.None);

        Assert.Equal("found", result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task UntilAsync_PredicateNullThenValue_PollsUntilSuccess()
    {
        // Returns null the first 3 times, then returns the value.
        var callCount = 0;

        var result = await AutoWait.UntilAsync(
            () =>
            {
                callCount++;
                return callCount >= 4 ? "value" : null;
            },
            selector: "#bar",
            timeout: ShortTimeout,
            retryInterval: FastInterval,
            ct: CancellationToken.None);

        Assert.Equal("value", result);
        Assert.True(callCount >= 4, $"Expected at least 4 calls but got {callCount}");
    }

    [Fact]
    public async Task UntilAsync_TimeoutExceeded_ThrowsFlawrightTimeoutException()
    {
        var ex = await Assert.ThrowsAsync<FlawrightTimeoutException>(async () =>
            await AutoWait.UntilAsync<string>(
                () => null,           // never returns a value
                selector: "#missing",
                timeout: ShortTimeout,
                retryInterval: FastInterval,
                ct: CancellationToken.None));

        Assert.Contains("#missing", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UntilAsync_TimeoutException_CarriesSelector()
    {
        var ex = await Assert.ThrowsAsync<FlawrightTimeoutException>(async () =>
            await AutoWait.UntilAsync<string>(
                () => null,
                selector: "name:DoesNotExist",
                timeout: ShortTimeout,
                retryInterval: FastInterval,
                ct: CancellationToken.None));

        Assert.Equal("name:DoesNotExist", ex.Selector);
        Assert.NotNull(ex.Timeout);
    }

    [Fact]
    public async Task UntilAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // already cancelled

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await AutoWait.UntilAsync<string>(
                () => null,
                selector: "#any",
                timeout: TimeSpan.FromSeconds(5),
                retryInterval: FastInterval,
                ct: cts.Token));
    }

    [Fact]
    public async Task UntilAsync_CancellationDuringPolling_StopsPromptly()
    {
        using var cts = new CancellationTokenSource();

        // Cancel after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            await cts.CancelAsync();
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await AutoWait.UntilAsync<string>(
                () => null,
                selector: "#loop",
                timeout: TimeSpan.FromSeconds(30),  // long timeout; cancellation fires first
                retryInterval: FastInterval,
                ct: cts.Token));
    }

    [Fact]
    public async Task UntilAsync_ConditionThrowsTransiently_RetriesAndSucceeds()
    {
        // First call throws; subsequent calls return a value.
        var callCount = 0;

        var result = await AutoWait.UntilAsync(
            () =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("transient");
                return "recovered";
            },
            selector: "#recover",
            timeout: ShortTimeout,
            retryInterval: FastInterval,
            ct: CancellationToken.None);

        Assert.Equal("recovered", result);
        Assert.True(callCount >= 2);
    }

    // ── UntilTrueAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task UntilTrueAsync_PredicateTrueOnFirstTry_ReturnsImmediately()
    {
        var callCount = 0;

        await AutoWait.UntilTrueAsync(
            () =>
            {
                callCount++;
                return true;
            },
            description: "should be true",
            timeout: ShortTimeout,
            retryInterval: FastInterval,
            ct: CancellationToken.None);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task UntilTrueAsync_PredicateFalseThenTrue_PollsUntilSuccess()
    {
        var callCount = 0;

        await AutoWait.UntilTrueAsync(
            () =>
            {
                callCount++;
                return callCount >= 3;
            },
            description: "wait for callCount >= 3",
            timeout: ShortTimeout,
            retryInterval: FastInterval,
            ct: CancellationToken.None);

        Assert.True(callCount >= 3);
    }

    [Fact]
    public async Task UntilTrueAsync_TimeoutExceeded_ThrowsFlawrightTimeoutException()
    {
        var ex = await Assert.ThrowsAsync<FlawrightTimeoutException>(async () =>
            await AutoWait.UntilTrueAsync(
                () => false,
                description: "Expected element to be visible",
                timeout: ShortTimeout,
                retryInterval: FastInterval,
                ct: CancellationToken.None));

        Assert.Contains("Expected element to be visible", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UntilTrueAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await AutoWait.UntilTrueAsync(
                () => false,
                description: "any",
                timeout: TimeSpan.FromSeconds(5),
                retryInterval: FastInterval,
                ct: cts.Token));
    }

    [Fact]
    public async Task UntilTrueAsync_ConditionThrowsTransiently_RetriesAndSucceeds()
    {
        var callCount = 0;

        await AutoWait.UntilTrueAsync(
            () =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("transient");
                return true;
            },
            description: "recover from transient",
            timeout: ShortTimeout,
            retryInterval: FastInterval,
            ct: CancellationToken.None);

        Assert.True(callCount >= 2);
    }

    [Fact]
    public async Task UntilTrueAsync_TimeoutException_IsSubclassOfTimeoutException()
    {
        var ex = await Assert.ThrowsAsync<FlawrightTimeoutException>(async () =>
            await AutoWait.UntilTrueAsync(
                () => false,
                description: "desc",
                timeout: ShortTimeout,
                retryInterval: FastInterval,
                ct: CancellationToken.None));

        Assert.IsAssignableFrom<TimeoutException>(ex);
    }
}
