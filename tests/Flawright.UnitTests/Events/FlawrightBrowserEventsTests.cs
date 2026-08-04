using Flawright.AumidResolver;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Events;

/// <summary>
/// Unit tests for <see cref="IFlawrightBrowserEvents"/> — covering event
/// firing for ApplicationLaunched, ApplicationClosing, ApplicationClosed, and
/// exception handling in event handlers.
/// </summary>
public sealed class FlawrightBrowserEventsTests
{
    private static readonly FlawrightOptions FastOpts = new()
    {
        DefaultTimeout = TimeSpan.FromMilliseconds(200),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(10),
    };

    private static readonly string[] ClosingThenClosedOrder = ["Closing", "Closed"];

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (FlawrightBrowser Browser, FakeApplicationHandle Handle)
        MakeBrowser(LaunchOptions? launchOpts = null)
    {
        var h = new FakeApplicationHandle(waitResult: true);
        var launcher = new FakeApplicationLauncher { Handle = h };
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var opts = launchOpts ?? new LaunchOptions { ApplicationPath = "notepad.exe" };
        var browser = new FlawrightBrowser(launcher, input, translator, opts, FastOpts);
        return (browser, h);
    }

    // ── ApplicationLaunched tests ─────────────────────────────────────────────

    [Fact]
    public async Task ApplicationLaunched_FiresAfterEnsureInitialized()
    {
        var launchOpts = new LaunchOptions { ApplicationPath = "calc.exe" };
        var (browser, handle) = MakeBrowser(launchOpts);

        ApplicationLaunchedEventArgs? firedArgs = null;
        browser.ApplicationLaunched += (_, args) => { firedArgs = args; };

        await browser.EnsureInitializedAsync();

        Assert.NotNull(firedArgs);
        Assert.Equal(handle.ProcessId, firedArgs.ProcessId);
        Assert.Equal("calc.exe", firedArgs.ExecutablePath);
        Assert.False(firedArgs.WasAttached);
        Assert.False(firedArgs.IsPackagedApp);
    }

    [Fact]
    public async Task ApplicationLaunched_FiresWithAumidWhenLaunchingStoreApp()
    {
        var launchOpts = new LaunchOptions { Aumid = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App" };
        var (browser, _) = MakeBrowser(launchOpts);

        ApplicationLaunchedEventArgs? firedArgs = null;
        browser.ApplicationLaunched += (_, args) => { firedArgs = args; };

        await browser.EnsureInitializedAsync();

        Assert.NotNull(firedArgs);
        Assert.Null(firedArgs.ExecutablePath);
        Assert.Equal("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", firedArgs.Aumid);
    }

    [Fact]
    public async Task ApplicationLaunched_FiresWithWasAttachedTrueWhenAttaching()
    {
        var attachOpts = new AttachOptions { ProcessId = 1234 };
        var h = new FakeApplicationHandle(waitResult: true);
        var launcher = new FakeApplicationLauncher { Handle = h };
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var browser = new FlawrightBrowser(launcher, input, translator, attachOpts, FastOpts);

        ApplicationLaunchedEventArgs? firedArgs = null;
        browser.ApplicationLaunched += (_, args) => { firedArgs = args; };

        await browser.EnsureInitializedAsync();

        Assert.NotNull(firedArgs);
        Assert.True(firedArgs.WasAttached);
    }

    // ── ApplicationClosing and ApplicationClosed tests ───────────────────────

    [Fact]
    public async Task ApplicationClosing_FiresBeforeBehaviorAndApplicationClosed_FiresAfter()
    {
        var (browser, handle) = MakeBrowser();
        await browser.EnsureInitializedAsync();

        var eventOrder = new List<string>();
        browser.ApplicationClosing += (_, _) => { eventOrder.Add("Closing"); };
        browser.ApplicationClosed += (_, _) => { eventOrder.Add("Closed"); };

        await browser.CloseAsync();

        Assert.Equal(ClosingThenClosedOrder, eventOrder, StringComparer.Ordinal);
    }

    [Fact]
    public async Task ApplicationClosed_PayloadReflectsGracefulFlag()
    {
        // Use a handle that stays alive so the graceful close behavior succeeds
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: false);
        var launcher = new FakeApplicationLauncher { Handle = handle };
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var browser = new FlawrightBrowser(launcher, input, translator, opts, FastOpts);
        await browser.EnsureInitializedAsync();

        ApplicationClosedEventArgs? firedArgs = null;
        browser.ApplicationClosed += (_, args) => { firedArgs = args; };

        // Set hasExited to true so that after close completes, the process is gone
        handle.HasExited = true;
        await browser.CloseAsync();

        Assert.NotNull(firedArgs);
        Assert.True(firedArgs.Graceful);
        Assert.True(firedArgs.ExitedCleanly);
    }

