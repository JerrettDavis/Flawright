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

        Assert.Equal(new[] { "Closing", "Closed" }, eventOrder);
    }

    [Fact]
    public async Task ApplicationClosed_PayloadReflectsGracefulFlag()
    {
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: true);
        var (browser, _) = MakeBrowser();
        // Manually inject the handle to control hasExited state
        await browser.EnsureInitializedAsync();

        ApplicationClosedEventArgs? firedArgs = null;
        browser.ApplicationClosed += (_, args) => { firedArgs = args; };

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
        var (browser, _) = MakeBrowser();
        await browser.EnsureInitializedAsync();

        browser.ApplicationClosing += (_, _) => { throw new InvalidOperationException("Handler error"); };

        // Should not throw; exception should be swallowed and close should succeed
        var result = await browser.CloseAsync();
        Assert.True(result);
    }
}
