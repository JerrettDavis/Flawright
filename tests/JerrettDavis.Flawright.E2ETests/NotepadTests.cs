using JerrettDavis.Flawright;
using JerrettDavis.Flawright.CloseBehaviors;
using JerrettDavis.Flawright.InputModes;
using Xunit;

namespace JerrettDavis.Flawright.E2ETests;

/// <summary>
/// E2E tests for Notepad.  Uses <see cref="IAsyncLifetime"/> so that
/// <c>DisposeAsync</c> is guaranteed to run even when a test throws.
/// </summary>
/// <remarks>
/// Uses <see cref="VirtualInputMode"/> — all actions in this fixture use
/// <c>FillAsync</c>, <c>InputValueAsync</c>, <c>CountAsync</c>,
/// <c>TitleAsync</c>, and <c>ScreenshotAsync</c>, which are all UIA-pattern
/// compatible.  No hover, drag, double-click, or key chords are used.
/// </remarks>
public class NotepadTests : IAsyncLifetime
{
    private IFlawright? _fw;

    public async Task InitializeAsync()
    {
        // Configure DismissDialogCloseBehavior so CloseAsync handles
        // the "save changes?" dialog that Notepad shows on exit.
        // VirtualInputMode: drives Notepad via UIA patterns — no focus-steal,
        // no cursor movement, safe for CI runners.
        _fw = await Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = "notepad.exe" },
            new FlawrightOptions
            {
                InputMode = new VirtualInputMode(),
                CloseBehavior = new DismissDialogCloseBehavior() // handles Win10 + Win11 Notepad
            });
    }

    public async Task DisposeAsync()
    {
        if (_fw != null)
        {
            // Runs the configured DismissDialogCloseBehavior — dismisses the
            // "save changes?" dialog if it appears, then waits for exit.
            await _fw.Browser.CloseAsync();
            await _fw.DisposeAsync();
        }
    }

    [Fact]
    public async Task Notepad_LaunchAndFindWindow()
    {
        var page = await _fw!.Browser.NewPageAsync();
        Assert.NotNull(page);
    }

    [Fact]
    public async Task Notepad_FindTextBox_ByControlType()
    {
        var page = await _fw!.Browser.NewPageAsync();

#pragma warning disable CS0618
        var textBox = await page.Locator("class:Edit").First.ElementHandleAsync();
#pragma warning restore CS0618
        Assert.NotNull(textBox);
    }

    [Fact]
    public async Task Notepad_TypeText()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.FillAsync("class:Edit", "Hello Flawright!");
        var text = await page.Locator("class:Edit").First.InputValueAsync();
        Assert.Equal("Hello Flawright!", text);
    }

    [Fact]
    public async Task Notepad_FindMenuBar()
    {
        var page = await _fw!.Browser.NewPageAsync();

#pragma warning disable CS0618
        var menuBar = await page.Locator("controltype:MenuBar").First.ElementHandleAsync();
#pragma warning restore CS0618
        Assert.NotNull(menuBar);
    }

    [Fact]
    public async Task Notepad_Screenshot()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var screenshot = await page.ScreenshotAsync();
        Assert.NotNull(screenshot);
        Assert.True(screenshot.Length > 0);
    }

    [Fact]
    public async Task Notepad_ExpectToBeVisible()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("class:Edit").Expect().ToBeVisibleAsync();
    }

    [Fact]
    public async Task Notepad_ExpectToBeEnabled()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("class:Edit").Expect().ToBeEnabledAsync();
    }

    [Fact]
    public async Task Notepad_CountElements()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var count = await page.Locator("class:Edit").CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Notepad_GetTitle_ReturnsNonEmpty()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var title = await page.TitleAsync();
        Assert.False(string.IsNullOrEmpty(title));
    }
}
