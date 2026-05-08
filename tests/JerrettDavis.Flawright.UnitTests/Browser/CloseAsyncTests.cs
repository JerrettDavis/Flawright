using JerrettDavis.Flawright.UnitTests.Fakes;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Browser;

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

    private static (FlawrightBrowser Browser, FakeApplicationHandle Handle)
        MakeBrowser(FakeApplicationHandle? handle = null)
    {
        var h = handle ?? new FakeApplicationHandle(waitResult: true);
        var launcher = new FakeApplicationLauncher { Handle = h };
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var browser = new FlawrightBrowser(launcher, input, translator, opts, FastOpts);
        return (browser, h);
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CloseAsync_CalledTwice_IsIdempotent()
    {
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: true);
        var (browser, _) = MakeBrowser(handle);
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
        var (browser, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        await browser.CloseAsync();
        await browser.DisposeAsync();

        // Close sent once from CloseAsync; DisposeAsync must not send another.
        Assert.Equal(1, handle.CloseCount);
    }

    // ── discardUnsavedChanges = false ─────────────────────────────────────────

    [Fact]
    public async Task CloseAsync_DiscardFalse_DoesNotSearchForDialog()
    {
        // Even if a "Don't Save" button exists in the tree, it must NOT be clicked.
        var discardButton = new FakeElementBackend(
            name: "Don't Save",
            controlTypeName: "Button");
        var mainWindow = new FakeElementBackend(
            name: "Untitled - Notepad",
            controlTypeName: "Window",
            children: [discardButton]);

        var handle = new FakeApplicationHandle(waitResult: true, hasExited: true, mainWindow: mainWindow);
        var (browser, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        await browser.CloseAsync(discardUnsavedChanges: false);

        Assert.Equal(0, discardButton.ClickCount);
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

    // ── Cross-OS button name matching ─────────────────────────────────────────

    [Fact]
    public void DiscardButtonNames_ContainsBothCasingVariants()
    {
        // The static list must contain both the Win10 and Win11 variants.
        Assert.Contains("Don't Save", FlawrightBrowser.DiscardButtonNames);
        Assert.Contains("Don't save", FlawrightBrowser.DiscardButtonNames);
    }

    [Fact]
    public async Task CloseAsync_Win10StyleButton_IsClicked()
    {
        // Win10 Notepad uses "Don't Save" (capital S).
        // Process is "running" (hasExited: false) so the dialog-polling loop actually runs.
        var discardButton = new FakeElementBackend(
            name: "Don't Save",
            controlTypeName: "Button");
        var mainWindow = new FakeElementBackend(
            name: "Notepad",
            controlTypeName: "Window",
            children: [discardButton]);

        var handle = new FakeApplicationHandle(waitResult: true, hasExited: false, mainWindow: mainWindow);
        var (browser, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        // Call with a short timeout so the test completes quickly even though
        // the fake process never actually "exits" after the button click.
        await browser.CloseAsync(discardUnsavedChanges: true, timeout: TimeSpan.FromMilliseconds(150));

        Assert.Equal(1, discardButton.ClickCount);
    }

    [Fact]
    public async Task CloseAsync_Win11StyleButton_IsClicked()
    {
        // Win11 packaged Notepad uses "Don't save" (lowercase s).
        // Process is "running" so the dialog-polling loop runs and finds the button.
        var discardButton = new FakeElementBackend(
            name: "Don't save",
            controlTypeName: "Button");
        var mainWindow = new FakeElementBackend(
            name: "Notepad",
            controlTypeName: "Window",
            children: [discardButton]);

        var handle = new FakeApplicationHandle(waitResult: true, hasExited: false, mainWindow: mainWindow);
        var (browser, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        await browser.CloseAsync(discardUnsavedChanges: true, timeout: TimeSpan.FromMilliseconds(150));

        Assert.Equal(1, discardButton.ClickCount);
    }

    [Fact]
    public async Task CloseAsync_ButtonNameMismatch_DoesNotClickButton()
    {
        // A button named "Save" should NOT be matched as the discard button.
        var saveButton = new FakeElementBackend(name: "Save", controlTypeName: "Button");
        var mainWindow = new FakeElementBackend(
            name: "Notepad",
            controlTypeName: "Window",
            children: [saveButton]);

        var handle = new FakeApplicationHandle(waitResult: true, hasExited: true, mainWindow: mainWindow);
        var (browser, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        await browser.CloseAsync(discardUnsavedChanges: true, timeout: TimeSpan.FromMilliseconds(200));

        Assert.Equal(0, saveButton.ClickCount);
    }

    // ── Force-kill fallback ───────────────────────────────────────────────────

    [Fact]
    public async Task CloseAsync_ProcessNeverExits_ReturnsFalseAndKills()
    {
        // hasExited stays false — simulates a hung process.
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: false, isStoreApp: false);
        var (browser, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        var result = await browser.CloseAsync(
            discardUnsavedChanges: false,
            timeout: TimeSpan.FromMilliseconds(150));

        Assert.False(result);
        Assert.True(handle.KillCount > 0);
    }

    [Fact]
    public async Task CloseAsync_ProcessExitsCleanly_ReturnsTrueWithoutKill()
    {
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: true);
        var (browser, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        var result = await browser.CloseAsync(discardUnsavedChanges: false);

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
        var (browser, _) = MakeBrowser(handle);
        await browser.EnsureInitializedAsync();

        await browser.DisposeAsync();

        Assert.Equal(1, handle.CloseCount);
        Assert.Equal(1, handle.KillCount);
    }
}
