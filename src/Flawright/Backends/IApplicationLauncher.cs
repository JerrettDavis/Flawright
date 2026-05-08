namespace Flawright.Backends;

/// <summary>
/// Abstracts the application launch and attach operations.
/// The sole production implementation is <c>FlaUiApplicationLauncher</c>.
/// Tests use <c>FakeApplicationLauncher</c>.
/// </summary>
internal interface IApplicationLauncher
{
    /// <summary>
    /// Launches an application using the supplied options.
    /// </summary>
    /// <param name="opts">Launch options (path, arguments, working directory, startup timeout).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A handle to the launched application.</returns>
    Task<IApplicationHandle> Launch(LaunchOptions opts, CancellationToken ct = default);

    /// <summary>
    /// Launches a packaged (store/UWP) application via its Application User Model ID.
    /// </summary>
    /// <param name="aumid">The AUMID, e.g. <c>Microsoft.WindowsCalculator_8wekyb3d8bbwe!App</c>.</param>
    /// <param name="args">Optional command-line arguments (may be empty string).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A handle to the launched store application.</returns>
    Task<IApplicationHandle> LaunchStoreApp(string aumid, string args, CancellationToken ct = default);

    /// <summary>
    /// Attaches to an already-running application by its OS process ID.
    /// </summary>
    /// <param name="pid">The target process ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A handle to the attached application.</returns>
    Task<IApplicationHandle> Attach(int pid, CancellationToken ct = default);

    /// <summary>
    /// Attaches to an already-running application by process name (without extension),
    /// selecting the instance at <paramref name="index"/> when multiple processes match.
    /// </summary>
    /// <param name="exeBaseName">Process name without <c>.exe</c> extension, e.g. <c>"notepad"</c>.</param>
    /// <param name="index">Zero-based index among matching processes (default 0).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A handle to the attached application.</returns>
    Task<IApplicationHandle> AttachByName(string exeBaseName, int index, CancellationToken ct = default);
}
