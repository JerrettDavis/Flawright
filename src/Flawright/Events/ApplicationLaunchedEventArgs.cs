namespace Flawright;

#pragma warning disable CA1711 // EventArgs suffix is the standard naming convention for event argument classes

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
public sealed class ApplicationLaunchedEventArgs : EventArgs
{
    /// <summary>The OS process ID of the launched/attached application.</summary>
    public int ProcessId { get; }

    /// <summary>The path to the executable that was launched, or null.</summary>
    public string? ExecutablePath { get; }

    /// <summary>The Application User Model ID if launched as a store app, or null.</summary>
    public string? Aumid { get; }

    /// <summary>True if the application was attached to; false if it was launched.</summary>
    public bool WasAttached { get; }

    /// <summary>True if the application is a packaged (UWP/store) app.</summary>
    public bool IsPackagedApp { get; }

    /// <summary>The UTC timestamp when the event was raised.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Initializes a new instance of ApplicationLaunchedEventArgs.</summary>
    public ApplicationLaunchedEventArgs(
        int processId,
        string? executablePath,
        string? aumid,
        bool wasAttached,
        bool isPackagedApp,
        DateTimeOffset timestamp)
    {
        ProcessId = processId;
        ExecutablePath = executablePath;
        Aumid = aumid;
        WasAttached = wasAttached;
        IsPackagedApp = isPackagedApp;
        Timestamp = timestamp;
    }
}

#pragma warning restore CA1711
