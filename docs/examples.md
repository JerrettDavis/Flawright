# Examples

Each example below is a self-contained test method. Add `using JerrettDavis.Flawright;` and a reference to `Flawright` to compile them.

## Calculator: arithmetic sequence

Clicks digits and operators in sequence, then asserts the display shows the correct result. Windows Calculator uses AutomationIds for its buttons.

```csharp
[Fact]
public async Task Calculator_AddsTwoNumbers()
{
    await using var fw = await Flawright.LaunchAsync(new LaunchOptions
    {
        ApplicationPath = "calc.exe"
    });
    var page = await fw.Browser.NewPageAsync();

    // Windows Calculator AutomationIds: digits are their face values ("1", "2", ...),
    // operators are "plus", "minus", "multiply", "divide", "equals"
    await page.ClickAsync("#1");
    await page.ClickAsync("#plus");
    await page.ClickAsync("#2");
    await page.ClickAsync("#equals");

    // The result display has AutomationId "CalculatorResults"
    await page.Locator("#CalculatorResults").Expect().ToHaveTextAsync("Display is 3");
}

[Fact]
public async Task Calculator_HasButtons()
{
    await using var fw = await Flawright.LaunchAsync(new LaunchOptions
    {
        ApplicationPath = "calc.exe"
    });
    var page = await fw.Browser.NewPageAsync();

    var buttonCount = await page.Locator("controltype:Button").CountAsync();
    Assert.True(buttonCount > 0, "Calculator should have buttons");

    // Spot-check a specific button
    await page.Locator("controltype:Button").Expect().ToBeVisibleAsync();
}
```

**Note:** Windows Calculator's AutomationIds vary by Windows version and Calculator version. Use Accessibility Insights to verify the exact IDs on your machine before running these tests.

## Notepad: type, screenshot, verify

Launches Notepad, types a multi-line document, takes a screenshot, and verifies the editor is populated.

