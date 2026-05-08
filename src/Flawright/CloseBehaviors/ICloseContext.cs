namespace Flawright.CloseBehaviors;

/// <summary>
/// Provides the resources a close behavior needs to perform a graceful shutdown.
/// </summary>
public interface ICloseContext
{
    /// <summary>The browser whose application is being closed.</summary>
    IFlawrightBrowser Browser { get; }

    /// <summary>How long the close action is allowed to take.</summary>
    TimeSpan Timeout { get; }

    /// <summary>Cancellation token observed during the close.</summary>
    CancellationToken CancellationToken { get; }

    /// <summary><see langword="true"/> if the underlying process has exited.</summary>
    bool HasExited { get; }

    /// <summary>
    /// Sends WM_CLOSE to the application's main window. Does not wait for exit.
    /// </summary>
    void SendCloseSignal();

    /// <summary>
    /// Polls until <see cref="HasExited"/> returns true or the elapsed time
    /// exceeds <paramref name="timeout"/>. Returns true if the app exited.
    /// </summary>
    Task<bool> WaitForExitAsync(TimeSpan timeout);

    /// <summary>
    /// Searches the application's open windows for a button with the given Name
    /// (case-sensitive UIA Name match). Returns the first match, or
    /// <see langword="null"/> if none was found.
    /// </summary>
    Task<IFlawrightElement?> FindButtonAsync(string buttonName);

    /// <summary>
    /// Force-kills the application's process tree.
    /// </summary>
    void Kill();
}
