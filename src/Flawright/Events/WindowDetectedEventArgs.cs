namespace Flawright;

#pragma warning disable CA1711 // EventArgs suffix is the standard naming convention for event argument classes

/// <summary>
/// Raised when a top-level window is detected by the application.
/// </summary>
/// <remarks>
/// <para>
/// This event is only raised when <see cref="FlawrightOptions.EnableWindowEvents"/>
/// is <see langword="true"/>. By default it is <see langword="false"/>,
/// so window detection events are not fired (to avoid noise in production code).
/// </para>
/// <para>
/// Windows are detected during calls to <see cref="IFlawrightBrowser.GetAllPagesAsync"/>
/// and <see cref="IFlawrightBrowser.WaitForPageAsync"/>, allowing subscribers to
/// observe the application's window discovery.
/// </para>
/// <para>
/// <b>Thread safety:</b> Handlers are invoked synchronously on the thread
/// that triggered the window detection.  Misbehaving handlers must not block or
/// raise unhandled exceptions, as handler exceptions are swallowed and not
/// propagated to the caller.
/// </para>
/// </remarks>
public sealed class WindowDetectedEventArgs : EventArgs
{
    /// <summary>The native window handle (HWND) of the top-level window.</summary>
    public nint WindowHandle { get; }

    /// <summary>The window title string, or null if the window has no title.</summary>
    public string? Title { get; }

    /// <summary>The OS process ID that owns the window.</summary>
    public int ProcessId { get; }

    /// <summary>Initializes a new instance of WindowDetectedEventArgs.</summary>
    public WindowDetectedEventArgs(
        nint windowHandle,
        string? title,
        int processId)
    {
        WindowHandle = windowHandle;
        Title = title;
        ProcessId = processId;
    }
}

#pragma warning restore CA1711
