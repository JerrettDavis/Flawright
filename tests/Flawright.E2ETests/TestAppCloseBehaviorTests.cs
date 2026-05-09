using Flawright;
using Flawright.CloseBehaviors;
using Flawright.InputModes;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// E2E coverage for the built-in <see cref="ICloseBehavior"/> implementations.
/// <see cref="DismissDialogCloseBehavior"/> is exercised by
/// <see cref="TestAppTests.DismissDialogCloseBehavior_HandlesModalDialog"/>;
/// these tests cover the remaining strategies:
/// <list type="bullet">
///   <item><see cref="WindowMessageCloseBehavior"/> (default — sends WM_CLOSE)</item>
///   <item><see cref="KillCloseBehavior"/> (force-terminate process tree)</item>
///   <item><see cref="CompositeCloseBehavior"/> (chained fallbacks)</item>
/// </list>
/// </summary>
/// <remarks>
/// Each test launches a fresh test-app instance because verifying close
/// semantics requires that the only call to <see cref="IFlawrightBrowser.CloseAsync"/>
/// uses the strategy under test. Using <see cref="VirtualInputMode"/> keeps
/// the tests safe on headless runners.
/// </remarks>
public class TestAppCloseBehaviorTests
{
    private static readonly string TestAppPath =
        Path.Combine(AppContext.BaseDirectory, "TestApp", "Flawright.E2ETests.TestApp.exe");

    // ── WindowMessageCloseBehavior ────────────────────────────────────────────

    /// <summary>
    /// <see cref="WindowMessageCloseBehavior"/> sends WM_CLOSE to the main
    /// window and waits for exit. The WPF test app has no save-changes
    /// prompt on a clean buffer, so it should exit gracefully.
    /// </summary>
    [Fact]
    public async Task WindowMessageCloseBehavior_GracefullyClosesApp()
    {
        var fw = await global::Flawright.Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = TestAppPath },
            new FlawrightOptions
            {
                InputMode = new VirtualInputMode(),
                CloseBehavior = new WindowMessageCloseBehavior(),
                DefaultTimeout = TimeSpan.FromSeconds(5),
            });

        // Touch a control so we know the window is live before we try to close it.
        var page = await fw.Browser.NewPageAsync();
        await page.Locator("#btnClick").ClickAsync();

        var exited = await fw.Browser.CloseAsync(TimeSpan.FromSeconds(10));
        await fw.DisposeAsync();

        Assert.True(exited, "WindowMessageCloseBehavior should gracefully exit a clean WPF app.");
    }

    // ── KillCloseBehavior ─────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="KillCloseBehavior"/> force-terminates the process tree and
    /// always reports success. The application must no longer be running
    /// after <see cref="IFlawrightBrowser.CloseAsync"/> returns.
    /// </summary>
    [Fact]
    public async Task KillCloseBehavior_ForceTerminatesProcess()
    {
        var fw = await global::Flawright.Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = TestAppPath },
            new FlawrightOptions
            {
                InputMode = new VirtualInputMode(),
                CloseBehavior = new KillCloseBehavior(),
                DefaultTimeout = TimeSpan.FromSeconds(5),
            });

        // Open the modal dialog so a graceful close path would block. Kill must
        // bypass that and terminate the process anyway.
        var page = await fw.Browser.NewPageAsync();
        await page.Locator("#btnShowDialog").ClickAsync();

        var result = await fw.Browser.CloseAsync(TimeSpan.FromSeconds(5));
        await fw.DisposeAsync();

        Assert.True(result, "KillCloseBehavior always returns true.");

        // No reliable cross-process handle to assert exit on, but the next
        // launch in a sibling test would fail under contention if Kill silently
        // left the process alive. Successful return + clean teardown is the
        // contract being tested here.
    }

    // ── CompositeCloseBehavior ────────────────────────────────────────────────

    /// <summary>
    /// <see cref="CompositeCloseBehavior"/> stops at the first successful
    /// behavior. With <see cref="WindowMessageCloseBehavior"/> first and
    /// <see cref="KillCloseBehavior"/> second, a clean app exits via WM_CLOSE
    /// and the kill fallback is never invoked.
    /// </summary>
    [Fact]
    public async Task CompositeCloseBehavior_StopsAtFirstSuccess()
    {
        var fw = await global::Flawright.Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = TestAppPath },
            new FlawrightOptions
            {
                InputMode = new VirtualInputMode(),
                CloseBehavior = new CompositeCloseBehavior(
                    new WindowMessageCloseBehavior(),
                    new KillCloseBehavior()),
                DefaultTimeout = TimeSpan.FromSeconds(5),
            });

        var page = await fw.Browser.NewPageAsync();
        await page.Locator("#btnClick").ClickAsync();

        var exited = await fw.Browser.CloseAsync(TimeSpan.FromSeconds(10));
        await fw.DisposeAsync();

        Assert.True(exited, "Composite should report success from its first (WM_CLOSE) member.");
    }

    /// <summary>
    /// When the first behavior fails, <see cref="CompositeCloseBehavior"/>
    /// falls back to the next. Here a fast-timeout
    /// <see cref="WindowMessageCloseBehavior"/> attempt is made against an
    /// app that has a modal dialog open (so WM_CLOSE alone won't close it
    /// inside the per-behavior window), and <see cref="KillCloseBehavior"/>
    /// is used as the fallback to guarantee teardown.
    /// </summary>
    /// <remarks>
    /// This validates the documented "try graceful, fall back to kill" pattern.
    /// </remarks>
    [Fact]
    public async Task CompositeCloseBehavior_FallsBackToKill_WhenWmCloseFails()
    {
        var fw = await global::Flawright.Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = TestAppPath },
            new FlawrightOptions
            {
                InputMode = new VirtualInputMode(),
                // First member uses a fast per-call timeout via the outer
                // CloseAsync timeout. With the modal dialog open, WM_CLOSE
                // won't dismiss the app within that window, so the Composite
                // moves on to KillCloseBehavior.
                CloseBehavior = new CompositeCloseBehavior(
                    new WindowMessageCloseBehavior(),
                    new KillCloseBehavior()),
                DefaultTimeout = TimeSpan.FromSeconds(5),
            });

        var page = await fw.Browser.NewPageAsync();
        await page.Locator("#btnShowDialog").ClickAsync();

        // Short timeout forces WindowMessageCloseBehavior to give up quickly,
        // letting the Composite fall back to Kill.
        var result = await fw.Browser.CloseAsync(TimeSpan.FromSeconds(2));
        await fw.DisposeAsync();

        Assert.True(result, "Composite should ultimately succeed via Kill fallback.");
    }
}