    [Fact]
    public async Task ApplicationClosing_ContainsProcessIdAndTimeout()
    {
        var (browser, handle) = MakeBrowser();
        await browser.EnsureInitializedAsync();

        ApplicationClosingEventArgs? firedArgs = null;
        browser.ApplicationClosing += (_, args) => { firedArgs = args; };

        var timeout = TimeSpan.FromSeconds(3);
        await browser.CloseAsync(timeout);

        Assert.NotNull(firedArgs);
        Assert.Equal(handle.ProcessId, firedArgs.ProcessId);
        Assert.Equal(timeout, firedArgs.Timeout);
    }

    // ── Exception handling in event handlers ──────────────────────────────────

    [Fact]
    public async Task EventHandlerException_Swallowed_DoesNotCrash()
    {
        var (browser, _) = MakeBrowser();

        browser.ApplicationLaunched += (_, _) => { throw new InvalidOperationException("Handler error"); };

        // Should not throw; exception should be swallowed
        await browser.EnsureInitializedAsync();
    }

    [Fact]
    public async Task MultipleEventHandlers_OneThrows_OthersStillFire()
    {
        var (browser, _) = MakeBrowser();

        var handler1Called = false;
        var handler2Called = false;

        browser.ApplicationLaunched += (_, _) => { handler1Called = true; };
        browser.ApplicationLaunched += (_, _) => { throw new InvalidOperationException("Handler error"); };
        browser.ApplicationLaunched += (_, _) => { handler2Called = true; };

        await browser.EnsureInitializedAsync();

        Assert.True(handler1Called);
        Assert.True(handler2Called);
    }

    [Fact]
    public async Task ApplicationClosing_ExceptionSwallowed_DoesNotAffectClose()
    {
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: false);
        var launcher = new FakeApplicationLauncher { Handle = handle };
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var browser = new FlawrightBrowser(launcher, input, translator, opts, FastOpts);
        await browser.EnsureInitializedAsync();

        browser.ApplicationClosing += (_, _) => { throw new InvalidOperationException("Handler error"); };
        handle.HasExited = true;

        // Should not throw; exception should be swallowed and close should succeed
        var result = await browser.CloseAsync();
        Assert.True(result);
    }

    // ── WindowDetected tests ──────────────────────────────────────────────────

    [Fact]
    public async Task WindowDetected_FiresForEachWindow_WhenEnabled()
    {
        var optsWithWindowEvents = FastOpts with { EnableWindowEvents = true };
        var launchOpts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var h = new FakeApplicationHandle(waitResult: true);
        var launcher = new FakeApplicationLauncher { Handle = h };
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var browser = new FlawrightBrowser(launcher, input, translator, launchOpts, optsWithWindowEvents);

        await browser.EnsureInitializedAsync();

        var firedEvents = new List<WindowDetectedEventArgs>();
        browser.WindowDetected += (_, args) => { firedEvents.Add(args); };

        await browser.GetAllPagesAsync();

        // FakeApplicationHandle returns one window, so we expect one event
        Assert.Single(firedEvents);
        Assert.Equal(h.ProcessId, firedEvents[0].ProcessId);
    }

    [Fact]
    public async Task WindowDetected_DoesNotFire_WhenDisabled()
    {
        var optsWithoutWindowEvents = FastOpts with { EnableWindowEvents = false };
        var launchOpts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var (browser, _) = MakeBrowser(launchOpts);

        // Re-create browser with explicitly disabled window events
        var h = new FakeApplicationHandle(waitResult: true);
        var launcher = new FakeApplicationLauncher { Handle = h };
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var browserNoEvents = new FlawrightBrowser(launcher, input, translator, launchOpts, optsWithoutWindowEvents);

        await browserNoEvents.EnsureInitializedAsync();

        var eventFired = false;
        browserNoEvents.WindowDetected += (_, _) => { eventFired = true; };

        await browserNoEvents.GetAllPagesAsync();

        Assert.False(eventFired);
    }

    [Fact]
    public async Task WindowDetected_FiresInWaitForPageAsync_WhenEnabled()
    {
        var optsWithWindowEvents = FastOpts with { EnableWindowEvents = true };
        var launchOpts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var h = new FakeApplicationHandle(waitResult: true);
        var launcher = new FakeApplicationLauncher { Handle = h };
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var browser = new FlawrightBrowser(launcher, input, translator, launchOpts, optsWithWindowEvents);

        await browser.EnsureInitializedAsync();

        var firedEvents = new List<WindowDetectedEventArgs>();
        browser.WindowDetected += (_, args) => { firedEvents.Add(args); };

        // Default fake window has name "FakeWindow"
        await browser.WaitForPageAsync("FakeWindow");

        Assert.Single(firedEvents);
        Assert.Equal(h.ProcessId, firedEvents[0].ProcessId);
    }
}

/// <summary>
/// Test resolver that simulates alias resolution.
/// </summary>
internal sealed class TestAumidResolver : IAumidResolver
{
    public string ResolvedAumid { get; set; } = "";

    public LaunchTarget Resolve(string applicationPath)
    {
        if (!string.IsNullOrEmpty(ResolvedAumid))
        {
            return new LaunchTarget(LaunchKind.Aumid, ResolvedAumid);
        }

        return new LaunchTarget(LaunchKind.Path, applicationPath);
    }
}
