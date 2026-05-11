namespace Flawright;

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
/// <param name="ProcessId">The OS process ID of the launched application.</param>
/// <param name="ElapsedMs">
/// Milliseconds elapsed during the guard wait (from start of
/// <c>WaitForProcessReady</c> to readiness confirmation or timeout).
/// </param>
/// <param name="ModulesProbeRetries">
/// Number of times the modules-ready probe had to be retried
/// (retries are due to Win32 error 299 — partial read during loader activity).
/// </param>
/// <param name="MainModuleProbeRetries">
/// Number of times the main-module-ready probe had to be retried
/// (retries are due to Win32 error 299 — partial read during loader activity).
/// </param>
public sealed record ProcessReadyGuardWaitedEventArgs(
    int ProcessId,
    long ElapsedMs,
    int ModulesProbeRetries,
    int MainModuleProbeRetries);
