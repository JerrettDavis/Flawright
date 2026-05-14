using Flawright;
using Flawright.Backends.Uia;
using Flawright.InputModes;
using Flawright.Locator;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// E2E tests for <c>TabControl</c> navigation and <c>GetByRole</c> resolution
/// of <c>TabItem</c> elements.
/// </summary>
/// <remarks>
/// WPF does not render the content of unselected tab pages into the UIA tree
/// until the tab is activated.  All tests that interact with controls inside a
/// tab must first click the corresponding <c>TabItem</c>.
/// </remarks>
public sealed class TestAppTabControlTests : IAsyncLifetime
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

    // ── Tab switching ──────────────────────────────────────────────────────────

    /// <summary>
    /// Clicking the "Selection" <c>TabItem</c> makes the slider visible;
    /// clicking the "Inputs" <c>TabItem</c> hides the slider and makes the
    /// multi-line TextBox visible.
    /// </summary>
    [Fact]
    public async Task TabControl_SwitchesActiveTab()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Switch to Selection tab via SelectionItemPattern (no focus dependency).
        await page.Locator("#tabSelection").SelectAsync();

        // A Selection-tab control (the slider) should now be visible.
        var sliderVisible = await page.Locator("#sliderVolume").IsVisibleAsync();
        Assert.True(sliderVisible, "sliderVolume should be visible after switching to Selection tab.");

        // Switch to Inputs tab via SelectionItemPattern (no focus dependency).
        await page.Locator("#tabInputs").SelectAsync();

        // An Inputs-tab control (the multi-line TextBox) should now be visible.
        var multilineVisible = await page.Locator("#txtMultiline").IsVisibleAsync();
        Assert.True(multilineVisible, "txtMultiline should be visible after switching to Inputs tab.");

        // The slider should no longer be visible (hidden by WPF tab content management).
        var sliderStillVisible = await page.Locator("#sliderVolume").IsVisibleAsync();
        Assert.False(sliderStillVisible, "sliderVolume should be hidden after switching away from Selection tab.");
    }

    /// <summary>
    /// Switching between Menu/Actions and Inputs tabs confirms bidirectional
    /// tab navigation works correctly.
    /// </summary>
    [Fact]
    public async Task TabControl_SwitchesActiveTab_RoundTrip()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Switch to Menu/Actions via SelectionItemPattern (no focus dependency).
        await page.Locator("#tabMenuActions").SelectAsync();

        var dataGridVisible = await page.Locator("#grdData").IsVisibleAsync();
        Assert.True(dataGridVisible, "grdData should be visible on Menu/Actions tab.");

        // Switch back to Inputs via SelectionItemPattern (no focus dependency).
        await page.Locator("#tabInputs").SelectAsync();

        var txtMultilineVisible = await page.Locator("#txtMultiline").IsVisibleAsync();
        Assert.True(txtMultilineVisible, "txtMultiline should be visible on Inputs tab.");
    }

    // ── GetByRole for TabItem ──────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightPage.GetByRole"/> with <c>AriaRole.Tab</c> and the
    /// name "Inputs" resolves the Inputs <c>TabItem</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>GetByRole(AriaRole.Tab)</c> generates a <c>[role=TabItem]</c> selector
    /// which translates to a native FlaUI <c>ByControlType(ControlType.TabItem)</c>
    /// condition plus a <c>HasName</c> post-filter.  On headless CI the
    /// <c>FindAllDescendants</c> call returns zero TabItem elements for the window
    /// root, causing the locator to time out despite TabItems being accessible via
    /// their AutomationId.  The discrepancy between AutomationId-based and
    /// ControlType-based discovery has not been reproduced interactively and is
    /// suspected to be a FlaUI UIA3 ControlType condition issue on the GitHub
    /// Actions windows-2025 runner.
    /// </para>
    /// <para>
    /// AutomationId-based tab switching (<c>#tabInputs</c>, <c>#tabSelection</c>)
    /// passes reliably and is the recommended approach for CI.
    /// </para>
    /// </remarks>
    [Fact(Skip = "GetByRole(AriaRole.Tab) ControlType condition returns no matches on headless CI despite AutomationId-based tab selectors working; suspected FlaUI UIA3 ControlType discovery issue on Windows Server runners. Use #tabInputs / #tabSelection AutomationId selectors as a workaround.")]
    public async Task TabControl_GetByRole_ResolvesTabItem()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Tab items are visible without needing to activate any specific tab first.
        var inputsTab = page.GetByRole(AriaRole.Tab, new LocatorGetByRoleOptions { Name = "Inputs" });

        // Use auto-waited assertion to tolerate brief UIA settle time after launch.
        await inputsTab.Expect().ToBeVisibleAsync();
    }

    /// <summary>
    /// Clicking the "Selection" tab via <c>GetByRole</c> activates it, making
    /// the slider visible.
    /// </summary>
    /// <remarks>
    /// Same root cause as <see cref="TabControl_GetByRole_ResolvesTabItem"/>:
    /// <c>GetByRole(AriaRole.Tab)</c> fails to find TabItem elements via the
    /// ControlType condition on headless CI.  Use <c>#tabSelection</c> AutomationId
    /// selector with <c>SelectAsync()</c> as a reliable alternative.
    /// </remarks>
    [Fact(Skip = "GetByRole(AriaRole.Tab) ControlType condition returns no matches on headless CI despite AutomationId-based tab selectors working; suspected FlaUI UIA3 ControlType discovery issue on Windows Server runners. Use #tabInputs / #tabSelection AutomationId selectors as a workaround.")]
    public async Task TabControl_GetByRole_ClickActivatesTab()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var selectionTab = page.GetByRole(AriaRole.Tab, new LocatorGetByRoleOptions { Name = "Selection" });
        // Use SelectAsync (SelectionItemPattern) — no focus/SendInput dependency.
        await selectionTab.SelectAsync();

        // Auto-waited assertion: retries until the slider is visible or the default timeout elapses.
        await page.Locator("#sliderVolume").Expect().ToBeVisibleAsync();
    }
}
