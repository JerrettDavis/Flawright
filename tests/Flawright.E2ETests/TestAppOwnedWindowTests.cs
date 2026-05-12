using Flawright;
using Flawright.InputModes;
using Xunit;
using Xunit.Abstractions;

namespace Flawright.E2ETests;

/// <summary>
/// E2E tests for the owned-window / dialog API:
/// <see cref="IFlawrightPage.WaitForDialogAsync"/>,
/// <see cref="IFlawrightPage.GetOwnedWindowsAsync"/>, and
/// <see cref="IFlawrightPage.GetModalWindowsAsync"/>.
/// </summary>
/// <remarks>
/// Tests use the repo-shipped WPF test app — no external prerequisite is required.
/// The test app's <c>btnShowDialog</c> button opens a <c>SaveChangesDialog</c>
/// window whose title is <c>"Save changes?"</c> and whose buttons are named
/// <c>"Save"</c>, <c>"Don't Save"</c>, and <c>"Cancel"</c>.
/// </remarks>
public sealed class TestAppOwnedWindowTests : IAsyncLifetime
{
    private static readonly string TestAppPath =
        Path.Combine(AppContext.BaseDirectory, "TestApp", "Flawright.E2ETests.TestApp.exe");

    private IFlawright? _fw;
    private readonly ITestOutputHelper _output;

