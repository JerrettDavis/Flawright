using JerrettDavis.Flawright.Internals;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Internals;

/// <summary>
/// Unit tests for <see cref="SafeProcessQueries"/>.
/// Documents the contract: a delegate that throws should return
/// <see langword="true"/> (the safe default for dispose paths).
/// </summary>
public sealed class SafeProcessQueriesTests
{
    [Fact]
    public void HasExitedSafe_DelegateReturnsTrue_ReturnsTrue()
    {
        var result = SafeProcessQueries.HasExitedSafe(() => true);
        Assert.True(result);
    }

    [Fact]
    public void HasExitedSafe_DelegateReturnsFalse_ReturnsFalse()
    {
        var result = SafeProcessQueries.HasExitedSafe(() => false);
        Assert.False(result);
    }

    [Fact]
    public void HasExitedSafe_DelegateThrowsInvalidOperationException_ReturnsTrue()
    {
        // Simulates the case where the underlying Process handle has been disposed
        // (e.g. an AppExecutionAlias stub that exited and whose handle was cleaned up).
        var result = SafeProcessQueries.HasExitedSafe(
            () => throw new InvalidOperationException("No process is associated with this object."));

        Assert.True(result, "A disposed/detached process handle should be treated as exited");
    }

    [Fact]
    public void HasExitedSafe_DelegateThrowsUnexpectedException_ReturnsTrue()
    {
        // Any other exception should also be swallowed on dispose paths.
        var result = SafeProcessQueries.HasExitedSafe(
            () => throw new InvalidCastException("unexpected platform exception"));

        Assert.True(result);
    }
}
