namespace Flawright;

/// <summary>
/// Raised at the start of <see cref="IFlawrightBrowser.CloseAsync"/>,
/// before the configured close behavior is invoked.
/// </summary>
/// <remarks>
/// <para>
/// This event provides visibility into the application close operation,
/// including the name of the close behavior being used, the timeout duration,
/// and the process being closed.
/// </para>
/// <para>
/// <b>Thread safety:</b> Handlers are invoked synchronously on the thread
/// that called <c>CloseAsync</c>.  Misbehaving handlers must not block or
/// raise unhandled exceptions, as handler exceptions are swallowed and not
/// propagated to the caller.
/// </para>
/// </remarks>
/// <param name="CloseBehaviorName">
/// The name of the close behavior type (e.g. "WindowMessageCloseBehavior",
/// "DismissDialogCloseBehavior", "KillCloseBehavior").
/// </param>
/// <param name="Timeout">
/// The timeout duration the close behavior will be allowed to run before
/// falling back to a process kill.
/// </param>
/// <param name="ProcessId">The OS process ID being closed.</param>
public sealed record ApplicationClosingEventArgs(
    string CloseBehaviorName,
    TimeSpan Timeout,
    int ProcessId);
