using Flawright;
using Flawright.InputModes;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// E2E tests validating cross-framework window-type coverage for the owned-window API:
/// WPF modal, WPF modeless owned, WPF tool window, WPF nested dialogs,
/// WinForms modal, WinForms modeless, Win32 MessageBox, and comdlg32 OpenFileDialog.
/// </summary>
/// <remarks>
/// Each test exercises <see cref="IFlawrightPage.WaitForDialogAsync"/> and/or
/// <see cref="IFlawrightPage.GetOwnedWindowsAsync"/> against a different window type
/// to verify that all common Windows UI frameworks are reachable via the HWND-based
/// enumeration backing these methods.
/// </remarks>
public sealed class TestAppWindowTypeTests : IAsyncLifetime
{
    private static readonly string TestAppPath =
        Path.Combine(AppContext.BaseDirectory, "TestApp", "Flawright.E2ETests.TestApp.exe");

    private IFlawright? _fw;

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

    // ── 1. WPF ShowDialog without an Owner ────────────────────────────────────

    /// <summary>
    /// A WPF dialog opened via <c>ShowDialog()</c> with no <c>Owner</c> assigned
    /// still appears in <see cref="IFlawrightPage.GetOwnedWindowsAsync"/> because
    /// the implementation enumerates all top-level windows in the process.
    /// </summary>
    [Fact]
    public async Task WpfShowDialogNoOwner_AppearsInGetOwnedWindowsAsync()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // UIA InvokePattern.Invoke is non-blocking even for ShowDialog — plain await is correct.
        await page.Locator("#btnShowDialogNoOwner").ClickAsync();

        var dialogPage = await page.WaitForDialogAsync(titlePattern: "Ownerless Dialog");
        Assert.NotNull(dialogPage);