> **Windows 10 vs Windows 11 selectors**
>
> Windows 11 ships a packaged WinUI3 Notepad. Its text editor has `AutomationId = "RichEditBox"`.
> Classic Windows 10 Notepad uses `ControlType.Edit`. Use the appropriate selector for your OS, or
> inspect with [Accessibility Insights](https://accessibilityinsights.io/) to confirm.

```csharp
[Fact]
public async Task Notepad_TypeAndVerify()
{
    await using var fw = await Flawright.LaunchAsync(new LaunchOptions
    {
        ApplicationPath = "notepad.exe"   // auto-resolves to AUMID on Windows 11
    });
    var page = await fw.Browser.NewPageAsync();

    const string content = "Line 1\nLine 2\nLine 3";

    // Win11 Notepad (WinUI3): use "#RichEditBox"
    // Classic Win10 Notepad:  use "controltype:Edit"
    await page.FillAsync("#RichEditBox", content);

    // Verify the text landed
    var text = await page.Locator("#RichEditBox").InnerTextAsync();
    Assert.Equal(content, text);

    // Capture a screenshot for the test report
    byte[] png = await page.ScreenshotAsync(@"C:\temp\notepad-test.png");
    Assert.True(png.Length > 0);
}

[Fact]
public async Task Notepad_MenuBarIsPresent()
{
    await using var fw = await Flawright.LaunchAsync(new LaunchOptions
    {
        ApplicationPath = "notepad.exe"   // auto-resolves to AUMID on Windows 11
    });
    var page = await fw.Browser.NewPageAsync();

    await page.Locator("controltype:MenuBar").Expect().ToBeVisibleAsync();

    // Count menu items: File, Edit, View, ... (varies by Windows version)
    var menuItemCount = await page.Locator("controltype:MenuItem").CountAsync();
    Assert.True(menuItemCount >= 3);
}
```

## File Explorer: navigate and count items

Launches File Explorer pointing at a specific path and counts visible items. Uses the `Arguments` field on `LaunchOptions`.

```csharp
[Fact]
public async Task FileExplorer_CountsItemsInWindowsFolder()
{
    await using var fw = await Flawright.LaunchAsync(new LaunchOptions
    {
        ApplicationPath = "explorer.exe",
        Arguments = new[] { @"C:\Windows\System32" }
    });
    var page = await fw.Browser.NewPageAsync();

    // File Explorer populates asynchronously; in production tests you'd
    // poll until count stabilizes. Here we just verify at least one item appears.
    var items = await page.Locator("controltype:ListItem").CountAsync();
    Assert.True(items > 0, "Expected items in System32 folder");
}
```

## Attaching to an already-running process

If the application is already running and you don't want Flawright to launch it, use `AttachAsync`.

```csharp
[Fact]
public async Task AttachToRunningNotepad()
{
    // Find the PID of an already-running Notepad
    var pid = System.Diagnostics.Process
        .GetProcessesByName("notepad")
        .FirstOrDefault()?.Id
        ?? throw new InvalidOperationException("Notepad is not running");

    await using var fw = await Flawright.AttachAsync(new AttachOptions
    {
        ProcessId = pid
    });
    var page = await fw.Browser.NewPageAsync();

    // Win11 Notepad: "#RichEditBox"; classic Win10: "controltype:Edit"
    await page.Locator("#RichEditBox").Expect().ToBeVisibleAsync();
}
```

## Multi-window: wait for a dialog

After clicking a menu item that opens a dialog, wait for the dialog window by title substring.

```csharp
[Fact]
public async Task Notepad_SaveAsDialog_Opens()
{
    await using var fw = await Flawright.LaunchAsync(new LaunchOptions
    {
        ApplicationPath = "notepad.exe"   // auto-resolves to AUMID on Windows 11
    });
    var page = await fw.Browser.NewPageAsync();

    // Open the Save As dialog via keyboard shortcut
    // Win11 Notepad: "#RichEditBox"; classic Win10: "controltype:Edit"
    await page.PressAsync("#RichEditBox", "Ctrl+Shift+S");

    // Wait for the Save As dialog to appear
    var dialog = await fw.Browser.WaitForPageAsync("Save As", timeout: TimeSpan.FromSeconds(10));
    Assert.NotNull(dialog);
}
```

## Keyboard input

Use `TypeAsync` for realistic key-by-key input (useful for controls with key handlers) and `PressAsync` for named keys and chords.

```csharp
[Fact]
public async Task Notepad_KeyboardInput()
{
    await using var fw = await Flawright.LaunchAsync(new LaunchOptions
    {
        ApplicationPath = "notepad.exe"   // auto-resolves to AUMID on Windows 11
    });
    var page = await fw.Browser.NewPageAsync();

    // Win11 Notepad (WinUI3): "#RichEditBox"; classic Win10: "controltype:Edit"
    // Type character-by-character (key events fire for each character)
    await page.TypeAsync("#RichEditBox", "Hello");

    // Press Enter
    await page.PressAsync("#RichEditBox", "Enter");

    // Type more
    await page.TypeAsync("#RichEditBox", "World");

    // Select all and copy with a chord
    await page.PressAsync("#RichEditBox", "Ctrl+A");
}
```

## Custom FlawrightOptions

Configure timeouts and screenshot output at the session level.

```csharp
await using var fw = await Flawright.LaunchAsync(
    new LaunchOptions { ApplicationPath = "myapp.exe" },
    new FlawrightOptions
    {
        DefaultTimeout       = TimeSpan.FromSeconds(10),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(50),
        ScreenshotDirectory  = @"C:\TestOutput\Screenshots"
    });

var page = await fw.Browser.NewPageAsync();

// ScreenshotAsync with no path uses ScreenshotDirectory + auto-generated filename
byte[] png = await page.ScreenshotAsync();
```

## Installer wizard: step through pages

A pattern for multi-step wizards. Each `Next` click advances the wizard; assertions verify you reached the expected step before proceeding.

```csharp
[Fact]
public async Task InstallerWizard_StepsThroughToCompletion()
{
    await using var fw = await Flawright.LaunchAsync(new LaunchOptions
    {
        ApplicationPath = @"C:\Installers\MyApp.exe"
    });
    var page = await fw.Browser.NewPageAsync();

    // Step 1: Welcome page
    await page.Locator("name:Welcome to the MyApp Setup Wizard").Expect().ToBeVisibleAsync();
    await page.ClickAsync("name:Next");

    // Step 2: License agreement
    await page.Locator("name:License Agreement").Expect().ToBeVisibleAsync();
    await page.ClickAsync("name:I Agree");
    await page.ClickAsync("name:Next");

    // Step 3: Installation folder (accept default)
    await page.Locator("name:Choose Install Location").Expect().ToBeVisibleAsync();
    await page.ClickAsync("name:Install");

    // Step 4: Completion
    await page.Locator("name:Completing the MyApp Setup Wizard").Expect().ToBeVisibleAsync();
    await page.ClickAsync("name:Finish");
}
```

**Note:** Installer wizard control names are highly application-specific. Run Accessibility Insights against your installer to find exact `Name` values for each control.
