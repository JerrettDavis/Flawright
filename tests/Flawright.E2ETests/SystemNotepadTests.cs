using Flawright;
using Flawright.CloseBehaviors;
using Flawright.InputModes;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// Opt-in E2E tests for the system Notepad application.
/// Each test skips automatically with a descriptive reason when
/// <c>notepad.exe</c> is not available as a real (non-stub) executable
/// on the current machine.
/// </summary>
/// <remarks>
/// <para>
/// These tests are intentionally opt-in: on <c>windows-latest</c> CI runners
/// (Windows Server 2025) the Store version of Notepad may not be installed,
/// causing every test to skip rather than fail.  They run automatically on
/// developer machines and Windows 11 Pro CI runners where Notepad is present.
/// </para>
/// <para>
/// For deterministic, always-on E2E coverage use
/// <see cref="TestAppTests"/> which targets the repo-shipped WPF test app.
/// </para>
/// <para>
/// Uses <see cref="VirtualInputMode"/> — all actions use UIA patterns only
/// (no focus-steal, no cursor movement), safe for CI runners.
/// </para>
/// </remarks>
public class SystemNotepadTests : IAsyncLifetime
{
    private IFlawright? _fw;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // Configure DismissDialogCloseBehavior so CloseAsync handles
        // the "save changes?" dialog that Notepad shows on exit.
        // VirtualInputMode: drives Notepad via UIA patterns — no focus-steal,
        // no cursor movement, safe for CI runners.
        _fw = await global::Flawright.Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = "notepad.exe" },
            new FlawrightOptions
            {
                InputMode = new VirtualInputMode(),
                CloseBehavior = new DismissDialogCloseBehavior() // handles Win10 + Win11 Notepad
            });
    }

    /// <inheritdoc/>
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

    /// <summary>Verifies that Notepad launches and a window can be retrieved.</summary>
    [RequiresAppFact(ExePath = "notepad.exe")]
    public async Task Notepad_LaunchAndFindWindow()
    {
        var page = await _fw!.Browser.NewPageAsync();
        Assert.NotNull(page);
    }

    /// <summary>Finds the Notepad text-editor control by its UIA class name.</summary>
    /// <remarks>
    /// Win11 UWP Notepad (v11.x, WinUI 3) uses <c>RichEditD2DPT</c> as the
    /// UIA ClassName for its editor pane; the old Win32 <c>Edit</c> class no
    /// longer exists in this version of the application.
    /// </remarks>
    [RequiresAppFact(ExePath = "notepad.exe")]
    public async Task Notepad_FindTextBox_ByControlType()
    {
        var page = await _fw!.Browser.NewPageAsync();

#pragma warning disable CS0618
        var textBox = await page.Locator("class:RichEditD2DPT").First.ElementHandleAsync();
#pragma warning restore CS0618
        Assert.NotNull(textBox);
    }

    /// <summary>Types text into the Notepad editor and reads it back.</summary>
    /// <remarks>
    /// Win11 UWP Notepad (v11.x, WinUI 3) uses <c>RichEditD2DPT</c> as the
    /// UIA ClassName for its editor pane; the old Win32 <c>Edit</c> class no
    /// longer exists in this version of the application.
    /// </remarks>
    [RequiresAppFact(ExePath = "notepad.exe")]
    public async Task Notepad_TypeText()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.FillAsync("class:RichEditD2DPT", "Hello Flawright!");
        var text = await page.Locator("class:RichEditD2DPT").First.InputValueAsync();
        Assert.Equal("Hello Flawright!", text);
    }

    /// <summary>Finds the menu bar in the Notepad window.</summary>
    [RequiresAppFact(ExePath = "notepad.exe")]
    public async Task Notepad_FindMenuBar()
    {
        var page = await _fw!.Browser.NewPageAsync();

#pragma warning disable CS0618
        var menuBar = await page.Locator("controltype:MenuBar").First.ElementHandleAsync();
#pragma warning restore CS0618
        Assert.NotNull(menuBar);
    }

    /// <summary>Takes a screenshot of the Notepad window.</summary>
    [RequiresAppFact(ExePath = "notepad.exe")]
    public async Task Notepad_Screenshot()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var screenshot = await page.ScreenshotAsync();
        Assert.NotNull(screenshot);
        Assert.True(screenshot.Length > 0);
    }

    /// <summary>Asserts that the Notepad editor is present and attached in the UIA tree.</summary>
    /// <remarks>
    /// Win11 UWP Notepad (v11.x, WinUI 3) uses <c>RichEditD2DPT</c> as the
    /// UIA ClassName for its editor pane; the old Win32 <c>Edit</c> class no
    /// longer exists in this version of the application.
    /// <para>
    /// Uses <c>ToBeAttachedAsync</c> rather than <c>ToBeVisibleAsync</c> because the
    /// WinUI 3 DirectComposition surface backing <c>RichEditD2DPT</c> can report
    /// <c>IsOffscreen = true</c> through FlaUI on multi-monitor systems even when
    /// the element is fully rendered and interactive.  <c>IsAttached</c> (element
    /// exists in the UIA tree) is the reliable signal for WinUI 3 elements.
    /// </para>
    /// </remarks>
    [RequiresAppFact(ExePath = "notepad.exe")]
    public async Task Notepad_ExpectToBeVisible()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("class:RichEditD2DPT").Expect().ToBeAttachedAsync();
    }

    /// <summary>Asserts that the Notepad editor is enabled.</summary>
    /// <remarks>
    /// Win11 UWP Notepad (v11.x, WinUI 3) uses <c>RichEditD2DPT</c> as the
    /// UIA ClassName for its editor pane; the old Win32 <c>Edit</c> class no
    /// longer exists in this version of the application.
    /// </remarks>
    [RequiresAppFact(ExePath = "notepad.exe")]
    public async Task Notepad_ExpectToBeEnabled()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("class:RichEditD2DPT").Expect().ToBeEnabledAsync();
    }

    /// <summary>Counts the number of text-editor controls in the Notepad window.</summary>
    /// <remarks>
    /// Win11 UWP Notepad (v11.x, WinUI 3) uses <c>RichEditD2DPT</c> as the
    /// UIA ClassName for its editor pane. Only the active tab's editor is
    /// exposed in the UIA tree, so the count is always 1 regardless of how
    /// many tabs are open.  The old Win32 <c>Edit</c> class no longer exists
    /// in this version of the application.
    /// </remarks>
    [RequiresAppFact(ExePath = "notepad.exe")]
    public async Task Notepad_CountElements()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var count = await page.Locator("class:RichEditD2DPT").CountAsync();
        Assert.Equal(1, count);
    }

    /// <summary>Verifies that the Notepad window title is non-empty.</summary>
    [RequiresAppFact(ExePath = "notepad.exe")]
    public async Task Notepad_GetTitle_ReturnsNonEmpty()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var title = await page.TitleAsync();
        Assert.False(string.IsNullOrEmpty(title));
    }
}
