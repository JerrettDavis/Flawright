using JerrettDavis.Flawright;
using Xunit;

namespace JerrettDavis.Flawright.E2ETests;

public class NotepadTests : IDisposable
{
    [Fact]
    public async Task Notepad_LaunchAndFindWindow()
    {
        await using var flawright = await Flawright.CreateAsync();
        var browser = await flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"
        });
        var page = await browser.NewPageAsync();

        Assert.NotNull(page);
    }

    [Fact]
    public async Task Notepad_FindTextBox_ByControlType()
    {
        await using var flawright = await Flawright.CreateAsync();
        await using var browser = await flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"
        });
        var page = await browser.NewPageAsync();

        var textBox = await page.Locator("controltype:Edit").FirstAsync();
        Assert.NotNull(textBox);
    }

    [Fact]
    public async Task Notepad_TypeText()
    {
        await using var flawright = await Flawright.CreateAsync();
        await using var browser = await flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"
        });
        var page = await browser.NewPageAsync();

        await page.FillAsync("controltype:Edit", "Hello Flawright!");
        var element = await page.Locator("controltype:Edit").FirstAsync();
        var text = await element.TextAsync();
        Assert.Equal("Hello Flawright!", text);
    }

    [Fact]
    public async Task Notepad_FindMenuBar()
    {
        await using var flawright = await Flawright.CreateAsync();
        await using var browser = await flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"
        });
        var page = await browser.NewPageAsync();

        var menuBar = await page.Locator("controltype:MenuBar").FirstAsync();
        Assert.NotNull(menuBar);
    }

    [Fact]
    public async Task Notepad_Screenshot()
    {
        await using var flawright = await Flawright.CreateAsync();
        await using var browser = await flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"
        });
        var page = await browser.NewPageAsync();

        var screenshot = await page.ScreenshotAsync();
        Assert.NotNull(screenshot);
        Assert.True(screenshot.Length > 0);
    }

    [Fact]
    public async Task Notepad_ExpectToBeVisible()
    {
        await using var flawright = await Flawright.CreateAsync();
        await using var browser = await flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"
        });
        var page = await browser.NewPageAsync();

        await page.Locator("controltype:Edit").Expect().ToBeVisibleAsync();
    }

    [Fact]
    public async Task Notepad_ExpectToBeEnabled()
    {
        await using var flawright = await Flawright.CreateAsync();
        await using var browser = await flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"
        });
        var page = await browser.NewPageAsync();

        await page.Locator("controltype:Edit").Expect().ToBeEnabledAsync();
    }

    [Fact]
    public async Task Notepad_CountElements()
    {
        await using var flawright = await Flawright.CreateAsync();
        await using var browser = await flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"
        });
        var page = await browser.NewPageAsync();

        var count = await page.Locator("controltype:Edit").CountAsync();
        Assert.Equal(1, count);
    }

    public void Dispose()
    {
    }
}