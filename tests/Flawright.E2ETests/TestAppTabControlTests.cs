using Flawright;
using Flawright.InputModes;
using Flawright.Selectors;
using Flawright.Locator;
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

        // Switch to Selection tab.
        await page.Locator("#tabSelection").ClickAsync();

        // A Selection-tab control (the slider) should now be visible.
        var sliderVisible = await page.Locator("#sliderVolume").IsVisibleAsync();
        Assert.True(sliderVisible, "sliderVolume should be visible after switching to Selection tab.");

        // Switch to Inputs tab.
        await page.Locator("#tabInputs").ClickAsync();

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

        // Switch to Menu/Actions.
        await page.Locator("#tabMenuActions").ClickAsync();

        var dataGridVisible = await page.Locator("#grdData").IsVisibleAsync();
        Assert.True(dataGridVisible, "grdData should be visible on Menu/Actions tab.");

        // Switch back to Inputs.
        await page.Locator("#tabInputs").ClickAsync();

        var txtMultilineVisible = await page.Locator("#txtMultiline").IsVisibleAsync();
        Assert.True(txtMultilineVisible, "txtMultiline should be visible on Inputs tab.");
    }

    // ── GetByRole for TabItem ──────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightPage.GetByRole"/> with <c>AriaRole.Tab</c> and the
    /// name "Inputs" resolves the Inputs <c>TabItem</c>.
    /// </summary>
    [Fact]
    public async Task TabControl_GetByRole_ResolvesTabItem()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Tab items are visible without needing to activate any specific tab first.
        var inputsTab = page.GetByRole(AriaRole.Tab, new LocatorGetByRoleOptions { Name = "Inputs" });

        var isVisible = await inputsTab.IsVisibleAsync();
        Assert.True(isVisible, "Inputs TabItem should be resolvable via GetByRole(Tab, {Name='Inputs'}).");
    }

    /// <summary>
    /// Clicking the "Selection" tab via <c>GetByRole</c> activates it, making
    /// the slider visible.
    /// </summary>
    [Fact]
    public async Task TabControl_GetByRole_ClickActivatesTab()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var selectionTab = page.GetByRole(AriaRole.Tab, new LocatorGetByRoleOptions { Name = "Selection" });
        await selectionTab.ClickAsync();

        var sliderVisible = await page.Locator("#sliderVolume").IsVisibleAsync();
        Assert.True(sliderVisible, "sliderVolume should be visible after activating Selection tab via GetByRole.");
    }
}
