using System.Diagnostics;
using Flawright;
using Flawright.CloseBehaviors;
using Flawright.InputModes;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// E2E tests for <see cref="Flawright.AttachAsync(AttachOptions, FlawrightOptions?, CancellationToken)"/>.
/// Each test launches the WPF test app via <see cref="Process.Start(string)"/>,
/// captures the PID, attaches Flawright to it, performs a UIA-only interaction,
/// and then tears down the externally-launched process explicitly.
/// </summary>
/// <remarks>
/// <para>
/// <b>Documented dispose behaviour for attached apps.</b>
/// Flawright does not currently distinguish between "launched" and "attached"
/// applications on dispose: <see cref="IFlawrightBrowser.CloseAsync"/> runs
/// the configured <see cref="ICloseBehavior"/> against the process, and the
/// fallback <c>DisposeAsync</c> path force-kills the process tree if it has not
/// yet exited. To "detach without killing" callers must therefore configure a
/// <see cref="ICloseBehavior"/> that returns <see langword="true"/> without
/// signalling the process — see <see cref="NoOpCloseBehavior"/> below — and
/// take care of process lifecycle themselves.
/// </para>
/// <para>
/// These tests use <see cref="VirtualInputMode"/> so they are safe to run on
/// headless / parallel CI runners.
/// </para>
/// </remarks>
public class TestAppAttachTests
{
    private static readonly string TestAppPath =
        Path.Combine(AppContext.BaseDirectory, "TestApp", "Flawright.E2ETests.TestApp.exe");

    /// <summary>
    /// Attaching by PID resolves a working <see cref="IFlawrightPage"/> bound
    /// to the externally-launched test app's main window, and a basic UIA
    /// interaction succeeds against the attached process.
    /// </summary>
    [Fact]
    public async Task AttachByPid_BindsToRunningProcess()
    {
        using var proc = StartTestApp();

        try
        {
            await using (var fw = await global::Flawright.Flawright.AttachAsync(
                new AttachOptions { ProcessId = proc.Id },
                new FlawrightOptions
                {
                    InputMode = new VirtualInputMode(),
                    DefaultTimeout = TimeSpan.FromSeconds(5),
                    // No-op close so we explicitly call CloseAsync() before dispose;
                    // that flips _closeAlreadyHandled so the dispose path does not
                    // kill the process — the test owns process lifecycle.
                    CloseBehavior = new NoOpCloseBehavior(),
                }))
            {
                var page = await fw.Browser.NewPageAsync();
                Assert.Equal("Flawright Test App", await page.TitleAsync());

                // Basic interaction proves the attached UIA tree is live.
                await page.Locator("#btnClick").ClickAsync();
                await page.Locator("#lblOutput").Expect().ToHaveTextAsync("Clicked");

                // Mark close as handled (no-op) so Dispose does not WM_CLOSE/kill.
                await fw.Browser.CloseAsync(TimeSpan.FromSeconds(1));
            }
        }
        finally
        {
            EnsureExited(proc);
        }
    }

    /// <summary>
    /// When <see cref="FlawrightOptions.CloseBehavior"/> is <see cref="NoOpCloseBehavior"/>,
    /// disposing the attached <see cref="IFlawright"/> instance must not
    /// terminate the externally-launched process — the caller retains
    /// ownership of process lifecycle.
    /// </summary>
    /// <remarks>
    /// This documents the supported "detach without killing" pattern: configure
    /// a no-op close behavior, dispose the Flawright wrapper, and verify the
    /// process is still alive. The default <see cref="WindowMessageCloseBehavior"/>
    /// will, by contrast, send WM_CLOSE on dispose — that scenario is exercised
    /// in <see cref="TestAppCloseBehaviorTests"/>.
    /// </remarks>
    [Fact]
    public async Task AttachByPid_NoOpCloseBehavior_DoesNotKillProcessOnDispose()
    {
        using var proc = StartTestApp();

        try
        {
            // Scope the await-using so dispose runs before we check the process.
            {
                await using var fw = await global::Flawright.Flawright.AttachAsync(
                    new AttachOptions { ProcessId = proc.Id },
                    new FlawrightOptions
                    {
                        InputMode = new VirtualInputMode(),
                        CloseBehavior = new NoOpCloseBehavior(),
                        DefaultTimeout = TimeSpan.FromSeconds(5),
                    });

                var page = await fw.Browser.NewPageAsync();
                Assert.Equal("Flawright Test App", await page.TitleAsync());

                // Explicit graceful close — must be a no-op against the process.
                await fw.Browser.CloseAsync(TimeSpan.FromSeconds(2));
            }

            // Give any stray dispose-side teardown a moment to do nothing.
            await Task.Delay(500);
            proc.Refresh();

            Assert.False(proc.HasExited,
                "Attached process must remain alive when CloseBehavior is a no-op " +
                "and Dispose runs the no-op path (CloseAsync was already called).");
        }
        finally
        {
            EnsureExited(proc);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Process StartTestApp()
    {
        var psi = new ProcessStartInfo
        {
            FileName = TestAppPath,
            UseShellExecute = false,
        };
        var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start test app.");

        // Wait briefly for the WPF main window to be ready so AttachAsync
        // can resolve it without racing the message pump.
        if (!proc.WaitForInputIdle(TimeSpan.FromSeconds(15)))
            throw new InvalidOperationException("Test app did not become input-idle.");

        return proc;
    }

    private static void EnsureExited(Process proc)
    {
        try
        {
            proc.Refresh();
            if (proc.HasExited)
                return;
            proc.Kill(entireProcessTree: true);
            proc.WaitForExit(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // best-effort cleanup — the process may already be gone
        }
    }

    /// <summary>
    /// A close behavior that does nothing and reports success. Used to
    /// suppress Flawright's default "send WM_CLOSE then force-kill" teardown
    /// for tests that own the process lifecycle externally (attach mode).
    /// </summary>
    private sealed class NoOpCloseBehavior : ICloseBehavior
    {
        public Task<bool> CloseAsync(ICloseContext context) => Task.FromResult(true);
    }
}