    public TestAppOwnedWindowTests(ITestOutputHelper output) => _output = output;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _fw = await global::Flawright.Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = TestAppPath },
            new FlawrightOptions
            {
                InputMode = new VirtualInputMode(),
                DefaultTimeout = TimeSpan.FromSeconds(10),
            });
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_fw != null)
        {
            await _fw.Browser.CloseAsync();
            await _fw.DisposeAsync();
        }
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    /// <summary>
    /// Opening the dialog then calling <see cref="IFlawrightPage.WaitForDialogAsync()"/>
    /// (no title filter) returns a dialog page; clicking "Cancel" inside it closes
    /// the dialog so that <see cref="IFlawrightPage.GetOwnedWindowsAsync"/> reports
    /// zero owned windows afterwards.
    /// </summary>
    [Fact]
    public async Task WaitForDialog_NoFilter_ReturnsDialogPage_ClickCancelClosesDialog()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Open the SaveChangesDialog.  UIA's InvokePattern.Invoke is non-blocking, so
        // ClickAsync returns immediately even for ShowDialog calls — plain await is correct.
        await page.Locator("#btnShowDialog").ClickAsync();

        // WaitForDialogAsync (no title filter) should return the owned dialog.
        var dialogPage = await page.WaitForDialogAsync();
        Assert.NotNull(dialogPage);

        // The Cancel button must be visible inside the dialog.
        var cancelVisible = await dialogPage.Locator("name:Cancel").IsVisibleAsync();
        Assert.True(cancelVisible, "Cancel button should be visible in the dialog.");

        // Click Cancel — dismisses the dialog.
        await dialogPage.Locator("name:Cancel").ClickAsync();

        // After Cancel the dialog should be gone; owned windows list must be empty.
        var ownedAfter = await page.GetOwnedWindowsAsync();
        Assert.Empty(ownedAfter);
    }

    // ── Title-filter happy path ───────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightPage.WaitForDialogAsync(string?, TimeSpan?, CancellationToken)"/>
    /// with a title substring of <c>"Save"</c> finds the <c>"Save changes?"</c> dialog.
    /// </summary>
    [Fact]
    public async Task WaitForDialog_TitleFilter_MatchesDialog()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // UIA's InvokePattern.Invoke is non-blocking, so ClickAsync returns immediately
        // even for ShowDialog calls — plain await is correct.
        await page.Locator("#btnShowDialog").ClickAsync();

        // WaitForDialogAsync with the "Save" substring should match "Save changes?".
        var dialogPage = await page.WaitForDialogAsync(titlePattern: "Save");
        Assert.NotNull(dialogPage);

        // Confirm we have the right dialog by checking a known button is visible.
        var dontSaveVisible = await dialogPage.Locator("name:Don't Save").IsVisibleAsync();
        Assert.True(dontSaveVisible, "'Don't Save' button should be visible in the matched dialog.");

        // Dismiss to keep cleanup clean.
        await dialogPage.Locator("name:Cancel").ClickAsync();
    }

    // ── Timeout path ─────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightPage.WaitForDialogAsync(string?, TimeSpan?, CancellationToken)"/>
    /// with a non-matching title pattern and a short timeout throws
    /// <see cref="FlawrightTimeoutException"/> rather than hanging.
    /// </summary>
    [Fact]
    public async Task WaitForDialog_NoMatchingTitle_ThrowsTimeoutException()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Do NOT open any dialog — the title "NonExistent" will never appear.
        await Assert.ThrowsAsync<FlawrightTimeoutException>(async () =>
        {
            await page.WaitForDialogAsync(
                titlePattern: "NonExistent",
                timeout: TimeSpan.FromMilliseconds(500));
        });
    }

    // ── Diagnostic ────────────────────────────────────────────────────────────

    /// <summary>
    /// Diagnostic-only test: prints CI-visible output showing whether
    /// <see cref="IFlawrightPage.GetOwnedWindowsAsync"/> ever detects the
    /// SaveChangesDialog and, if so, what HWND and title it reports.
    /// No assertions — the test always passes. Read the xUnit output in CI logs.
    /// </summary>
    [Fact]
    public async Task Diagnostic_DialogDiscovery_PrintsTopLevelWindowsOverTime()
    {
        var page = await _fw!.Browser.NewPageAsync();
        var pageHwnd = GetPageHandle(page);
        _output.WriteLine($"Page (main window) HWND = 0x{pageHwnd:X}");

        // BEFORE click: enumerate top-levels
        var before = await page.GetOwnedWindowsAsync();
        _output.WriteLine($"BEFORE click: GetOwnedWindowsAsync count = {before.Count}");
        foreach (var w in before)
            _output.WriteLine($"  - hwnd=0x{GetPageHandle(w):X} title='{await TryGetTitleAsync(w)}'");

        // Click the dialog-opening button
        await page.Locator("#btnShowDialog").ClickAsync();
        _output.WriteLine("Click dispatched.");

        // Poll every 200 ms for 12 s, logging each snapshot
        for (var i = 0; i < 60; i++)
        {
            await Task.Delay(200);
            var snapshot = await page.GetOwnedWindowsAsync();
            if (snapshot.Count > 0 || i % 5 == 0)
            {
                _output.WriteLine($"t+{(i + 1) * 200}ms: count = {snapshot.Count}");
                foreach (var w in snapshot)
                    _output.WriteLine($"  - hwnd=0x{GetPageHandle(w):X} title='{await TryGetTitleAsync(w)}'");
            }

            if (snapshot.Count > 0 && i > 5)
                break;
        }

        // Diagnostic only — no assertions
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns the native window handle for <paramref name="page"/> by reflecting
    /// into the internal <c>_windowBackend</c> field and reading its
    /// <c>NativeWindowHandle</c> property. Returns <see cref="IntPtr.Zero"/> on
    /// any failure so the diagnostic test degrades gracefully.
    /// </summary>
    private static nint GetPageHandle(IFlawrightPage page)
    {
        try
        {
            var field = page.GetType()
                .GetField("_windowBackend", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
                return IntPtr.Zero;

            var backend = field.GetValue(page);
            if (backend == null)
                return IntPtr.Zero;

            var prop = backend.GetType()
                .GetProperty("NativeWindowHandle", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prop == null)
                return IntPtr.Zero;

            return (nint)(prop.GetValue(backend) ?? IntPtr.Zero);
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// Returns the title of <paramref name="page"/>, or a placeholder string if
    /// <see cref="IFlawrightPage.TitleAsync"/> throws.
    /// </summary>
    private static async Task<string> TryGetTitleAsync(IFlawrightPage page)
    {
        try
        {
            return await page.TitleAsync();
        }
        catch (Exception ex)
        {
            return $"<title-error: {ex.GetType().Name}>";
        }
    }
}
