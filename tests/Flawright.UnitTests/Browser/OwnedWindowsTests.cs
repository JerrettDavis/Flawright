using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Browser;

/// <summary>
/// Unit tests for the owned-window and dialog discovery APIs introduced in Wave 1:
/// <see cref="IFlawrightPage.GetOwnedWindowsAsync"/>,
/// <see cref="IFlawrightPage.GetModalWindowsAsync"/>,
/// <see cref="IFlawrightPage.WaitForDialogAsync"/>, and the
/// <see cref="IFlawrightBrowserEvents.DialogOpened"/> event.
/// </summary>
public sealed class OwnedWindowsTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly FlawrightOptions FastOpts = new()
    {
        DefaultTimeout = TimeSpan.FromMilliseconds(200),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(10),
    };

    private static readonly FlawrightOptions FastOptsWithEvents = new()
    {
        DefaultTimeout = TimeSpan.FromMilliseconds(200),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(10),
        EnableWindowEvents = true,
    };

    /// <summary>
    /// Creates a browser + page with a controllable fake application handle.
    /// </summary>
    private static (FlawrightBrowser Browser, FakeApplicationHandle Handle, FlawrightPage Page)
        MakeBrowserAndPage(FlawrightOptions? opts = null, nint pageWindowHandle = default)
    {
        var effectiveOpts = opts ?? FastOpts;
        var mainWindow = new FakeElementBackend(name: "MainWindow", controlTypeName: "Window");
        mainWindow.FakeNativeWindowHandle = pageWindowHandle == default ? new nint(100) : pageWindowHandle;

        var handle = new FakeApplicationHandle(mainWindow: mainWindow);
        var launcher = new FakeApplicationLauncher { Handle = handle };
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var browser = new FlawrightBrowser(launcher, input, translator,
            new LaunchOptions { ApplicationPath = "test.exe" }, effectiveOpts);

        // Manually wire the page so we skip the async launch path.
        var page = new FlawrightPage(mainWindow, input, effectiveOpts, translator, browser, handle);
        return (browser, handle, page);
    }

    // ── GetOwnedWindowsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetOwnedWindowsAsync_NoOwnedWindows_ReturnsEmpty()
    {
        var (_, _, page) = MakeBrowserAndPage();

        var result = await page.GetOwnedWindowsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOwnedWindowsAsync_ReturnsWrappedPagesForOwnedWindows()
    {
        var ownerHwnd = new nint(100);
        var (_, handle, page) = MakeBrowserAndPage(pageWindowHandle: ownerHwnd);

        var dialog1 = new FakeElementBackend(name: "Save Dialog", controlTypeName: "Window");
        dialog1.FakeNativeWindowHandle = new nint(200);
        var dialog2 = new FakeElementBackend(name: "About Box", controlTypeName: "Window");
        dialog2.FakeNativeWindowHandle = new nint(201);

        handle.OwnedWindowsByHandle[ownerHwnd] = [dialog1, dialog2];

        var result = await page.GetOwnedWindowsAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetOwnedWindowsAsync_EachOwnedWindowHasCorrectTitle()
    {
        var ownerHwnd = new nint(100);
        var (_, handle, page) = MakeBrowserAndPage(pageWindowHandle: ownerHwnd);

        var dialog1 = new FakeElementBackend(name: "Save Dialog", controlTypeName: "Window");
        dialog1.FakeNativeWindowHandle = new nint(200);
        handle.OwnedWindowsByHandle[ownerHwnd] = [dialog1];

        var result = await page.GetOwnedWindowsAsync();

        var title = await result[0].TitleAsync();
        Assert.Equal("Save Dialog", title);
    }

    [Fact]
    public async Task GetOwnedWindowsAsync_NoApp_ReturnsEmpty()
    {
        // Page created without an app handle (direct construction, no browser).
        var mainWindow = new FakeElementBackend(name: "Window", controlTypeName: "Window");
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var page = new FlawrightPage(mainWindow, input, FastOpts, translator);

        var result = await page.GetOwnedWindowsAsync();

        Assert.Empty(result);
    }

    // ── GetModalWindowsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetModalWindowsAsync_NoModals_ReturnsEmpty()
    {
        var (_, _, page) = MakeBrowserAndPage();

        var result = await page.GetModalWindowsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetModalWindowsAsync_ReturnsModalWindowsFromBackend()
    {
        var ownerHwnd = new nint(100);
        var mainWindow = new FakeElementBackend(name: "MainWindow", controlTypeName: "Window");
        mainWindow.FakeNativeWindowHandle = ownerHwnd;

        var modalWindow = new FakeElementBackend(name: "Modal Dialog", controlTypeName: "Window");
        modalWindow.FakeNativeWindowHandle = new nint(300);
        mainWindow.FakeModalWindows.Add(modalWindow);

        var handle = new FakeApplicationHandle(mainWindow: mainWindow);
        var launcher = new FakeApplicationLauncher { Handle = handle };
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var browser = new FlawrightBrowser(launcher, input, translator,
            new LaunchOptions { ApplicationPath = "test.exe" }, FastOpts);
        var page = new FlawrightPage(mainWindow, input, FastOpts, translator, browser, handle);

        var result = await page.GetModalWindowsAsync();

        Assert.Single(result);
        var title = await result[0].TitleAsync();
        Assert.Equal("Modal Dialog", title);
    }

    [Fact]
    public async Task GetModalWindowsAsync_NonWindowElement_ReturnsEmpty()
    {
        // An element that is not a window will have no FakeModalWindows.
        var nonWindowBackend = new FakeElementBackend(name: "Button", controlTypeName: "Button");
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var page = new FlawrightPage(nonWindowBackend, input, FastOpts, translator);

        var result = await page.GetModalWindowsAsync();

        Assert.Empty(result);
    }

    // ── WaitForDialogAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task WaitForDialogAsync_NullTitlePattern_ReturnsFirstOwnedWindow()
    {
        var ownerHwnd = new nint(100);
        var (_, handle, page) = MakeBrowserAndPage(pageWindowHandle: ownerHwnd);

        var dialog = new FakeElementBackend(name: "Some Dialog", controlTypeName: "Window");
        dialog.FakeNativeWindowHandle = new nint(200);
        handle.OwnedWindowsByHandle[ownerHwnd] = [dialog];

        var result = await page.WaitForDialogAsync(titlePattern: null);

        Assert.NotNull(result);
        var title = await result.TitleAsync();
        Assert.Equal("Some Dialog", title);
    }

    [Fact]
    public async Task WaitForDialogAsync_MatchingTitlePattern_ReturnsMatchingDialog()
    {
        var ownerHwnd = new nint(100);
        var (_, handle, page) = MakeBrowserAndPage(pageWindowHandle: ownerHwnd);

        var dialog = new FakeElementBackend(name: "Unsaved Changes", controlTypeName: "Window");
        dialog.FakeNativeWindowHandle = new nint(200);
        handle.OwnedWindowsByHandle[ownerHwnd] = [dialog];

        var result = await page.WaitForDialogAsync(titlePattern: "unsaved");

        Assert.NotNull(result);
        var title = await result.TitleAsync();
        Assert.Equal("Unsaved Changes", title);
    }

    [Fact]
    public async Task WaitForDialogAsync_NoMatchWithinTimeout_ThrowsFlawrightTimeoutException()
    {
        var ownerHwnd = new nint(100);
        var (_, _, page) = MakeBrowserAndPage(pageWindowHandle: ownerHwnd);
        // No owned windows registered — nothing to match.

        await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => page.WaitForDialogAsync(titlePattern: "NonExistent"));
    }

    [Fact]
    public async Task WaitForDialogAsync_NoApp_ThrowsFlawrightTimeoutException()
    {
        var mainWindow = new FakeElementBackend(name: "Window", controlTypeName: "Window");
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var page = new FlawrightPage(mainWindow, input, FastOpts, translator);

        await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => page.WaitForDialogAsync());
    }

    [Fact]
    public async Task WaitForDialogAsync_CaseInsensitiveMatch()
    {
        var ownerHwnd = new nint(100);
        var (_, handle, page) = MakeBrowserAndPage(pageWindowHandle: ownerHwnd);

        var dialog = new FakeElementBackend(name: "WARNING: Unsaved", controlTypeName: "Window");
        dialog.FakeNativeWindowHandle = new nint(200);
        handle.OwnedWindowsByHandle[ownerHwnd] = [dialog];

        // Pattern uses different casing from the actual title.
        var result = await page.WaitForDialogAsync(titlePattern: "warning");

        var title = await result.TitleAsync();
        Assert.Equal("WARNING: Unsaved", title);
    }

    // ── DialogOpened event ────────────────────────────────────────────────────

    [Fact]
    public async Task WaitForDialogAsync_FiresDialogOpenedEvent()
    {
        var ownerHwnd = new nint(100);
        var (browser, handle, page) = MakeBrowserAndPage(pageWindowHandle: ownerHwnd);

        var dialog = new FakeElementBackend(name: "Confirm Close", controlTypeName: "Window");
        dialog.FakeNativeWindowHandle = new nint(200);
        handle.OwnedWindowsByHandle[ownerHwnd] = [dialog];

        DialogOpenedEventArgs? firedArgs = null;
        browser.DialogOpened += (_, args) => { firedArgs = args; };

        await page.WaitForDialogAsync();

        Assert.NotNull(firedArgs);
        Assert.Equal("Confirm Close", firedArgs.DialogTitle);
        Assert.Equal(ownerHwnd, firedArgs.ParentWindowHandle);
        Assert.Equal(new nint(200), firedArgs.DialogWindowHandle);
        Assert.Equal(handle.ProcessId, firedArgs.ParentProcessId);
    }

    [Fact]
    public async Task GetOwnedWindowsAsync_WithEventsEnabled_FiresDialogOpenedForEachWindow()
    {
        var ownerHwnd = new nint(100);
        var (browser, handle, page) = MakeBrowserAndPage(
            opts: FastOptsWithEvents, pageWindowHandle: ownerHwnd);

        var dialog1 = new FakeElementBackend(name: "Dialog 1", controlTypeName: "Window");
        dialog1.FakeNativeWindowHandle = new nint(201);
        var dialog2 = new FakeElementBackend(name: "Dialog 2", controlTypeName: "Window");
        dialog2.FakeNativeWindowHandle = new nint(202);

        handle.OwnedWindowsByHandle[ownerHwnd] = [dialog1, dialog2];

        var firedArgs = new List<DialogOpenedEventArgs>();
        browser.DialogOpened += (_, args) => firedArgs.Add(args);

        await page.GetOwnedWindowsAsync();

        Assert.Equal(2, firedArgs.Count);
        Assert.Contains(firedArgs, a => string.Equals(a.DialogTitle, "Dialog 1", StringComparison.Ordinal));
        Assert.Contains(firedArgs, a => string.Equals(a.DialogTitle, "Dialog 2", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetOwnedWindowsAsync_FiresDialogOpenedEvenWithEventsDisabled()
    {
        var ownerHwnd = new nint(100);
        var (browser, handle, page) = MakeBrowserAndPage(
            opts: FastOpts, pageWindowHandle: ownerHwnd);  // EnableWindowEvents = false

        var dialog = new FakeElementBackend(name: "Some Dialog", controlTypeName: "Window");
        dialog.FakeNativeWindowHandle = new nint(201);
        handle.OwnedWindowsByHandle[ownerHwnd] = [dialog];

        int eventCount = 0;
        browser.DialogOpened += (_, _) => eventCount++;

        await page.GetOwnedWindowsAsync();

        // DialogOpened should fire regardless of EnableWindowEvents (that flag only gates WindowDetected).
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public async Task DialogOpenedEvent_DedupsAcrossRepeatedCalls()
    {
        var ownerHwnd = new nint(100);
        var (browser, handle, page) = MakeBrowserAndPage(pageWindowHandle: ownerHwnd);

        var dialog = new FakeElementBackend(name: "Test Dialog", controlTypeName: "Window");
        dialog.FakeNativeWindowHandle = new nint(200);
        handle.OwnedWindowsByHandle[ownerHwnd] = [dialog];

        int eventCount = 0;
        browser.DialogOpened += (_, _) => eventCount++;

        // Call GetOwnedWindowsAsync twice while the same dialog is still open.
        await page.GetOwnedWindowsAsync();
        await page.GetOwnedWindowsAsync();

        // Event should fire only once (deduped).
        Assert.Equal(1, eventCount);
    }

    // ── DialogOpenedEventArgs ─────────────────────────────────────────────────

    [Fact]
    public void DialogOpenedEventArgs_StoresAllProperties()
    {
        var args = new DialogOpenedEventArgs(
            parentProcessId: 42,
            parentWindowHandle: new nint(100),
            dialogWindowHandle: new nint(200),
            dialogTitle: "My Dialog",
            isModal: true);

        Assert.Equal(42, args.ParentProcessId);
        Assert.Equal(new nint(100), args.ParentWindowHandle);
        Assert.Equal(new nint(200), args.DialogWindowHandle);
        Assert.Equal("My Dialog", args.DialogTitle);
        Assert.True(args.IsModal);
    }

    [Fact]
    public void DialogOpenedEventArgs_IsEventArgs()
    {
        var args = new DialogOpenedEventArgs(1, IntPtr.Zero, IntPtr.Zero, null, false);
        Assert.IsAssignableFrom<EventArgs>(args);
    }
}
