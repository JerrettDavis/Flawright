namespace Flawright;

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
/// <param name="WindowHandle">
/// The native window handle (HWND) of the top-level window,
/// as an <see cref="nint"/> pointer.
/// </param>
/// <param name="Title">
/// The window title string, or <see langword="null"/> if the window has no title.
/// </param>
/// <param name="ProcessId">The OS process ID that owns the window.</param>
public sealed record WindowDetectedEventArgs(
    nint WindowHandle,
    string? Title,
    int ProcessId);
