namespace Flawright;

#pragma warning disable CA1711 // EventArgs suffix is the standard naming convention for event argument classes

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
public sealed class ApplicationClosingEventArgs : EventArgs
{
    /// <summary>The name of the close behavior type being used.</summary>
    public string CloseBehaviorName { get; }

    /// <summary>The timeout duration the close behavior will be allowed to run.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>The OS process ID being closed.</summary>
    public int ProcessId { get; }

    /// <summary>Initializes a new instance of ApplicationClosingEventArgs.</summary>
    public ApplicationClosingEventArgs(
        string closeBehaviorName,
        TimeSpan timeout,
        int processId)
    {
        CloseBehaviorName = closeBehaviorName;
        Timeout = timeout;
        ProcessId = processId;
    }
}

#pragma warning restore CA1711
