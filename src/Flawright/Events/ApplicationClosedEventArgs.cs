namespace Flawright;

/// <summary>
/// Raised at the end of <see cref="IFlawrightBrowser.CloseAsync"/>,
/// after the close behavior has run and the process has exited (or been killed).
/// </summary>
/// <remarks>
/// <para>
/// This event provides visibility into the outcome of the close operation,
/// including whether the close was graceful (the behavior succeeded) or
/// required a force-kill fallback.
/// </para>
/// <para>
/// <b>Thread safety:</b> Handlers are invoked synchronously on the thread
/// that called <c>CloseAsync</c>.  Misbehaving handlers must not block or
/// raise unhandled exceptions, as handler exceptions are swallowed and not
/// propagated to the caller.
/// </para>
/// </remarks>
/// <param name="CloseBehaviorName">
/// The name of the close behavior type that was used (e.g. "WindowMessageCloseBehavior",
/// "DismissDialogCloseBehavior", "KillCloseBehavior").
/// </param>
/// <param name="Graceful">
/// <see langword="true"/> if the close behavior returned <see langword="true"/>
/// (graceful close); <see langword="false"/> if a force-kill was required.
/// </param>
/// <param name="Timeout">
/// The timeout duration that was applied.
/// </param>
/// <param name="ProcessId">The OS process ID that was closed.</param>
/// <param name="ExitedCleanly">
/// <see langword="true"/> if the process has exited (even if a force-kill
/// was required); <see langword="false"/> if the process is still running
/// (rare edge case when attached to external process).
/// </param>
public sealed record ApplicationClosedEventArgs(
    string CloseBehaviorName,
    bool Graceful,
    TimeSpan Timeout,
    int ProcessId,
    bool ExitedCleanly);
