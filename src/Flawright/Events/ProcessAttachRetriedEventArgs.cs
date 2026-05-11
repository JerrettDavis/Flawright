namespace Flawright;

/// <summary>
/// Raised when <see cref="Internals.ProcessAttachRetry"/> retries a failed
/// attach operation due to a transient Win32 error.
/// </summary>
/// <remarks>
/// <para>
/// FlaUI's internal <c>GetMainModuleFilepath</c> diagnostic call (made
/// during attach to capture a debug-log filepath) can fail with Win32 error
/// 299 (ERROR_PARTIAL_COPY — "Only part of a ReadProcessMemory or
/// WriteProcessMemory request was completed") when the process is still
/// loading its DLL modules immediately after launch.  The retry logic uses
/// exponential backoff (10 ms, 20 ms, 40 ms, etc.) to wait for the loader
/// to finish before retrying the attach.
/// </para>
/// <para>
/// This event is raised for each retry attempt. If the attach eventually
/// succeeds after retries, the overall launch will still succeed and the
/// caller will see no error — this event documents the transient failures
/// that were hidden by the retry logic.
/// </para>
/// <para>
/// <b>Thread safety:</b> Handlers are invoked synchronously on the thread
/// that triggered the launch.  Misbehaving handlers must not block or
/// raise unhandled exceptions, as handler exceptions are swallowed and not
/// propagated to the caller.
/// </para>
/// </remarks>
/// <param name="AttemptNumber">
/// The zero-based attempt number (1 for the first retry, 2 for the second, etc.).
/// </param>
/// <param name="DelayMs">
/// The milliseconds waited before this retry attempt (e.g. 10 ms for the
/// first retry, 20 ms for the second).
/// </param>
/// <param name="Win32ErrorCode">
/// The Win32 error code that triggered the retry (typically 299 —
/// ERROR_PARTIAL_COPY).
/// </param>
public sealed record ProcessAttachRetriedEventArgs(
    int AttemptNumber,
    int DelayMs,
    int Win32ErrorCode);
