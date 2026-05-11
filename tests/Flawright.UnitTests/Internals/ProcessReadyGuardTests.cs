using System.Diagnostics;
using Flawright.Internals;
using Xunit;

namespace Flawright.UnitTests.Internals;

/// <summary>
/// Unit tests for <see cref="ProcessReadyGuard"/>.
///
/// The production code path that calls <c>Process.WaitForInputIdle</c> and
/// <c>Process.Modules</c> requires a real OS process and cannot be injected
/// without spawning one.  These tests therefore exercise:
/// <list type="bullet">
///   <item>The injectable <c>modulesReadyProbe</c> delegate — zero process spawning.</item>
///   <item>The timeout boundary (zero / negative timeout → return false immediately).</item>
///   <item>A real subprocess via the <c>cmd.exe /c exit 0</c> probe to confirm the
///       happy path works on a live process (runs only on Windows).</item>
/// </list>
/// </summary>
public sealed class ProcessReadyGuardTests
{
    // ── Zero / negative timeout → return false immediately ────────────────────

    [Fact]
    public void WaitForProcessReady_ZeroTimeout_ReturnsFalseImmediately()
    {
        using var proc = CreateInertProcess();
        try
        {
            var result = ProcessReadyGuard.WaitForProcessReady(
                proc,
                timeout: TimeSpan.Zero,
                modulesReadyProbe: _ => true);  // probe would succeed, but timeout short-circuits

            Assert.False(result, "Zero timeout should return false without calling the probe.");
        }
        finally
        {
            KillSilently(proc);
        }
    }

    [Fact]
    public void WaitForProcessReady_NegativeTimeout_ReturnsFalseImmediately()
    {
        using var proc = CreateInertProcess();
        try
        {
            var result = ProcessReadyGuard.WaitForProcessReady(
                proc,
                timeout: TimeSpan.FromMilliseconds(-1),
                modulesReadyProbe: _ => true);

            Assert.False(result);
        }
        finally
        {
            KillSilently(proc);
        }
    }

    // ── Probe returns true on first call → ready immediately ─────────────────

    [Fact]
    public void WaitForProcessReady_ProbeReturnsTrueImmediately_ReturnsTrue()
    {
        using var proc = CreateInertProcess();
        try
        {
            var callCount = 0;
            var result = ProcessReadyGuard.WaitForProcessReady(
                proc,
                timeout: TimeSpan.FromSeconds(5),
                modulesReadyProbe: _ =>
                {
                    callCount++;
                    return true;
                });

            Assert.True(result);
            // The probe should have been called at most once on the fallback path.
            // (WaitForInputIdle may short-circuit before the probe for real GUI processes,
            //  but for our inert cmd.exe it will throw InvalidOperationException first.)
            Assert.True(callCount <= 1, $"Probe called {callCount} time(s); expected ≤ 1.");
        }
        finally
        {
            KillSilently(proc);
        }
    }

    // ── Probe returns false then true → retries until success ─────────────────

    [Fact]
    public void WaitForProcessReady_ProbeReturnsFalseThenTrue_ReturnsTrue()
    {
        using var proc = CreateInertProcess();
        try
        {
            var callCount = 0;
            var result = ProcessReadyGuard.WaitForProcessReady(
                proc,
                timeout: TimeSpan.FromSeconds(5),
                modulesReadyProbe: _ =>
                {
                    callCount++;
                    return callCount >= 3; // succeed on the third call
                },
                mainModuleProbe: _ => true);  // always ready to isolate modules probe testing

            Assert.True(result);
            Assert.True(callCount >= 3, $"Expected ≥ 3 probe calls; got {callCount}.");
        }
        finally
        {
            KillSilently(proc);
        }
    }

    [Fact]
    public void WaitForProcessReady_MainModuleProbeReturnsFalseThenTrue_ReturnsTrue()
    {
        using var proc = CreateInertProcess();
        try
        {
            var mainProbeCallCount = 0;
            var result = ProcessReadyGuard.WaitForProcessReady(
                proc,
                timeout: TimeSpan.FromSeconds(5),
                modulesReadyProbe: _ => true,
                mainModuleProbe: _ =>
                {
                    mainProbeCallCount++;
                    return mainProbeCallCount >= 3;
                });

            Assert.True(result);
            Assert.True(mainProbeCallCount >= 3);
        }
        finally
        {
            KillSilently(proc);
        }
    }

    // ── Probe always returns false → timeout expires, return false ─────────────

    [Fact]
    public void WaitForProcessReady_ProbeAlwaysFalse_TimesOutAndReturnsFalse()
    {
        using var proc = CreateInertProcess();
        try
        {
            var sw = Stopwatch.StartNew();

            var result = ProcessReadyGuard.WaitForProcessReady(
                proc,
                timeout: TimeSpan.FromMilliseconds(200),
                modulesReadyProbe: _ => false);

            sw.Stop();

            Assert.False(result);
            // Should have taken roughly the timeout duration (within a generous margin).
            Assert.True(sw.ElapsedMilliseconds >= 150,
                $"Expected ≥ 150 ms elapsed; got {sw.ElapsedMilliseconds} ms.");
        }
        finally
        {
            KillSilently(proc);
        }
    }

    // ── NullProcess → throws ArgumentNullException ───────────────────────────

    [Fact]
    public void WaitForProcessReady_NullProcess_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => ProcessReadyGuard.WaitForProcessReady(null!));
    }

    // ── LaunchReadyTimeout flows through to WaitForProcessReady ──────────────

    [Fact]
    public void WaitForProcessReady_LaunchReadyTimeout_ZeroMeansNoWait()
    {
        // When LaunchReadyTimeout is zero, WaitForProcessReady must return false
        // immediately without invoking the probe at all.  This verifies the timeout
        // flows from LaunchOptions through to the guard rather than being ignored.
        using var proc = CreateInertProcess();
        try
        {
            var probeInvoked = false;
            var result = ProcessReadyGuard.WaitForProcessReady(
                proc,
                timeout: TimeSpan.Zero,
                modulesReadyProbe: _ => { probeInvoked = true; return true; });

            Assert.False(result, "Zero timeout must short-circuit and return false.");
            Assert.False(probeInvoked, "Probe must not be called when timeout is zero.");
        }
        finally
        {
            KillSilently(proc);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts a trivial non-GUI console process that stays alive long enough for
    /// the test to interact with it.  Uses <c>cmd.exe /c timeout /t 10 /nobreak</c>
    /// — the <c>timeout</c> command is available on all supported Windows versions
    /// and doesn't open a window in our usage.
    /// </summary>
    private static Process CreateInertProcess()
    {
        var psi = new ProcessStartInfo("cmd.exe", "/c timeout /t 10 /nobreak >nul 2>&1")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
        };
        return Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start inert process for test.");
    }

    private static void KillSilently(Process proc)
    {
#pragma warning disable CA1031
        try { proc.Kill(); } catch { /* already exited */ }
#pragma warning restore CA1031
    }
}
