using Flawright;
using Flawright.InputModes;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// E2E tests for <see cref="IFlawrightAssertions.ToBeFocusedAsync"/> now that
/// the assertion is wired to UIA <c>HasKeyboardFocus</c>.
/// </summary>
/// <remarks>
/// Focus tests require an active desktop session.  On headless CI runners without
/// an interactive desktop, keyboard focus may not transfer between controls
/// as expected.  These tests may be intermittent on environments where the test
/// process window is not in the foreground.
/// </remarks>
public sealed class TestAppFocusTests : IAsyncLifetime
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

    // ── ToBeFocusedAsync — positive ───────────────────────────────────────────

    /// <summary>
    /// After calling <see cref="IFlawrightLocator.FocusAsync"/> on
    /// <c>btnFocusTarget1</c>, <see cref="IFlawrightAssertions.ToBeFocusedAsync"/>
    /// passes.
    /// </summary>
    /// <remarks>
    /// This test requires the application window to be in the foreground so that
    /// UIA <c>HasKeyboardFocus</c> reflects real focus state.  On CI it may be
    /// affected by window activation timing.
    /// </remarks>
    [Fact]
    public async Task ToBeFocusedAsync_OnFocusedButton_Passes()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Activate Menu/Actions tab to make the focus buttons visible.
        await page.Locator("#tabMenuActions").ClickAsync();

        // Bring the application window to the front before focusing.
        await page.BringToFrontAsync();

        var button1 = page.Locator("#btnFocusTarget1");
        await button1.FocusAsync();

        // ToBeFocusedAsync should pass — button 1 now has keyboard focus.
        await button1.Expect().ToBeFocusedAsync();
    }

    // ── ToBeFocusedAsync — negated ─────────────────────────────────────────

    /// <summary>
    /// After focusing <c>btnFocusTarget1</c>, asserting that
    /// <c>btnFocusTarget2</c> is NOT focused should pass.
    /// </summary>
    [Fact]
    public async Task ToBeFocusedAsync_NotFocused_PassesOnNegation()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Activate Menu/Actions tab.
        await page.Locator("#tabMenuActions").ClickAsync();

        await page.BringToFrontAsync();

        var button1 = page.Locator("#btnFocusTarget1");
        var button2 = page.Locator("#btnFocusTarget2");

        // Focus button 1.
        await button1.FocusAsync();

        // Button 2 should NOT be focused.
        await button2.Expect().Not.ToBeFocusedAsync();
    }

    // ── FocusAsync — switch between controls ──────────────────────────────────

    /// <summary>
    /// Focusing button 1 then button 2 transfers keyboard focus to button 2,
    /// which is confirmed via <see cref="IFlawrightAssertions.ToBeFocusedAsync"/>.
    /// </summary>
    [Fact]
    public async Task FocusAsync_SwitchesFocusBetweenControls()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Activate Menu/Actions tab.
        await page.Locator("#tabMenuActions").ClickAsync();

        await page.BringToFrontAsync();

        var button1 = page.Locator("#btnFocusTarget1");
        var button2 = page.Locator("#btnFocusTarget2");

        // Focus button 1, then shift focus to button 2.
        await button1.FocusAsync();
        await button2.FocusAsync();

        // Button 2 should now hold focus.
        await button2.Expect().ToBeFocusedAsync();
    }
}
