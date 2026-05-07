using JerrettDavis.Flawright;
using Xunit;

namespace JerrettDavis.Flawright.E2ETests;

public class CalculatorTests : IDisposable
{
    [Fact]
    public async Task Calculator_LaunchAndFindWindow()
    {
        await using var flawright = await Flawright.CreateAsync();
        var browser = await flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "calc.exe"
        });
        var page = await browser.NewPageAsync();

        Assert.NotNull(page);
    }

    [Fact]
    public async Task Calculator_FindNumberButtons()
    {
        await using var flawright = await Flawright.CreateAsync();
        await using var browser = await flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "calc.exe"
        });
        var page = await browser.NewPageAsync();

        var buttonCount = await page.Locator("controltype:Button").CountAsync();
        Assert.True(buttonCount > 0, "Calculator should have buttons");
    }

    [Fact]
    public async Task Calculator_ClickButton()
    {
        await using var flawright = await Flawright.CreateAsync();
        await using var browser = await flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "calc.exe"
        });
        var page = await browser.NewPageAsync();

        // Calculator uses automation IDs for number buttons
        var button3 = await page.Locator("#3").FirstAsync();
        await button3.ClickAsync();
        // Clicking without error means the API works
    }

    [Fact]
    public async Task Calculator_Screenshot()
    {
        await using var flawright = await Flawright.CreateAsync();
        await using var browser = await flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "calc.exe"
        });
        var page = await browser.NewPageAsync();

        var screenshot = await page.ScreenshotAsync();
        Assert.NotNull(screenshot);
        Assert.True(screenshot.Length > 0);
    }

    [Fact]
    public async Task Calculator_ExpectButtonsToBeVisible()
    {
        await using var flawright = await Flawright.CreateAsync();
        await using var browser = await flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "calc.exe"
        });
        var page = await browser.NewPageAsync();

        await page.Locator("controltype:Button").Expect().ToBeVisibleAsync();
    }

    public void Dispose()
    {
    }
}