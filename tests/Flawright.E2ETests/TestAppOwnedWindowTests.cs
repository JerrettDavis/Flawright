using Flawright;
using Flawright.InputModes;
using Xunit;

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

        // After Cancel the "Save changes?" dialog must no longer be among the owned
        // windows. (EnumWindows can transiently include hidden helper top-levels
        // created by WPF/WinForms — we only assert our specific dialog is gone.)
        await Internals.AutoWait.UntilTrueAsync(
            async ct =>
            {
                var owned = await page.GetOwnedWindowsAsync(ct);
                foreach (var w in owned)
                {
                    var t = await w.TitleAsync(ct);
                    if (string.Equals(t, "Save changes?", StringComparison.Ordinal))
                        return false;
                }
                return true;
            },
            "Save changes? dialog should be gone after Cancel",
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);
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

}
