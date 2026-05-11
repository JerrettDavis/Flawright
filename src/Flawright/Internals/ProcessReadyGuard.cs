using System.ComponentModel;
using System.Diagnostics;

namespace Flawright.Internals;

/// <summary>
/// Waits for a freshly-launched process to finish loading its DLL modules before
/// FlaUI attempts <c>EnumProcessModules</c> calls that can fail with
/// <see cref="Win32Exception"/> (error 299 — only part of a
/// ReadProcessMemory/WriteProcessMemory request was completed) when the process
/// loader is still mapping DLLs into the address space.
/// </summary>
/// <remarks>
/// <para>
/// <b>Root cause:</b> <c>Process.Start</c> / <c>Application.AttachOrLaunch</c> returns
/// as soon as the OS creates the process record.  The Win32 loader then maps DLL modules
/// asynchronously.  FlaUI's <c>EnumProcessModules</c> calls (used inside
/// <c>WaitWhileMainHandleIsMissing</c> and similar paths) can race against the loader on
/// a busy CI runner, producing the partial-read Win32Exception.
/// </para>
/// <para>
/// <b>Fix strategy:</b>
/// <list type="number">
///   <item>
///     Call <see cref="Process.WaitForInputIdle(int)"/> (the standard Win32 readiness
///     signal).  For WPF / WinForms / Win32 apps that pump a message loop, this blocks
///     until the app is ready to process input — which implies the loader has finished.
///   </item>
///   <item>
///     If <c>WaitForInputIdle</c> is not available (non-UI / console processes, or the
///     call throws <see cref="InvalidOperationException"/>), fall back to polling
///     <see cref="Process.Modules"/> until one read completes without a partial-read
///     <see cref="Win32Exception"/>.  This directly exercises the same kernel path that
///     <c>EnumProcessModules</c> uses, so a successful read guarantees subsequent FlaUI
///     calls will not race.
///   </item>
/// </list>
/// Both strategies are bounded by the <c>timeout</c> parameter so the happy path (the
/// vast majority of launches) adds at most one successful kernel call and then proceeds
/// immediately.
/// </para>
/// </remarks>
internal static class ProcessReadyGuard
{
    /// <summary>
    /// The Win32 error code for "only part of a ReadProcessMemory or
    /// WriteProcessMemory request was completed" (ERROR_PARTIAL_COPY = 299).
    /// This is the native error surfaced as <see cref="Win32Exception"/> when
    /// <c>EnumProcessModules</c> is called while a process is still loading its
    /// DLL modules.
    /// </summary>
    private const int ErrorPartialCopy = 299;

    /// <summary>
    /// Poll interval used during the modules-enumeration fallback.
    /// Short enough to not add measurable latency; long enough to avoid
    /// busy-waiting on a loaded CI runner.
    /// </summary>
    private static readonly TimeSpan FallbackPollInterval = TimeSpan.FromMilliseconds(50);

    // ── Injectable collaborator for unit tests ────────────────────────────────

    /// <summary>
    /// Delegate type for the modules-ready check so unit tests can inject a
    /// fake without spawning real processes.
    /// </summary>
    /// <param name="p">The process to inspect.</param>
    /// <returns>
    /// <see langword="true"/> when the process modules are enumerable (no partial-read
    /// error); <see langword="false"/> when the loader is still in progress.
    /// </returns>
    internal delegate bool ModulesReadyProbe(Process p);

