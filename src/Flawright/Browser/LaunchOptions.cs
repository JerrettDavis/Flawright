using Flawright.AumidResolver;

namespace Flawright;

/// <summary>
/// Options for launching a new application via <see cref="Flawright.LaunchAsync(LaunchOptions, FlawrightOptions?, CancellationToken)"/>.
/// Exactly one of <see cref="ApplicationPath"/> or <see cref="Aumid"/> must be set.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="ApplicationPath"/> for traditional Win32 / desktop-bridge executables.
/// Use <see cref="Aumid"/> for packaged (UWP / WinUI 3 / store-published) apps.
/// Both cannot be set simultaneously; if neither is set an
/// <see cref="ArgumentException"/> is thrown when the browser initialises.
/// </para>
/// <para>
/// <see cref="WorkingDirectory"/> is silently ignored when launching via AUMID —
/// setting it alongside <see cref="Aumid"/> is a programming error and throws
/// <see cref="ArgumentException"/>.
/// </para>
/// </remarks>
/// <example>
/// Win32 launch:
/// <code>
/// var opts = new LaunchOptions
/// {
///     ApplicationPath  = @"C:\Windows\System32\notepad.exe",
///     Arguments        = ["myfile.txt"],
///     WorkingDirectory = @"C:\Documents"
/// };
/// </code>
/// Store-app launch via AUMID:
/// <code>
/// var opts = new LaunchOptions
/// {
///     Aumid = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App"
/// };
/// </code>
/// </example>
public sealed record LaunchOptions
{
    /// <summary>
    /// Full path or filename (resolved via PATH) of the executable to launch.
    /// Mutually exclusive with <see cref="Aumid"/> — exactly one must be set.
    /// </summary>
    public string? ApplicationPath { get; init; }

    /// <summary>
    /// Application User Model ID of the packaged (store / UWP) app to launch,
    /// e.g. <c>"Microsoft.WindowsCalculator_8wekyb3d8bbwe!App"</c>.
    /// Mutually exclusive with <see cref="ApplicationPath"/> — exactly one must be set.
    /// </summary>
    public string? Aumid { get; init; }

    /// <summary>
    /// Optional command-line arguments to pass to the process.
    /// When launching via <see cref="Aumid"/>, multiple arguments are joined with
    /// a single space and passed as a single string.
    /// </summary>
    public string[]? Arguments { get; init; }

    /// <summary>
    /// Working directory for the launched process.
    /// <see langword="null"/> inherits the current process's working directory.
    /// <b>Not supported for AUMID launches</b> — setting this alongside
    /// <see cref="Aumid"/> throws <see cref="ArgumentException"/>.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Maximum time to wait for the application's main window handle to appear
    /// after launch.  <see langword="null"/> uses the default of 30 seconds.
    /// </summary>
    public TimeSpan? StartupTimeout { get; init; }

    /// <summary>
    /// Maximum time to wait for the launched process to finish loading its DLL
    /// modules before handing control to FlaUI.  Defaults to 10 seconds when
    /// <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After <c>Process.Start</c> / <c>Application.AttachOrLaunch</c> returns,
    /// the Win32 loader maps DLL modules asynchronously.  On a loaded runner,
    /// FlaUI's internal <c>EnumProcessModules</c> calls (used during
    /// <c>WaitWhileMainHandleIsMissing</c> and window discovery) can race against
    /// the loader, producing a <see cref="System.ComponentModel.Win32Exception"/>
    /// with the message "Only part of a ReadProcessMemory or WriteProcessMemory
    /// request was completed" (Win32 error 299).
    /// </para>
    /// <para>
    /// Flawright gates the FlaUI call behind
    /// <see cref="Internals.ProcessReadyGuard.WaitForProcessReady"/> which first
    /// tries <c>Process.WaitForInputIdle</c> (the standard Win32 readiness signal
    /// for GUI apps) and falls back to polling <c>Process.Modules</c> until the
    /// read succeeds without a partial-read error.  Set this to a shorter value
    /// in integration tests where you want to fail fast, or to a longer value on
    /// very slow machines.
    /// </para>
    /// </remarks>
    public TimeSpan? LaunchReadyTimeout { get; init; }

    /// <summary>
    /// Custom AUMID resolver used to detect whether
    /// <see cref="ApplicationPath"/> refers to an AppExecutionAlias stub or
    /// shell-launcher shim and map it to a packaged-app AUMID.
    /// <see langword="null"/> uses <see cref="WindowsAumidResolver"/> (the
    /// recommended default).
    /// </summary>
    /// <remarks>
    /// Override this in unit tests to inject a <c>FakeAumidResolver</c> that
    /// returns predetermined <see cref="LaunchTarget"/> values without touching
    /// the file system or registry.
    /// </remarks>
    public IAumidResolver? AumidResolver { get; init; }
}
