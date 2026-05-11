using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Browser;

/// <summary>
/// Tests for edge-case paths in <see cref="FlawrightBrowser"/>:
/// - CloseAsync when not yet initialized (_app == null)
/// - CloseAsync called twice (idempotent)
/// - RaiseEvent with null handler (no subscribers)
/// - DisposeAsync for attached (wasAttached) processes
/// </summary>
public sealed class FlawrightBrowserEdgeCaseTests
{
    private static readonly FlawrightOptions FastOpts = new()
    {
        DefaultTimeout = TimeSpan.FromMilliseconds(200),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(10),
    };

    private static (FlawrightBrowser Browser, FakeApplicationHandle Handle)
        MakeLaunchBrowser(string path = "notepad.exe")
    {
        var h = new FakeApplicationHandle(waitResult: true);
        var launcher = new FakeApplicationLauncher { Handle = h };
        var browser = new FlawrightBrowser(
            launcher,
            new FakeInputBackend(),
            new FakeConditionTranslator(),
            new LaunchOptions { ApplicationPath = path },
            FastOpts);
        return (browser, h);
    }

    // ── CloseAsync before EnsureInitialized (_app == null) ───────────────────

    [Fact]
    public async Task CloseAsync_BeforeInitialization_ReturnsTrue()
    {
        // Never called EnsureInitializedAsync — _app is null
        var (browser, _) = MakeLaunchBrowser();

        var result = await browser.CloseAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task CloseAsync_BeforeInitialization_DoesNotFireApplicationClosingEvent()
    {
        var (browser, _) = MakeLaunchBrowser();

        var firedClosing = false;
        browser.ApplicationClosing += (_, _) => { firedClosing = true; };

        await browser.CloseAsync();

        Assert.False(firedClosing);
    }

    // ── CloseAsync called twice (idempotent) ──────────────────────────────────

    [Fact]
    public async Task CloseAsync_CalledTwice_SecondCallReturnsTrueImmediately()
    {
        // First call may return false (process hasn't exited); second must return true (idempotent).
        var (browser, handle) = MakeLaunchBrowser();
        await browser.EnsureInitializedAsync();

        // Mark as exited so first close succeeds cleanly
        handle.HasExited = true;
        var result1 = await browser.CloseAsync();
        var result2 = await browser.CloseAsync();

        Assert.True(result1);  // graceful because HasExited = true
        Assert.True(result2);  // idempotent
    }

    [Fact]
    public async Task CloseAsync_CalledTwice_FiresApplicationClosingOnlyOnce()
    {
        var (browser, handle) = MakeLaunchBrowser();
        await browser.EnsureInitializedAsync();

        var closingCount = 0;
        browser.ApplicationClosing += (_, _) => { closingCount++; };

        handle.HasExited = true;
        await browser.CloseAsync();
        await browser.CloseAsync();

        Assert.Equal(1, closingCount);
    }

    // ── RaiseEvent with no subscribers (null handler) ─────────────────────────

    [Fact]
    public async Task ApplicationLaunched_WithNoSubscribers_DoesNotThrow()
    {
        // No handlers attached — RaiseEvent should handle null handler gracefully
        var (browser, _) = MakeLaunchBrowser();

        // Should not throw even though no ApplicationLaunched handler is subscribed
        await browser.EnsureInitializedAsync();
    }

    [Fact]
    public async Task ApplicationClosing_WithNoSubscribers_DoesNotThrow()
    {
        var (browser, _) = MakeLaunchBrowser();
        await browser.EnsureInitializedAsync();

        // No handlers attached — should not throw
        await browser.CloseAsync();
    }

    // ── DisposeAsync for attached (wasAttached) process ───────────────────────

    [Fact]
    public async Task DisposeAsync_WhenAttached_DoesNotCallKillProcessTree()
    {
        var h = new FakeApplicationHandle(waitResult: true);
        var launcher = new FakeApplicationLauncher { Handle = h };
        var browser = new FlawrightBrowser(
            launcher,
            new FakeInputBackend(),
            new FakeConditionTranslator(),
            new AttachOptions { ProcessId = 1234 },
            FastOpts);

        await browser.EnsureInitializedAsync();

        // Dispose should NOT kill the process for attached handles
        await browser.DisposeAsync();

        Assert.Equal(0, h.KillCount);
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_IsIdempotent()
    {
        var (browser, _) = MakeLaunchBrowser();
        await browser.EnsureInitializedAsync();

        // Should not throw on second dispose
        await browser.DisposeAsync();
        await browser.DisposeAsync();
    }
}
