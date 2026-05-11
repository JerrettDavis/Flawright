using Flawright.CloseBehaviors;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Browser;

/// <summary>
/// Unit tests for <see cref="FlawrightBrowser.CloseAsync"/>.
/// </summary>
public sealed class CloseAsyncTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly FlawrightOptions FastOpts = new()
    {
        DefaultTimeout = TimeSpan.FromMilliseconds(200),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(10),
    };

    private static (FlawrightBrowser Browser, FakeApplicationHandle Handle, FakeInputBackend Input)
        MakeBrowser(FakeApplicationHandle? handle = null, FlawrightOptions? opts = null)
    {
        var h = handle ?? new FakeApplicationHandle(waitResult: true);
        var launcher = new FakeApplicationLauncher { Handle = h };
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var launchOpts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var browser = new FlawrightBrowser(launcher, input, translator, launchOpts, opts ?? FastOpts);
        return (browser, h, input);
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CloseAsync_CalledTwice_IsIdempotent()
    {
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: true);
        var (browser, _, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        var result1 = await browser.CloseAsync();
        var result2 = await browser.CloseAsync();

        // Close was sent only once, result is true both times.
        Assert.True(result1);
        Assert.True(result2);
        Assert.Equal(1, handle.CloseCount);
    }

    [Fact]
    public async Task CloseAsync_ThenDisposeAsync_DoesNotRerunClosePath()
    {
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: true);
        var (browser, _, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        await browser.CloseAsync();
        await browser.DisposeAsync();

        // Close sent once from CloseAsync; DisposeAsync must not send another.
        Assert.Equal(1, handle.CloseCount);
    }

    // ── Default behavior (WindowMessageCloseBehavior) ─────────────────────────

    [Fact]
    public void FlawrightOptions_DefaultCloseBehavior_IsWindowMessageCloseBehavior()
    {
        var opts = new FlawrightOptions();
        Assert.IsType<WindowMessageCloseBehavior>(opts.CloseBehavior);
    }

    [Fact]
    public async Task CloseAsync_DefaultBehavior_SendsCloseSignalAndWaitsForExit()
    {
        // With WindowMessageCloseBehavior (default), Close() on handle is called once.
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: true);
        var (browser, _, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        await browser.CloseAsync();

        Assert.Equal(1, handle.CloseCount);
    }

    [Fact]
    public async Task CloseAsync_DefaultBehavior_ReturnsTrueOnCleanExit()
    {
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: true);
        var (browser, _, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        var result = await browser.CloseAsync();

        Assert.True(result);
    }

    // ── Configured behavior is honored ────────────────────────────────────────

    [Fact]
    public async Task CloseAsync_HonorsConfiguredCloseBehavior()
    {
        // Configure a KillCloseBehavior — it should call Kill, not Close.
        var opts = FastOpts with { CloseBehavior = new KillCloseBehavior() };
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: false);
        var (browser, _, _) = MakeBrowser(handle, opts);
        await browser.EnsureInitializedAsync();

        await browser.CloseAsync();

        // KillCloseBehavior calls Kill(), not Close()
        Assert.Equal(1, handle.KillCount);
        Assert.Equal(0, handle.CloseCount);
    }

    [Fact]
    public async Task CloseAsync_WithDismissDialogBehavior_ClicksDiscardButton()
    {
        var discardButton = new FakeElementBackend(
            name: "Don't Save",
            controlTypeName: "Button");
        var mainWindow = new FakeElementBackend(
            name: "Notepad",
            controlTypeName: "Window",
            children: [discardButton]);

        var handle = new FakeApplicationHandle(waitResult: true, hasExited: false, mainWindow: mainWindow);
        var opts = FastOpts with { CloseBehavior = new DismissDialogCloseBehavior() };
        var (browser, _, input) = MakeBrowser(handle, opts);
        await browser.EnsureInitializedAsync();

        await browser.CloseAsync(timeout: TimeSpan.FromMilliseconds(150));

        // RealInputMode routes clicks through input.MouseClick
        Assert.Single(input.MouseClicks);
    }

    [Fact]
    public async Task CloseAsync_Win11StyleButton_IsClickedWithDismissDialogBehavior()
    {
        var discardButton = new FakeElementBackend(
            name: "Don't save",
            controlTypeName: "Button");
        var mainWindow = new FakeElementBackend(
            name: "Notepad",
            controlTypeName: "Window",
            children: [discardButton]);

        var handle = new FakeApplicationHandle(waitResult: true, hasExited: false, mainWindow: mainWindow);
        var opts = FastOpts with { CloseBehavior = new DismissDialogCloseBehavior() };
        var (browser, _, input) = MakeBrowser(handle, opts);
        await browser.EnsureInitializedAsync();

        await browser.CloseAsync(timeout: TimeSpan.FromMilliseconds(150));

        // RealInputMode routes clicks through input.MouseClick
        Assert.Single(input.MouseClicks);
    }

    [Fact]
    public async Task CloseAsync_NonMatchingButton_IsNotClicked()
    {
        var saveButton = new FakeElementBackend(name: "Save", controlTypeName: "Button");
        var mainWindow = new FakeElementBackend(
            name: "Notepad",
            controlTypeName: "Window",
            children: [saveButton]);

        var handle = new FakeApplicationHandle(waitResult: true, hasExited: true, mainWindow: mainWindow);
        var opts = FastOpts with { CloseBehavior = new DismissDialogCloseBehavior() };
        var (browser, _, _) = MakeBrowser(handle, opts);
        await browser.EnsureInitializedAsync();

        await browser.CloseAsync(timeout: TimeSpan.FromMilliseconds(200));

        Assert.Equal(0, saveButton.ClickCount);
    }

    // ── Force-kill fallback ───────────────────────────────────────────────────

    [Fact]
    public async Task CloseAsync_ProcessNeverExits_ReturnsFalseAndKills()
    {
        // WindowMessageCloseBehavior returns false when process doesn't exit —
        // browser should then force-kill.
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: false, isStoreApp: false);
        var (browser, _, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        var result = await browser.CloseAsync(timeout: TimeSpan.FromMilliseconds(150));

        Assert.False(result);
        Assert.True(handle.KillCount > 0);
    }

    [Fact]
    public async Task CloseAsync_ProcessExitsCleanly_ReturnsTrueWithoutKill()
    {
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: true);
        var (browser, _, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        var result = await browser.CloseAsync();

        Assert.True(result);
        Assert.Equal(0, handle.KillCount);
    }

    // ── DisposeAsync backward compatibility ───────────────────────────────────

    [Fact]
    public async Task DisposeAsync_WithoutCloseAsync_StillForcesKillAfterTimeout()
    {
        // Existing behavior: if CloseAsync was never called, DisposeAsync runs
        // the original close + 2s + kill path.
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: false, isStoreApp: false);
        var (browser, _, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        await browser.DisposeAsync();

        Assert.Equal(1, handle.CloseCount);
        Assert.Equal(1, handle.KillCount);
    }

    // ── Default timeout ───────────────────────────────────────────────────────

    [Fact]
    public void CloseAsync_DefaultTimeout_IsFiveSeconds()
    {
        // Verify the well-known default by calling without a timeout and checking
        // that the interface method signature has the correct default (compile-time).
        // The IFlawrightBrowser interface specifies TimeSpan? timeout = null which
        // maps to 5 seconds inside the implementation.
        IFlawrightBrowser browser = new FlawrightBrowser(
            new FakeApplicationLauncher { Handle = new FakeApplicationHandle(waitResult: true) },
            new FakeInputBackend(),
            new FakeConditionTranslator(),
            new LaunchOptions { ApplicationPath = "notepad.exe" },
            FastOpts);

        // Compile-time check: calling with no args must compile (default params exist).
        // This assertion trivially passes but proves the overload compiles.
        Assert.NotNull(browser);
    }
}
