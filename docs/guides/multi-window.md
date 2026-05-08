# Multi-Window Apps

Many Windows applications open secondary windows — modal dialogs, modeless tool windows, MDI child windows, file pickers, print dialogs, and so on. Flawright's browser API provides the primitives to enumerate and wait for these windows.

## Key APIs

### `IFlawrightBrowser.NewPageAsync()`

Returns the application's *main* window — the first top-level window returned by FlaUI's `GetMainWindow`. Use this for the initial state of your test.

### `IFlawrightBrowser.GetAllPagesAsync()`

Returns all current top-level windows. Call this to enumerate open windows without waiting for a new one:

```csharp
var pages = await fw.Browser.GetAllPagesAsync();
foreach (var p in pages)
{
    var title = await p.TitleAsync();
    Console.WriteLine(title);
}
```

### `IFlawrightBrowser.WaitForPageAsync(string title, TimeSpan? timeout = null)`

Waits for a window whose title *contains* the given string (case-sensitive substring match). Returns the page when it appears, or throws `FlawrightTimeoutException` on timeout.

```csharp
// Wait up to 10 seconds for a "Save As" dialog
var dialog = await fw.Browser.WaitForPageAsync(
    "Save As",
    timeout: TimeSpan.FromSeconds(10));
```

## Patterns

### Modal dialog

Most "Save As", "Open File", and application-specific dialogs appear as separate top-level windows. Wait for them by title:

```csharp
var page = await fw.Browser.NewPageAsync();

// Trigger the dialog
await page.ClickAsync("name:File");
await page.ClickAsync("name:Save As...");

// Wait for it
var dialog = await fw.Browser.WaitForPageAsync("Save As");

// Interact with the dialog
var fileNameBox = dialog.Locator("controltype:Edit");
await fileNameBox.FillAsync(@"C:\output\test.txt");
await dialog.ClickAsync("name:Save");
```

### File picker

Windows Common File Dialog (used by most apps when calling `GetOpenFileName` / `GetSaveFileName` or WinUI's `FileOpenPicker`) appears as a separate top-level window:

```csharp
var page = await fw.Browser.NewPageAsync();
await page.ClickAsync("#OpenFileButton");

// Wait for the file picker dialog
var picker = await fw.Browser.WaitForPageAsync("Open", timeout: TimeSpan.FromSeconds(10));

// Type the file path directly into the file name field
var fileNameField = picker.Locator("controltype:Edit").Filter(
    new LocatorFilterOptions { HasText = "File name:" });

// Or use the "File name:" combo box directly
await picker.FillAsync("[name=\"File name:\"]", @"C:\test\input.csv");
await picker.ClickAsync("name:Open");
```

### Checking for a new window after action

If you need to detect that an action opened a new window (without knowing the title in advance):

```csharp
var before = await fw.Browser.GetAllPagesAsync();

await page.ClickAsync("name:New Window");

// Poll until a new page appears
IFlawrightPage? newPage = null;
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

while (!cts.Token.IsCancellationRequested)
{
    var after = await fw.Browser.GetAllPagesAsync();
    var extra = after.Except(before, ReferenceEqualityComparer.Instance).ToList();
    if (extra.Count > 0)
    {
        newPage = extra[0];
        break;
    }
    await Task.Delay(100, cts.Token);
}

Assert.NotNull(newPage);
```

### MDI applications

MDI (Multiple Document Interface) child windows appear inside the MDI frame window. In UIA, MDI children are typically `Pane` or `Window` elements inside the parent. They are not separate top-level windows, so `GetAllPagesAsync` will not find them — use scoped locators instead:

```csharp
var page = await fw.Browser.NewPageAsync();

// MDI children are Pane/Window elements inside the main window
var mdiChildren = await page.Locator("controltype:Pane").AllAsync();

// Or scope to the MDI client area by its ClassName
var mdiClient = page.Locator("class:MDIClient");
var firstChild = mdiClient.Locator("controltype:Window").First;
```

## Worked example: Notepad Save As dialog

```csharp
using JerrettDavis.Flawright;
using JerrettDavis.Flawright.Locator;
using Xunit;

public class MultiWindowTests : IAsyncLifetime
{
    private Flawright? _fw;

    public async Task InitializeAsync()
    {
        _fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"
        });
    }

    [Fact]
    public async Task SaveAsDialog_AcceptsPath()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Type something to save
        await page.FillAsync("[name=\"Text editor\"]", "content to save");

        // Open Save As (Ctrl+Shift+S in Win11 Notepad)
        await page.PressAsync("[name=\"Text editor\"]", "Ctrl+Shift+S");

        // Wait for the Save As dialog — title contains "Save As"
        var dialog = await _fw!.Browser.WaitForPageAsync(
            "Save As",
            timeout: TimeSpan.FromSeconds(10));

        Assert.NotNull(dialog);

        // The file name field — use Accessibility Insights to find the right selector
        // for your OS version's Common File Dialog
        var fileNameField = dialog.Locator("[name=\"File name:\"]");
        await fileNameField.FillAsync(@"C:\Temp\flawright-test.txt");

        // Click Save (or Cancel to avoid actually writing a file)
        await dialog.ClickAsync("name:Cancel");
    }

    [Fact]
    public async Task AllPages_ReturnsMainWindow()
    {
        var pages = await _fw!.Browser.GetAllPagesAsync();

        Assert.NotEmpty(pages);
        var title = await pages[0].TitleAsync();
        Assert.Contains("Notepad", title);
    }

    public async Task DisposeAsync()
    {
        if (_fw != null)
            await _fw.DisposeAsync();
    }
}
```

## Gotchas

**`WaitForPageAsync` uses substring match**
The title argument is a substring — `"Save As"` matches a window titled `"Save As — Notepad"` or just `"Save As"`. Do not pass the full expected title if it varies by application.

**Timing — act then wait**
Always trigger the action that opens the dialog *before* calling `WaitForPageAsync`. Do not call `WaitForPageAsync` first — it will return immediately if no matching window exists (the timeout starts counting from the call).

**Common File Dialog versions**
Windows 10 and 11 use different versions of the Common File Dialog for native and packaged apps. The exact selector for the file name edit box varies. Common patterns:
- `[name="File name:"]` — the labeled combo box / edit field
- `controltype:Edit` inside the dialog (may need `.Nth(0)` if multiple edit fields)

Use Accessibility Insights on your target OS to confirm the right selector.

**UAC-elevated dialogs**
If the application is running elevated and opens a file picker, the file picker inherits the elevated integrity level. See [Elevated apps guide](elevated-apps.md) for the integrity-level boundary issue.

**Tooltip and context menu windows**
Tooltips and context menus also appear as top-level windows. `GetAllPagesAsync` may include them. Filter by title or use `WaitForPageAsync` with a non-empty, specific title.

## Related docs

- [Auto-waiting](../auto-waiting.md) — timeout configuration for window waits
- [Elevated apps guide](elevated-apps.md) — if dialogs require elevation
- [File Explorer guide](file-explorer.md) — file picker specifics for explorer.exe
