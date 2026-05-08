namespace JerrettDavis.Flawright.Backends;

/// <summary>
/// Abstracts a launched or attached desktop application handle.
/// The sole production implementation is <c>FlaUiApplicationHandle</c>.
/// Tests use <c>FakeApplicationHandle</c>.
/// </summary>
internal interface IApplicationHandle : IDisposable
{
    /// <summary>Gets the OS process ID of the application.</summary>
    int ProcessId { get; }

    /// <summary>Gets whether the application process has exited.</summary>
    bool HasExited { get; }

    /// <summary>
    /// Gets whether the application was launched as a store (UWP/packaged) app.
    /// When <see langword="true"/>, <c>DisposeAsync</c> must not call
    /// <c>KillProcessTree</c>.
    /// </summary>
    bool IsStoreApp { get; }

    /// <summary>
    /// Blocks until the main window handle appears or the timeout elapses.
    /// </summary>
    /// <param name="timeout">Maximum duration to wait.</param>
    /// <returns><see langword="true"/> if the handle appeared; <see langword="false"/> on timeout.</returns>
    bool WaitWhileMainHandleIsMissing(TimeSpan timeout);

    /// <summary>Sends a close signal to the application (graceful).</summary>
    void Close();

    /// <summary>Kills the entire process tree (forceful, non-graceful).</summary>
    void KillProcessTree();

    /// <summary>Returns an element backend for the application's main window.</summary>
    IElementBackend GetMainWindow();

    /// <summary>Returns element backends for all current top-level windows.</summary>
    IReadOnlyList<IElementBackend> GetAllTopLevelWindows();
}
