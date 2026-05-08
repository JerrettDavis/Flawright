// ReSharper disable All
// This file is a compile-time regression guard.
// The methods in ReadmeQuickstartCompileCheck contain verbatim code from
// README.md and /docs/*.md.  They are NEVER invoked at runtime.
// If any method body fails to compile, the README API is broken — fix it.

using JerrettDavis.Flawright.Locator;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests;

#pragma warning disable CS1998  // async method lacks await — these are compile-time stubs
#pragma warning disable CS0219  // variable assigned but never used
#pragma warning disable CA1822  // member does not access instance data

/// <summary>
/// Compile-time snapshots of every code sample in README.md and /docs/*.md.
/// None of these methods are called; they exist so CI catches API drift.
/// </summary>
internal static class ReadmeQuickstartCompileCheck
{
    // ── README: Quickstart ────────────────────────────────────────────────────

    /// <summary>README quickstart — the exact snippet that triggered the bug report (updated for Win11).</summary>
    public static async Task ReadmeQuickstart()
    {
        await using var fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"   // auto-resolves to AUMID on Windows 11
        });

        var page = await fw.Browser.NewPageAsync();

        // Fill the editor — Win11 Notepad (WinUI3) uses AutomationId "RichEditBox"
        await page.FillAsync("#RichEditBox", "Hello from Flawright!");

        // Assert the text is present
        await page.Locator("#RichEditBox").Expect().ToBeVisibleAsync();

        // Take a screenshot — string-path overload (the bug that was reported)
        byte[] png = await page.ScreenshotAsync(@"C:\temp\notepad.png");
    }

    // ── README: Attach example ────────────────────────────────────────────────

    public static async Task ReadmeAttach()
    {
        await using var fw = await Flawright.AttachAsync(new AttachOptions
        {
            ProcessId = 12345
        });
    }

    // ── README: LaunchAsync with FlawrightOptions ─────────────────────────────

    public static async Task ReadmeLaunchWithOptions()
    {
        await using var fw = await Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = "notepad.exe" },
            new FlawrightOptions
            {
                DefaultTimeout = TimeSpan.FromSeconds(10),
                DefaultRetryInterval = TimeSpan.FromMilliseconds(50),
                ScreenshotDirectory = @"C:\TestOutput"
            });
    }

    // ── README: Browser API ───────────────────────────────────────────────────

    public static async Task ReadmeBrowser()
    {
        await using var fw = await Flawright.LaunchAsync(new LaunchOptions { ApplicationPath = "notepad.exe" });

        var page = await fw.Browser.NewPageAsync();
        var pages = await fw.Browser.GetAllPagesAsync();
        var dialog = await fw.Browser.WaitForPageAsync("Save As", timeout: TimeSpan.FromSeconds(10));
    }

    // ── README: Page API ──────────────────────────────────────────────────────

    public static async Task ReadmePage()
    {
        await using var fw = await Flawright.LaunchAsync(new LaunchOptions { ApplicationPath = "notepad.exe" });
        var page = await fw.Browser.NewPageAsync();

        await page.ClickAsync("name:OK");
        await page.FillAsync("controltype:Edit", "some text");
        await page.TypeAsync("controltype:Edit", "hello");
        await page.PressAsync("controltype:Edit", "Ctrl+S");
        await page.CheckAsync("controltype:CheckBox");
        await page.UncheckAsync("controltype:CheckBox");
        await page.SelectOptionAsync("controltype:ComboBox", "Option A");

        var el = await page.WaitForSelectorAsync("name:Loading Complete");
        var locator = page.Locator("#username");
        var title = await page.TitleAsync();
    }

    // ── README: Locator API ───────────────────────────────────────────────────

    public static async Task ReadmeLocator()
    {
        await using var fw = await Flawright.LaunchAsync(new LaunchOptions { ApplicationPath = "notepad.exe" });
        var page = await fw.Browser.NewPageAsync();

        var saveButton = page.Locator("name:Save");

        await saveButton.ClickAsync();

        // Sync narrowing (not async — this was the v0.1 breakage point)
        var firstLocator = saveButton.First;
        var count = await page.Locator("controltype:Button").CountAsync();
        var second = page.Locator("controltype:ListItem").Nth(1);
        var all = await page.Locator("controltype:ListItem").AllAsync();

        var filtered = page.Locator("controltype:ListItem")
            .Filter(new LocatorFilterOptions { HasText = "Save" });

        await saveButton.Expect().ToBeEnabledAsync();
    }

    // ── README: Screenshots ───────────────────────────────────────────────────

    public static async Task ReadmeScreenshots()
    {
        await using var fw = await Flawright.LaunchAsync(new LaunchOptions { ApplicationPath = "notepad.exe" });
        var page = await fw.Browser.NewPageAsync();

        // Options-based overload
        byte[] png1 = await page.ScreenshotAsync();

        // String-path convenience overload (the bug fix)
        byte[] png2 = await page.ScreenshotAsync(@"C:\temp\screenshot.png");
    }

    // ── docs/getting-started.md: Notepad test ────────────────────────────────

    public static async Task DocsGettingStartedNotepadTest()
    {
        await using var fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"   // auto-resolves to AUMID on Windows 11
        });

        var page = await fw.Browser.NewPageAsync();

        // Win11 Notepad (WinUI3): "#RichEditBox"; classic Win10: "controltype:Edit"
        await page.FillAsync("#RichEditBox", "Hello from Flawright!");

        // v0.2 API: read text via InnerTextAsync on the locator (not FirstAsync)
        var text = await page.Locator("#RichEditBox").InnerTextAsync();

        await page.Locator("controltype:MenuBar").Expect().ToBeVisibleAsync();

        var png = await page.ScreenshotAsync();
    }

    // ── docs/examples.md: Calculator ─────────────────────────────────────────

    public static async Task DocsExamplesCalculator()
    {
        await using var fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "calc.exe"
        });
        var page = await fw.Browser.NewPageAsync();

        await page.ClickAsync("#1");
        await page.ClickAsync("#plus");
        await page.ClickAsync("#2");
        await page.ClickAsync("#equals");

        await page.Locator("#CalculatorResults").Expect().ToHaveTextAsync("Display is 3");

        var buttonCount = await page.Locator("controltype:Button").CountAsync();
        await page.Locator("controltype:Button").Expect().ToBeVisibleAsync();
    }

    // ── docs/examples.md: Notepad type and screenshot ─────────────────────────

    public static async Task DocsExamplesNotepadScreenshot()
    {
        await using var fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"   // auto-resolves to AUMID on Windows 11
        });
        var page = await fw.Browser.NewPageAsync();

        const string Content = "Line 1\nLine 2\nLine 3";

        // Win11 Notepad (WinUI3): "#RichEditBox"; classic Win10: "controltype:Edit"
        await page.FillAsync("#RichEditBox", Content);

        // v0.2 API: read text via InnerTextAsync
        var text = await page.Locator("#RichEditBox").InnerTextAsync();

        // String-path convenience overload
        byte[] png = await page.ScreenshotAsync(@"C:\temp\notepad-test.png");
    }

    // ── docs/examples.md: Attach to running process ──────────────────────────

    public static async Task DocsExamplesAttach()
    {
        var pid = System.Diagnostics.Process
            .GetProcessesByName("notepad")
            .FirstOrDefault()?.Id
            ?? throw new InvalidOperationException("Notepad is not running");

        await using var fw = await Flawright.AttachAsync(new AttachOptions
        {
            ProcessId = pid
        });
        var page = await fw.Browser.NewPageAsync();

        // Win11 Notepad (WinUI3): "#RichEditBox"; classic Win10: "controltype:Edit"
        await page.Locator("#RichEditBox").Expect().ToBeVisibleAsync();
    }

    // ── docs/examples.md: Multi-window ───────────────────────────────────────

    public static async Task DocsExamplesMultiWindow()
    {
        await using var fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"   // auto-resolves to AUMID on Windows 11
        });
        var page = await fw.Browser.NewPageAsync();

        // Win11 Notepad (WinUI3): "#RichEditBox"; classic Win10: "controltype:Edit"
        await page.PressAsync("#RichEditBox", "Ctrl+Shift+S");

        var dialog = await fw.Browser.WaitForPageAsync("Save As", timeout: TimeSpan.FromSeconds(10));
    }

    // ── docs/examples.md: Custom FlawrightOptions ────────────────────────────

    public static async Task DocsExamplesCustomOptions()
    {
        await using var fw = await Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = "myapp.exe" },
            new FlawrightOptions
            {
                DefaultTimeout = TimeSpan.FromSeconds(10),
                DefaultRetryInterval = TimeSpan.FromMilliseconds(50),
                ScreenshotDirectory = @"C:\TestOutput\Screenshots"
            });

        var page = await fw.Browser.NewPageAsync();

        byte[] png = await page.ScreenshotAsync();
    }

    // ── docs/selectors.md: Filtering ─────────────────────────────────────────

    public static async Task DocsSelectorsFiltering()
    {
        await using var fw = await Flawright.LaunchAsync(new LaunchOptions { ApplicationPath = "notepad.exe" });
        var page = await fw.Browser.NewPageAsync();

        var saveItems = page.Locator("controltype:ListItem")
            .Filter(new LocatorFilterOptions { HasText = "Save" });

        var first = saveItems.First;
    }

    // ── docs/selectors.md: Nth element ───────────────────────────────────────

    public static async Task DocsSelectorsNth()
    {
        await using var fw = await Flawright.LaunchAsync(new LaunchOptions { ApplicationPath = "notepad.exe" });
        var page = await fw.Browser.NewPageAsync();

        // Sync Nth (v0.2) — was async NthAsync in v0.1
        var item = page.Locator("controltype:ListItem").Nth(1);

        var n = await page.Locator("controltype:Button").CountAsync();
        var all = await page.Locator("controltype:ListItem").AllAsync();
    }
}

/// <summary>
/// Xunit test class that proves the compile-time snapshots above compile.
/// If ReadmeQuickstartCompileCheck fails to compile, this class will not
/// compile either, and the test will fail to build — CI catches the drift.
/// </summary>
public sealed class ReadmeQuickstartTests
{
    [Fact]
    public void ReadmeAndDocs_CompilesAgainstV0_2_Api()
    {
        // The mere existence of ReadmeQuickstartCompileCheck as a type that
        // compiled is the test.  Every method body in that class is a verbatim
        // snippet from the README or /docs.  If the API changes and the docs
        // are not updated, this file will fail to compile and CI will catch it.
        Assert.True(true, "All README / docs code samples compile against the current API.");
    }
}
#pragma warning restore CS1998
#pragma warning restore CS0219
#pragma warning restore CA1822
