using JerrettDavis.Flawright.Backends;

namespace JerrettDavis.Flawright.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="IApplicationHandle"/> for unit tests.
///
/// All properties are mutable so test setup can configure expected values.
/// </summary>
internal sealed class FakeApplicationHandle : IApplicationHandle
{
    private bool _disposed;

    /// <summary>Initialises a fake handle with sensible defaults.</summary>
    /// <param name="processId">Simulated process ID. Default 1.</param>
    /// <param name="hasExited">Initial exit state. Default <see langword="false"/>.</param>
    /// <param name="isStoreApp">Whether this simulates a store app. Default <see langword="false"/>.</param>
    /// <param name="waitResult">
    /// Value returned by <see cref="WaitWhileMainHandleIsMissing"/>.
    /// Default <see langword="true"/> (handle appears promptly).
    /// </param>
    /// <param name="mainWindow">Optional fake element to return from <see cref="GetMainWindow"/>.</param>
    public FakeApplicationHandle(
        int processId = 1,
        bool hasExited = false,
        bool isStoreApp = false,
        bool waitResult = true,
        FakeElementBackend? mainWindow = null)
    {
        ProcessId = processId;
        HasExited = hasExited;
        IsStoreApp = isStoreApp;
        WaitResult = waitResult;
        _mainWindow = mainWindow ?? new FakeElementBackend(name: "FakeWindow", controlTypeName: "Window");
    }

    // ── Configurable state ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public int ProcessId { get; set; }

    /// <inheritdoc/>
    public bool HasExited { get; set; }

    /// <inheritdoc/>
    public bool IsStoreApp { get; set; }

    /// <summary>
    /// Value returned by <see cref="WaitWhileMainHandleIsMissing"/>.
    /// Set to <see langword="false"/> to simulate a startup timeout.
    /// </summary>
    public bool WaitResult { get; set; }

    private FakeElementBackend _mainWindow;

    /// <summary>Sets the main window element returned by <see cref="GetMainWindow"/>.</summary>
    public void SetMainWindow(FakeElementBackend window) => _mainWindow = window;

    // ── Interaction recording ─────────────────────────────────────────────────

    /// <summary>How many times <see cref="Close"/> was called.</summary>
    public int CloseCount { get; private set; }

    /// <summary>How many times <see cref="KillProcessTree"/> was called.</summary>
    public int KillCount { get; private set; }

    /// <summary>Whether <see cref="Dispose"/> was called.</summary>
    public bool IsDisposed => _disposed;

    // ── IApplicationHandle ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool WaitWhileMainHandleIsMissing(TimeSpan timeout) => WaitResult;

    /// <inheritdoc/>
    public void Close() => CloseCount++;

    /// <inheritdoc/>
    public void KillProcessTree() => KillCount++;

    /// <inheritdoc/>
    public IElementBackend GetMainWindow() => _mainWindow;

    /// <inheritdoc/>
    public IReadOnlyList<IElementBackend> GetAllTopLevelWindows()
        => (IReadOnlyList<IElementBackend>)(new List<IElementBackend> { _mainWindow }.AsReadOnly());

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposed = true;
    }
}
