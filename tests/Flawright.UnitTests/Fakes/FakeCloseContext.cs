using Flawright.CloseBehaviors;

namespace Flawright.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="ICloseContext"/> for unit-testing <see cref="ICloseBehavior"/> implementations.
/// All behavior is configurable and all interactions are recorded.
/// </summary>
internal sealed class FakeCloseContext : ICloseContext
{
    private readonly Queue<bool> _waitForExitResults;

    /// <summary>
    /// Initialises a fake context.
    /// </summary>
    /// <param name="hasExited">Initial value for <see cref="HasExited"/>.</param>
    /// <param name="timeout">Value returned from <see cref="Timeout"/>.</param>
    /// <param name="waitForExitResults">
    /// Values returned by successive calls to <see cref="WaitForExitAsync"/>.
    /// If fewer results are provided than calls made, the last value is repeated.
    /// </param>
    /// <param name="findButtonResult">
    /// Element returned by <see cref="FindButtonAsync"/>. <see langword="null"/> means not found.
    /// </param>
    public FakeCloseContext(
        bool hasExited = false,
        TimeSpan? timeout = null,
        IEnumerable<bool>? waitForExitResults = null,
        IFlawrightElement? findButtonResult = null)
    {
        HasExited = hasExited;
        Timeout = timeout ?? TimeSpan.FromSeconds(5);
        _waitForExitResults = new Queue<bool>(waitForExitResults ?? [true]);
        FindButtonResult = findButtonResult;
    }

    // ── Configurable state ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool HasExited { get; set; }

    /// <inheritdoc/>
    public TimeSpan Timeout { get; }

    /// <inheritdoc/>
    public CancellationToken CancellationToken => CancellationToken.None;

    /// <inheritdoc/>
    public IFlawrightBrowser Browser => null!; // not needed for behavior unit tests

    /// <summary>The element to return from <see cref="FindButtonAsync"/> (or null for not-found).</summary>
    public IFlawrightElement? FindButtonResult { get; set; }

    // ── Interaction recording ─────────────────────────────────────────────────

    /// <summary>How many times <see cref="SendCloseSignal"/> was called.</summary>
    public int SendCloseSignalCount { get; private set; }

    /// <summary>How many times <see cref="Kill"/> was called.</summary>
    public int KillCount { get; private set; }

    /// <summary>How many times <see cref="WaitForExitAsync"/> was called.</summary>
    public int WaitForExitCallCount { get; private set; }

    /// <summary>Button names passed to <see cref="FindButtonAsync"/>, in order.</summary>
    public IReadOnlyList<string> FindButtonCalls => _findButtonCalls.AsReadOnly();
    private readonly List<string> _findButtonCalls = [];

    // ── ICloseContext ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void SendCloseSignal() => SendCloseSignalCount++;

    /// <inheritdoc/>
    public void Kill() => KillCount++;

    /// <inheritdoc/>
    public Task<bool> WaitForExitAsync(TimeSpan timeout)
    {
        WaitForExitCallCount++;
        bool result;
        if (_waitForExitResults.Count == 1)
            result = _waitForExitResults.Peek(); // keep last value for repeated calls
        else if (_waitForExitResults.Count > 1)
            result = _waitForExitResults.Dequeue();
        else
            result = true;

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<IFlawrightElement?> FindButtonAsync(string buttonName)
    {
        _findButtonCalls.Add(buttonName);
        return Task.FromResult(FindButtonResult);
    }
}
