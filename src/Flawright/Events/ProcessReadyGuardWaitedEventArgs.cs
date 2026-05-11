namespace Flawright;

#pragma warning disable CA1711 // EventArgs suffix is the standard naming convention for event argument classes

/// <summary>
/// Raised when <see cref="Internals.ProcessReadyGuard.WaitForProcessReady"/>
/// was invoked and encountered non-trivial retries.
/// </summary>
/// <remarks>
/// <para>
/// The guard waits for a freshly-launched process to finish loading its DLL
/// modules before FlaUI attempts EnumProcessModules calls.  When the process
/// loads its modules immediately, the guard exits without retries and this
/// event is <em>not</em> raised.  The event is only raised when the guard
/// had to wait and poll the process's module list one or more times.
/// </para>
/// <para>
/// <b>Thread safety:</b> Handlers are invoked synchronously on the thread
/// that triggered the process launch.  Misbehaving handlers must not block or
/// raise unhandled exceptions, as handler exceptions are swallowed and not
/// propagated to the caller.
/// </para>
/// </remarks>
public sealed class ProcessReadyGuardWaitedEventArgs : EventArgs
{
    /// <summary>The OS process ID of the launched application.</summary>
    public int ProcessId { get; }

    /// <summary>Milliseconds elapsed during the guard wait.</summary>
    public long ElapsedMs { get; }

    /// <summary>Number of times the modules-ready probe had to be retried.</summary>
    public int ModulesProbeRetries { get; }

    /// <summary>Number of times the main-module-ready probe had to be retried.</summary>
    public int MainModuleProbeRetries { get; }

    /// <summary>Initializes a new instance of ProcessReadyGuardWaitedEventArgs.</summary>
    public ProcessReadyGuardWaitedEventArgs(
        int processId,
        long elapsedMs,
        int modulesProbeRetries,
        int mainModuleProbeRetries)
    {
        ProcessId = processId;
        ElapsedMs = elapsedMs;
        ModulesProbeRetries = modulesProbeRetries;
        MainModuleProbeRetries = mainModuleProbeRetries;
    }
}

#pragma warning restore CA1711