    /// <summary>
    /// Delegate type for the main-module-ready check so unit tests can inject a
    /// fake without spawning real processes.
    /// </summary>
    /// <param name="p">The process to inspect.</param>
    /// <returns>
    /// <see langword="true"/> when the process main module is readable (no partial-read
    /// error); <see langword="false"/> when the loader is still in progress.
    /// </returns>
    internal delegate bool MainModuleProbe(Process p);

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Waits until <paramref name="process"/> has finished loading its DLL modules,
    /// or until <paramref name="timeout"/> elapses — whichever comes first.
    /// </summary>
    /// <param name="process">
    /// The freshly-launched process.  Must not be <see langword="null"/>.
    /// </param>
    /// <param name="timeout">
    /// Maximum time to wait.  Defaults to 10 seconds when
    /// <see langword="null"/>.  A zero or negative value is treated as "do not
    /// wait" — useful in unit tests that want to exercise the timeout path.
    /// </param>
    /// <param name="modulesReadyProbe">
    /// Optional injectable probe used by unit tests.  Production code leaves
    /// this <see langword="null"/> to use the real <c>Process.Modules</c> poll.
    /// </param>
    /// <param name="mainModuleProbe">
    /// Optional injectable probe for the main module readiness check used by unit tests.
    /// Production code leaves this <see langword="null"/> to use the real
    /// <c>Process.MainModule.FileName</c> access poll.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the process is ready (modules enumerable);
    /// <see langword="false"/> when the timeout elapsed before readiness was
    /// confirmed (process still loading, or non-UI process where neither
    /// strategy could confirm).
    /// </returns>
    internal static bool WaitForProcessReady(
        Process process,
        TimeSpan? timeout = null,
        ModulesReadyProbe? modulesReadyProbe = null,
        MainModuleProbe? mainModuleProbe = null)
    {
        ArgumentNullException.ThrowIfNull(process);

        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(10);

        if (effectiveTimeout <= TimeSpan.Zero)
            return false;

        // ── Strategy 1: WaitForInputIdle ──────────────────────────────────────
        // For WPF / WinForms / Win32 GUI apps, this is the canonical way to
        // know the message loop (and thus the loader) is ready.  We treat any
        // return value from WaitForInputIdle as "ready" — it returns false only
        // when the timeout expires, and we check that via the return code.
        try
        {
            var ms = (int)Math.Min(effectiveTimeout.TotalMilliseconds, int.MaxValue);
            var idleResult = process.WaitForInputIdle(ms);
            if (idleResult)
                return true;

            // WaitForInputIdle returned false = timeout elapsed without becoming
            // idle.  Fall through to the modules-poll fallback which may still
            // succeed (e.g. console apps that don't have a message loop but whose
            // modules ARE enumerable).
        }
        catch (InvalidOperationException)
        {
            // Non-UI process (no message queue) — WaitForInputIdle is not
            // applicable.  Fall through to the modules-poll strategy.
        }
#pragma warning disable CA1031 // Unexpected WaitForInputIdle failure — fall through to fallback
        catch (Exception)
        {
            // Platform-specific or other transient error — proceed to fallback.
        }
#pragma warning restore CA1031

        // ── Strategy 2: Poll Process.Modules ─────────────────────────────────
        // Directly exercises EnumProcessModules.  A successful call confirms the
        // kernel will not return ERROR_PARTIAL_COPY to FlaUI's subsequent calls.
        var probe = modulesReadyProbe ?? DefaultModulesProbe;
        var mainProbe = mainModuleProbe ?? DefaultMainModuleProbe;
        var deadline = DateTime.UtcNow + effectiveTimeout;

        while (DateTime.UtcNow < deadline)
        {
            var modulesReady = probe(process);
            var mainModuleReady = mainProbe(process);

            if (modulesReady && mainModuleReady)
                return true;

            Thread.Sleep(FallbackPollInterval);
        }

        return false;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Production probe: attempts to access <see cref="Process.Modules"/>.
    /// Returns <see langword="true"/> when enumeration succeeds without a
    /// partial-read error; <see langword="false"/> when the loader is still in
    /// progress.
    /// </summary>
    private static bool DefaultModulesProbe(Process process)
    {
        try
        {
            // Accessing the Modules property calls EnumProcessModules internally.
            // A successful access (Count > 0) means the loader has finished.
            return process.Modules.Count > 0;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorPartialCopy)
        {
            // ERROR_PARTIAL_COPY (299): loader still in progress — retry.
            return false;
        }
        catch (InvalidOperationException)
        {
            // Process exited between our check and the Modules access.
            // Treat as "ready" so callers can proceed to the exit check.
            return true;
        }
        // All other exceptions (access denied, process not found, etc.) propagate so the
        // caller sees the real error rather than silently treating a broken process as ready.
    }

    /// <summary>
    /// Production probe: attempts to access <see cref="Process.MainModule"/>.
    /// Returns <see langword="true"/> when access succeeds without a
    /// partial-read error; <see langword="false"/> when the loader is still in
    /// progress. FlaUI's Application.Attach calls GetMainModuleFilepath internally
    /// for debug logging, so this probe mirrors that path to ensure the guard
    /// verifies the same code path before releasing.
    /// </summary>
    private static bool DefaultMainModuleProbe(Process process)
    {
        try
        {
            _ = process.MainModule?.FileName;
            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorPartialCopy)
        {
            // ERROR_PARTIAL_COPY (299): loader still in progress — retry.
            return false;
        }
        catch (InvalidOperationException)
        {
            // Process exited between our check and the MainModule access.
            // Treat as "ready" so callers can proceed to the exit check.
            return true;
        }
        // All other exceptions (access denied, process not found, etc.) propagate so the
        // caller sees the real error rather than silently treating a broken process as ready.
    }
}