        // The inline Window has no buttons — dismiss via Escape (WPF honours it on ShowDialog windows).
        await dialogPage.Keyboard.PressAsync("Escape");
    }

    // ── 2. WPF modeless owned window ─────────────────────────────────────────

    /// <summary>
    /// A modeless WPF <see cref="System.Windows.Window"/> shown via <c>Show()</c>
    /// (not <c>ShowDialog</c>) with <c>Owner = this</c> appears in
    /// <see cref="IFlawrightPage.GetOwnedWindowsAsync"/>.
    /// </summary>
    [Fact]
    public async Task WpfModelessOwnedWindow_AppearsInGetOwnedWindowsAsync()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("#btnShowModelessOwned").ClickAsync();

        var dialogPage = await page.WaitForDialogAsync(titlePattern: "Modeless Owned Window");
        Assert.NotNull(dialogPage);

        // Modeless window — let test teardown (CloseAsync) handle cleanup.
    }

    // ── 3. WPF ToolWindow ─────────────────────────────────────────────────────

    /// <summary>
    /// A WPF <see cref="System.Windows.Window"/> with
    /// <c>WindowStyle = WindowStyle.ToolWindow</c> and an owner appears in
    /// <see cref="IFlawrightPage.GetOwnedWindowsAsync"/>.
    /// </summary>
    [Fact]
    public async Task WpfToolWindow_AppearsInGetOwnedWindowsAsync()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("#btnShowToolWindow").ClickAsync();

        var dialogPage = await page.WaitForDialogAsync(titlePattern: "Tool Window");
        Assert.NotNull(dialogPage);

        // Modeless — let teardown handle it.
    }

    // ── 4. WPF nested dialog (outer owns inner) ───────────────────────────────

    /// <summary>
    /// Opening an outer dialog then triggering an inner dialog from within it
    /// results in the inner dialog appearing in the outer dialog page's
    /// <see cref="IFlawrightPage.WaitForDialogAsync"/> result.
    /// </summary>
    [Fact]
    public async Task WpfNestedDialog_InnerAppearsInOuterOwnedWindows()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Open outer dialog (modal — UIA click is non-blocking from this side).
        await page.Locator("#btnShowNestedDialog").ClickAsync();

        var outerPage = await page.WaitForDialogAsync(titlePattern: "Outer Dialog");
        Assert.NotNull(outerPage);

        // From within the outer dialog, open the inner dialog.
        await outerPage.Locator("#btnOpenInner").ClickAsync();

        var innerPage = await outerPage.WaitForDialogAsync(titlePattern: "Inner");
        Assert.NotNull(innerPage);

        // Dismiss inner via its Close button, then outer closes naturally.
        await innerPage.Locator("name:Close").ClickAsync();

        // Outer dialog is still up — wait is not needed; just dismiss it by
        // closing the application in teardown (CloseAsync). We have verified
        // the inner dialog was found.
    }

    // ── 5. WinForms modal ─────────────────────────────────────────────────────

    /// <summary>
    /// A <see cref="System.Windows.Forms.Form"/> shown via <c>ShowDialog</c>
    /// appears in <see cref="IFlawrightPage.GetOwnedWindowsAsync"/>.
    /// </summary>
    [Fact]
    public async Task WinFormsModal_AppearsInGetOwnedWindowsAsync()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("#btnShowWinFormsModal").ClickAsync();

        var dialogPage = await page.WaitForDialogAsync(titlePattern: "WinForms Modal");
        Assert.NotNull(dialogPage);

        // Click the Close button inside the WinForms form.
        await dialogPage.Locator("name:Close").ClickAsync();
    }

    // ── 6. WinForms modeless ──────────────────────────────────────────────────

    /// <summary>
    /// A <see cref="System.Windows.Forms.Form"/> shown via <c>Show()</c>
    /// (modeless) appears in <see cref="IFlawrightPage.GetOwnedWindowsAsync"/>.
    /// </summary>
    /// <remarks>
    /// Uses an extended <see cref="IFlawrightPage.WaitForDialogAsync"/> timeout
    /// (30s vs. the suite's 10s default). Under loaded, parallel CI runs on the
    /// hosted Windows runner, the WinForms modeless <c>Form</c> can take longer
    /// than 10s for its window/title to materialize in UIA's owned-window
    /// enumeration, causing an intermittent <see cref="FlawrightTimeoutException"/>
    /// even though the dialog reliably appears — just later than the default
    /// window. This does not weaken the assertion, it only tolerates slower
    /// CI-only window materialization.
    /// </remarks>
    [Fact]
    public async Task WinFormsModeless_AppearsInGetOwnedWindowsAsync()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("#btnShowWinFormsModeless").ClickAsync();

        var dialogPage = await page.WaitForDialogAsync(
            titlePattern: "WinForms Modeless",
            timeout: TimeSpan.FromSeconds(30));
        Assert.NotNull(dialogPage);

        await dialogPage.Locator("name:Close").ClickAsync();
    }

    // ── 7. Win32 MessageBox ───────────────────────────────────────────────────

    /// <summary>
    /// A Win32 <c>MessageBox</c> (system-modal) appears in
    /// <see cref="IFlawrightPage.GetOwnedWindowsAsync"/>. The test dismisses it
    /// via UIA click on the OK button.
    /// </summary>
    [Fact]
    public async Task MessageBox_AppearsInGetOwnedWindowsAsync()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // MessageBox.Show blocks the dispatcher but UIA click is non-blocking from the test.
        await page.Locator("#btnShowMessageBox").ClickAsync();

        var dialogPage = await page.WaitForDialogAsync(titlePattern: "Test MessageBox");
        Assert.NotNull(dialogPage);

        // Dismiss via OK button (UIA name match).
        await dialogPage.Locator("name:OK").ClickAsync();
    }

    // ── 8. comdlg32 OpenFileDialog ────────────────────────────────────────────

    /// <summary>
    /// A <see cref="System.Windows.Forms.OpenFileDialog"/> appears in
    /// <see cref="IFlawrightPage.GetOwnedWindowsAsync"/>. The test dismisses it
    /// via Escape key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="System.Windows.Forms.OpenFileDialog"/> is used instead of
    /// <see cref="Microsoft.Win32.OpenFileDialog"/> because Windows 11 / Server 2025
    /// routes the Win32 picker through a new out-of-process XAML host (PickerHost.exe),
    /// which does not appear in <see cref="IFlawrightPage.GetOwnedWindowsAsync"/>.
    /// The WinForms picker uses the legacy in-process comdlg32 path.
    /// </para>
    /// <para>
    /// The title "Open" is the default comdlg32 / WinForms picker title and is
    /// used here so the test is not sensitive to OS-level title overrides.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task OpenFileDialog_AppearsInGetOwnedWindowsAsync()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("#btnShowOpenFileDialog").ClickAsync();

        // Accept either the app-supplied title or the OS-level "Open" title.
        var dialogPage = await page.WaitForDialogAsync(
            titlePattern: "Open",
            timeout: TimeSpan.FromSeconds(30));
        Assert.NotNull(dialogPage);

        // Dismiss via Escape.
        await dialogPage.Keyboard.PressAsync("Escape");
    }
}
