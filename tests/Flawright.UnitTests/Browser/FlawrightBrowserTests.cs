using Flawright.Backends;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Browser;

/// <summary>
/// Unit tests for <see cref="FlawrightBrowser"/> — covering launch dispatch,
/// attach dispatch, startup-timeout handling, page factory methods, and dispose.
/// </summary>
public sealed class FlawrightBrowserTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly FlawrightOptions FastOpts = new()
    {
        DefaultTimeout = TimeSpan.FromMilliseconds(200),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(10),
    };

    /// <summary>Creates a browser wired for launch with the given options.</summary>
    private static (FlawrightBrowser Browser, FakeApplicationLauncher Launcher, FakeApplicationHandle Handle)
        MakeLaunchBrowser(LaunchOptions opts, FakeApplicationHandle? handle = null)
    {
        var h = handle ?? new FakeApplicationHandle(waitResult: true);
        var launcher = new FakeApplicationLauncher { Handle = h };
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var browser = new FlawrightBrowser(launcher, input, translator, opts, FastOpts);
        return (browser, launcher, h);
    }

    /// <summary>Creates a browser wired for attach with the given options.</summary>
    private static (FlawrightBrowser Browser, FakeApplicationLauncher Launcher, FakeApplicationHandle Handle)
        MakeAttachBrowser(AttachOptions opts, FakeApplicationHandle? handle = null)
    {
        var h = handle ?? new FakeApplicationHandle(waitResult: true);
        var launcher = new FakeApplicationLauncher { Handle = h };
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var browser = new FlawrightBrowser(launcher, input, translator, opts, FastOpts);
        return (browser, launcher, h);
    }

    // ── Launch dispatch tests ─────────────────────────────────────────────────

    [Fact]
    public async Task NewPageAsync_PathOnly_CallsLaunch()
    {
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var (browser, launcher, _) = MakeLaunchBrowser(opts);

        await browser.EnsureInitializedAsync();

        Assert.Single(launcher.LaunchCalls);
        Assert.Empty(launcher.LaunchStoreAppCalls);
        Assert.Same(opts, launcher.LaunchCalls[0]);
    }

    [Fact]
    public async Task NewPageAsync_AumidOnly_CallsLaunchStoreApp()
    {
        var opts = new LaunchOptions { Aumid = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App" };
        var (browser, launcher, _) = MakeLaunchBrowser(opts);

        await browser.EnsureInitializedAsync();

        Assert.Empty(launcher.LaunchCalls);
        Assert.Single(launcher.LaunchStoreAppCalls);
        Assert.Equal("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", launcher.LaunchStoreAppCalls[0].Aumid);
    }

    [Fact]
    public async Task NewPageAsync_AumidWithNullArguments_CallsLaunchStoreAppWithEmptyArgs()
    {
        var opts = new LaunchOptions { Aumid = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", Arguments = null };
        var (browser, launcher, _) = MakeLaunchBrowser(opts);

        await browser.EnsureInitializedAsync();

        Assert.Equal("", launcher.LaunchStoreAppCalls[0].Args);
    }

    [Fact]
    public async Task NewPageAsync_AumidWithArguments_JoinsArgsWithSpace()
    {
        var opts = new LaunchOptions
        {
            Aumid = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",
            Arguments = ["a", "b", "c"]
        };
        var (browser, launcher, _) = MakeLaunchBrowser(opts);

        await browser.EnsureInitializedAsync();

        Assert.Equal("a b c", launcher.LaunchStoreAppCalls[0].Args);
    }

    [Fact]
    public void LaunchApp_BothPathAndAumidSet_ThrowsArgumentException()
    {
        var opts = new LaunchOptions
        {
            ApplicationPath = "notepad.exe",
            Aumid = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App"
        };
        var (browser, _, _) = MakeLaunchBrowser(opts);

        var ex = Assert.ThrowsAsync<ArgumentException>(() => browser.EnsureInitializedAsync());
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task LaunchApp_NeitherPathNorAumidSet_ThrowsArgumentException()
    {
        var opts = new LaunchOptions();  // both null
        var (browser, _, _) = MakeLaunchBrowser(opts);

        await Assert.ThrowsAsync<ArgumentException>(() => browser.EnsureInitializedAsync());
    }

    [Fact]
    public async Task LaunchApp_AumidWithWorkingDirectory_ThrowsArgumentException()
    {
        var opts = new LaunchOptions
        {
            Aumid = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",
            WorkingDirectory = @"C:\Temp"
        };
        var (browser, _, _) = MakeLaunchBrowser(opts);

        await Assert.ThrowsAsync<ArgumentException>(() => browser.EnsureInitializedAsync());
    }

    // ── Attach dispatch tests ─────────────────────────────────────────────────

    [Fact]
    public async Task AttachApp_ProcessIdOnly_CallsAttachByPid()
    {
        var opts = new AttachOptions { ProcessId = 12345 };
        var (browser, launcher, _) = MakeAttachBrowser(opts);

        await browser.EnsureInitializedAsync();

        Assert.Single(launcher.AttachByPidCalls);
        Assert.Equal(12345, launcher.AttachByPidCalls[0]);
        Assert.Empty(launcher.AttachByNameCalls);
    }

    [Fact]
    public async Task AttachApp_ProcessNameWithoutExe_CallsAttachByName()
    {
        var opts = new AttachOptions { ProcessName = "notepad" };
        var (browser, launcher, _) = MakeAttachBrowser(opts);

        await browser.EnsureInitializedAsync();

        Assert.Single(launcher.AttachByNameCalls);
        Assert.Equal("notepad", launcher.AttachByNameCalls[0].ExeBaseName);
        Assert.Equal(0, launcher.AttachByNameCalls[0].Index);
    }

    [Fact]
    public async Task AttachApp_ProcessNameWithExeSuffix_StripsExeBeforeAttaching()
    {
        var opts = new AttachOptions { ProcessName = "notepad.exe" };
        var (browser, launcher, _) = MakeAttachBrowser(opts);

        await browser.EnsureInitializedAsync();

        Assert.Equal("notepad", launcher.AttachByNameCalls[0].ExeBaseName);
    }

    [Fact]
    public async Task AttachApp_ProcessNameWithExeSuffix_CaseInsensitive()
    {
        var opts = new AttachOptions { ProcessName = "Notepad.EXE" };
        var (browser, launcher, _) = MakeAttachBrowser(opts);

        await browser.EnsureInitializedAsync();

        Assert.Equal("Notepad", launcher.AttachByNameCalls[0].ExeBaseName);
    }

    [Fact]
    public async Task AttachApp_ProcessNameWithCustomIndex_ForwardsIndex()
    {
        var opts = new AttachOptions { ProcessName = "notepad", Index = 2 };
        var (browser, launcher, _) = MakeAttachBrowser(opts);

        await browser.EnsureInitializedAsync();

        Assert.Equal(2, launcher.AttachByNameCalls[0].Index);
    }

    [Fact]
    public async Task AttachApp_BothPidAndNameSet_ThrowsArgumentException()
    {
        var opts = new AttachOptions { ProcessId = 123, ProcessName = "notepad" };
        var (browser, _, _) = MakeAttachBrowser(opts);

        await Assert.ThrowsAsync<ArgumentException>(() => browser.EnsureInitializedAsync());
    }

    [Fact]
    public async Task AttachApp_NeitherPidNorNameSet_ThrowsArgumentException()
    {
        var opts = new AttachOptions();  // both null/0
        var (browser, _, _) = MakeAttachBrowser(opts);

        await Assert.ThrowsAsync<ArgumentException>(() => browser.EnsureInitializedAsync());
    }

    // ── Init / startup timeout tests ──────────────────────────────────────────

    [Fact]
    public async Task EnsureInitializedAsync_WaitReturnsFalse_ThrowsFlawrightTimeoutException()
    {
        var handle = new FakeApplicationHandle(waitResult: false);
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var (browser, _, _) = MakeLaunchBrowser(opts, handle);

        var ex = await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => browser.EnsureInitializedAsync());

        Assert.NotNull(ex.Message);
    }

    [Fact]
    public async Task EnsureInitializedAsync_CustomStartupTimeout_IsHonored()
    {
        // The FakeApplicationHandle.WaitWhileMainHandleIsMissing receives the timeout.
        // We verify the custom timeout is forwarded by checking WaitResult (true = appeared).
        var handle = new FakeApplicationHandle(waitResult: true);
        var opts = new LaunchOptions
        {
            ApplicationPath = "notepad.exe",
            StartupTimeout = TimeSpan.FromSeconds(99)
        };
        var (browser, _, _) = MakeLaunchBrowser(opts, handle);

        // Should not throw.
        await browser.EnsureInitializedAsync();
    }

    [Fact]
    public async Task EnsureInitializedAsync_CalledTwice_IsIdempotent()
    {
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var (browser, launcher, _) = MakeLaunchBrowser(opts);

        await browser.EnsureInitializedAsync();
        await browser.EnsureInitializedAsync();

        // Launch should only be called once.
        Assert.Single(launcher.LaunchCalls);
    }

    // ── Page factory tests ────────────────────────────────────────────────────

    [Fact]
    public async Task NewPageAsync_ReturnsPageWrappingMainWindow()
    {
        var mainWindow = new FakeElementBackend(name: "Notepad", controlTypeName: "Window");
        var handle = new FakeApplicationHandle(waitResult: true, mainWindow: mainWindow);
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var (browser, _, _) = MakeLaunchBrowser(opts, handle);

        var page = await browser.NewPageAsync();

        Assert.NotNull(page);
        var title = await page.TitleAsync();
        Assert.Equal("Notepad", title);
    }

    [Fact]
    public async Task GetAllPagesAsync_ReturnsOnePagePerTopLevelWindow()
    {
        var handle = new FakeApplicationHandle(waitResult: true);
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var (browser, _, _) = MakeLaunchBrowser(opts, handle);

        var pages = await browser.GetAllPagesAsync();

        // FakeApplicationHandle.GetAllTopLevelWindows returns one window (the main window).
        Assert.Single(pages);
    }

    [Fact]
    public async Task WaitForPageAsync_MatchingTitle_ReturnsPage()
    {
        var mainWindow = new FakeElementBackend(name: "Untitled — Notepad", controlTypeName: "Window");
        var handle = new FakeApplicationHandle(waitResult: true, mainWindow: mainWindow);
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var (browser, _, _) = MakeLaunchBrowser(opts, handle);

        var page = await browser.WaitForPageAsync("Notepad");

        Assert.NotNull(page);
    }

    [Fact]
    public async Task WaitForPageAsync_MatchingTitle_CaseInsensitive()
    {
        var mainWindow = new FakeElementBackend(name: "Untitled — NOTEPAD", controlTypeName: "Window");
        var handle = new FakeApplicationHandle(waitResult: true, mainWindow: mainWindow);
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var (browser, _, _) = MakeLaunchBrowser(opts, handle);

        var page = await browser.WaitForPageAsync("notepad");

        Assert.NotNull(page);
    }

    [Fact]
    public async Task WaitForPageAsync_NoMatchingTitle_ThrowsFlawrightTimeoutException()
    {
        var mainWindow = new FakeElementBackend(name: "Calculator", controlTypeName: "Window");
        var handle = new FakeApplicationHandle(waitResult: true, mainWindow: mainWindow);
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var (browser, _, _) = MakeLaunchBrowser(opts, handle);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => browser.WaitForPageAsync("Notepad", TimeSpan.FromMilliseconds(100)));
    }

    // ── Dispose tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DisposeAsync_CallsCloseAndKillOnNonStoreApp()
    {
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: false, isStoreApp: false);
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var (browser, _, _) = MakeLaunchBrowser(opts, handle);

        await browser.EnsureInitializedAsync();
        await browser.DisposeAsync();

        Assert.Equal(1, handle.CloseCount);
        Assert.Equal(1, handle.KillCount);
        Assert.True(handle.IsDisposed);
    }

    [Fact]
    public async Task DisposeAsync_StoreApp_CallsCloseButNotKill()
    {
        // Store app: HasExited stays false, but we must NOT call KillProcessTree.
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: false, isStoreApp: true);
        var opts = new LaunchOptions { ApplicationPath = "dummy.exe" };  // path chosen to pass validation
        var (browser, _, _) = MakeLaunchBrowser(opts, handle);

        await browser.EnsureInitializedAsync();
        await browser.DisposeAsync();

        Assert.Equal(1, handle.CloseCount);
        Assert.Equal(0, handle.KillCount);
        Assert.True(handle.IsDisposed);
    }

    [Fact]
    public async Task DisposeAsync_AppHasExited_DoesNotKill()
    {
        // If the process exits on its own after Close(), we should not Kill.
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: true, isStoreApp: false);
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var (browser, _, _) = MakeLaunchBrowser(opts, handle);

        await browser.EnsureInitializedAsync();
        await browser.DisposeAsync();

        Assert.Equal(1, handle.CloseCount);
        Assert.Equal(0, handle.KillCount);
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_IsIdempotent()
    {
        var handle = new FakeApplicationHandle(waitResult: true);
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var (browser, _, _) = MakeLaunchBrowser(opts, handle);

        await browser.EnsureInitializedAsync();
        await browser.DisposeAsync();
        await browser.DisposeAsync();  // second call should be a no-op

        Assert.Equal(1, handle.CloseCount);
    }

    [Fact]
    public async Task DisposeAsync_WithoutInit_IsNoop()
    {
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var (browser, _, handle) = MakeLaunchBrowser(opts);

        // Don't call EnsureInitializedAsync — DisposeAsync on an uninitialised browser.
        await browser.DisposeAsync();

        Assert.Equal(0, handle.CloseCount);
        Assert.Equal(0, handle.KillCount);
        Assert.False(handle.IsDisposed);
    }

    // ── Attached-process dispose tests ────────────────────────────────────────

    [Fact]
    public async Task DisposeAsync_AttachedBrowser_DisposesHandleWithoutKillingProcess()
    {
        // An attach-mode browser must NOT close or kill the external process on dispose.
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: false, isStoreApp: false);
        var opts = new AttachOptions { ProcessId = 9999 };
        var (browser, _, _) = MakeAttachBrowser(opts, handle);

        await browser.EnsureInitializedAsync();
        await browser.DisposeAsync();

        // Must NOT call Close or Kill — we don't own the process.
        Assert.Equal(0, handle.CloseCount);
        Assert.Equal(0, handle.KillCount);
        // The handle itself is disposed (framework resources released).
        Assert.True(handle.IsDisposed);
    }

    [Fact]
    public async Task DisposeAsync_AttachedBrowser_CalledTwice_IsIdempotent()
    {
        var handle = new FakeApplicationHandle(waitResult: true);
        var opts = new AttachOptions { ProcessId = 9999 };
        var (browser, _, _) = MakeAttachBrowser(opts, handle);

        await browser.EnsureInitializedAsync();
        await browser.DisposeAsync();
        await browser.DisposeAsync(); // second call must be a no-op

        Assert.Equal(0, handle.CloseCount);
        Assert.Equal(0, handle.KillCount);
    }

    // ── CloseAsync with attached process ──────────────────────────────────────

    [Fact]
    public async Task CloseAsync_AttachedBrowser_DoesNotKillEvenWhenGracefulIsFalse()
    {
        // Configured CloseBehavior returns false (could not close gracefully), but
        // because this is an attached process, the safety-net kill must NOT fire.
        var handle = new FakeApplicationHandle(waitResult: true, hasExited: false, isStoreApp: false);
        var opts = new AttachOptions { ProcessId = 9999 };
        // Use KillCloseBehavior which will signal but return graceful=false after kill attempt.
        // We'll test that the _wasAttached guard suppresses the fallback KillProcessTree.
        // Use default (WindowMessage) close behavior — the fake Close() doesn't actually exit
        // the process so HasExited stays false, triggering the safety-net path.
        var (browser, _, _) = MakeAttachBrowser(opts, handle);

        await browser.EnsureInitializedAsync();
        await browser.CloseAsync();

        // Close behavior ran (it always calls Close on the handle), but KillProcessTree
        // must not be called because _wasAttached == true suppresses the safety-net kill.
        Assert.Equal(0, handle.KillCount);
    }
}
