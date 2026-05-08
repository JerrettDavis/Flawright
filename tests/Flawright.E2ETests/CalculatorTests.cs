using Flawright;
using Flawright.InputModes;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// E2E tests for Windows Calculator.  Uses <see cref="IAsyncLifetime"/> so
/// that <c>DisposeAsync</c> is guaranteed to run even when a test throws.
/// <see cref="Flawright.DisposeAsync"/> closes the process and kills it
/// if it has not yet exited.
/// </summary>
/// <remarks>
/// Uses <see cref="VirtualInputMode"/> — all actions in this fixture use
/// <c>ClickAsync</c> (via <c>InvokePattern</c>), <c>CountAsync</c>, and
/// <c>ScreenshotAsync</c>, which are all UIA-pattern compatible.  No hover,
/// drag, double-click, or key chords are used.
/// </remarks>
public class CalculatorTests : IAsyncLifetime
{
    private IFlawright? _fw;

    public async Task InitializeAsync()
    {
        // VirtualInputMode: drives Calculator via UIA patterns — no focus-steal,
        // no cursor movement, safe for CI runners.
        _fw = await Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = "calc.exe" },
            new FlawrightOptions
            {
                InputMode = new VirtualInputMode()
            });
    }

    public async Task DisposeAsync()
    {
        if (_fw != null)
            await _fw.DisposeAsync();
    }

    [Fact]
    public async Task Calculator_LaunchAndFindWindow()
    {
        var page = await _fw!.Browser.NewPageAsync();
        Assert.NotNull(page);
    }

    [Fact]
    public async Task Calculator_FindNumberButtons()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var buttonCount = await page.Locator("controltype:Button").CountAsync();
        Assert.True(buttonCount > 0, "Calculator should have buttons");
    }

    [Fact]
    public async Task Calculator_ClickButton()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Calculator uses automation IDs for number buttons
        await page.Locator("#3").First.ClickAsync();
        // Clicking without error means the API works
    }

    [Fact]
    public async Task Calculator_Screenshot()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var screenshot = await page.ScreenshotAsync();
        Assert.NotNull(screenshot);
        Assert.True(screenshot.Length > 0);
    }

    [Fact]
    public async Task Calculator_ExpectButtonsToBeVisible()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("controltype:Button").Expect().ToBeVisibleAsync();
    }
}
