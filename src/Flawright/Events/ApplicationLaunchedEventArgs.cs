namespace Flawright;

/// <summary>
/// Raised after an application has been launched or attached to and its
/// process handle is available for inspection.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised during the first initialization of the browser
/// (<see cref="FlawrightBrowser.EnsureInitializedAsync"/>), after the
/// application process has been created and before the main window
/// appearance timeout begins.
/// </para>
/// <para>
/// <b>Thread safety:</b> Handlers are invoked synchronously on the thread
/// that triggered initialization.  Misbehaving handlers must not block or
/// raise unhandled exceptions, as handler exceptions are swallowed and not
/// propagated to the caller.
/// </para>
/// </remarks>
/// <param name="ProcessId">The OS process ID of the launched/attached application.</param>
/// <param name="ExecutablePath">
/// The path to the executable that was launched, or <see langword="null"/>
/// if the application was launched via AUMID or attached to an existing process.
/// </param>
/// <param name="Aumid">
/// The Application User Model ID if the app was launched as a store app,
/// or <see langword="null"/> for traditional Win32 launches.
/// </param>
/// <param name="WasAttached">
/// <see langword="true"/> if the application was attached to
/// (via <see cref="Flawright.AttachAsync"/>);
/// <see langword="false"/> if it was launched (via <see cref="Flawright.LaunchAsync"/>).
/// </param>
/// <param name="IsPackagedApp">
/// <see langword="true"/> if the application is a packaged (UWP/store) app;
/// <see langword="false"/> for traditional Win32 applications.
/// </param>
/// <param name="Timestamp">
/// The UTC timestamp when the event was raised (typically immediately
/// after <c>Application.AttachOrLaunch</c> returns).
/// </param>
public sealed record ApplicationLaunchedEventArgs(
    int ProcessId,
    string? ExecutablePath,
    string? Aumid,
    bool WasAttached,
    bool IsPackagedApp,
    DateTimeOffset Timestamp);
