namespace Flawright;

#pragma warning disable CA1711 // EventArgs suffix is the standard naming convention for event argument classes

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
public sealed class ApplicationClosedEventArgs : EventArgs
{
    /// <summary>The name of the close behavior type that was used.</summary>
    public string CloseBehaviorName { get; }

    /// <summary>True if the close was graceful; false if a force-kill was required.</summary>
    public bool Graceful { get; }

    /// <summary>The timeout duration that was applied.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>The OS process ID that was closed.</summary>
    public int ProcessId { get; }

    /// <summary>True if the process has exited (even if a force-kill was required).</summary>
    public bool ExitedCleanly { get; }

    /// <summary>Initializes a new instance of ApplicationClosedEventArgs.</summary>
    public ApplicationClosedEventArgs(
        string closeBehaviorName,
        bool graceful,
        TimeSpan timeout,
        int processId,
        bool exitedCleanly)
    {
        CloseBehaviorName = closeBehaviorName;
        Graceful = graceful;
        Timeout = timeout;
        ProcessId = processId;
        ExitedCleanly = exitedCleanly;
    }
}

#pragma warning restore CA1711
