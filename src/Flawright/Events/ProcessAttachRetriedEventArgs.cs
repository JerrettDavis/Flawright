namespace Flawright;

#pragma warning disable CA1711 // EventArgs suffix is the standard naming convention for event argument classes

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
public sealed class ProcessAttachRetriedEventArgs : EventArgs
{
    /// <summary>The zero-based attempt number (1 for first retry, 2 for second, etc.).</summary>
    public int AttemptNumber { get; }

    /// <summary>The milliseconds waited before this retry attempt.</summary>
    public int DelayMs { get; }

    /// <summary>The Win32 error code that triggered the retry (typically 299).</summary>
    public int Win32ErrorCode { get; }

    /// <summary>Initializes a new instance of ProcessAttachRetriedEventArgs.</summary>
    public ProcessAttachRetriedEventArgs(
        int attemptNumber,
        int delayMs,
        int win32ErrorCode)
    {
        AttemptNumber = attemptNumber;
        DelayMs = delayMs;
        Win32ErrorCode = win32ErrorCode;
    }
}

#pragma warning restore CA1711
