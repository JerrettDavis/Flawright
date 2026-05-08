# Win11 Notepad

Windows 11 ships a reimplemented Notepad built on WinUI3 and distributed as an MSIX packaged app. It looks like the classic Notepad but exposes a different UIA tree — notably, it added tab support and changed how the editor control is identified.

## Launching

```csharp
// Simplest form — Flawright auto-resolves the alias on Windows 11
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = "notepad.exe"
});
```

On Windows 11, Flawright detects that `notepad.exe` resolves to an AppExecutionAlias for the packaged app and automatically calls `Application.LaunchStoreApp("Microsoft.WindowsNotepad_8wekyb3d8bbwe!App")`. You never need to specify the AUMID manually.

To launch by AUMID directly (e.g., if auto-resolution is disabled or you want to be explicit):

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    Aumid = "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App"
});
```

## Selector cheat sheet

| Element | Selector | Notes |
|---|---|---|
| Editor (newer tabbed builds) | `[name="Text editor"]` | Name-based; confirmed Win11 22H2+ tabbed Notepad |
| Editor (older Win11 non-tabbed) | `#RichEditBox` | AutomationId; still works on some Win11 builds |
| Menu bar | `controltype:MenuBar` | Always present |
| File menu item | `name:File` | Top-level menu |
| Edit menu item | `name:Edit` | Top-level menu |
| View menu item | `name:View` | Top-level menu |
| Tab bar | `controltype:Tab` | Only present in tabbed Notepad builds |
| Individual tab | `controltype:TabItem` | Each open file is a TabItem |
| Title bar | `controltype:TitleBar` | Window chrome |

> **Gotcha — tabbed Notepad dropped `RichEditBox`**
>
> The tabbed version of Win11 Notepad (shipped in Windows 11 22H2 and later updates) no longer exposes `AutomationId = "RichEditBox"` on the editor. Use `[name="Text editor"]` instead. On earlier Win11 builds without tabs, `#RichEditBox` still works. If you need to support both, try `#RichEditBox` first and fall back to `[name="Text editor"]` — or inspect the live UIA tree with [Accessibility Insights](https://accessibilityinsights.io/) to confirm which your build uses.

## Worked example

```csharp
using Flawright;
using Xunit;

public class NotepadTests : IAsyncLifetime
{
    private Flawright? _fw;

    public async Task InitializeAsync()
    {
        // Configure DismissDialogCloseBehavior so CloseAsync handles the
        // "save changes?" dialog that Notepad shows when exiting with unsaved content.
        _fw = await Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = "notepad.exe" },
            new FlawrightOptions
            {
                CloseBehavior = new DismissDialogCloseBehavior() // handles Win10 + Win11 Notepad
            });
    }

    [Fact]
    public async Task TypeAndVerifyText()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Use the name selector — works on tabbed and non-tabbed Win11 builds
        await page.FillAsync("[name=\"Text editor\"]", "Hello from Flawright!");

        var text = await page.Locator("[name=\"Text editor\"]").InnerTextAsync();
        Assert.Equal("Hello from Flawright!", text);
    }

    [Fact]
    public async Task OpenFileMenu()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.ClickAsync("name:File");

        // After clicking, the File menu expands; wait for a known item
        await page.Locator("name:New").Expect().ToBeVisibleAsync();
    }

    [Fact]
    public async Task SaveAsDialog_Opens()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Type something so there is content to save
        await page.FillAsync("[name=\"Text editor\"]", "test content");

        // Open Save As via keyboard shortcut
        await page.PressAsync("[name=\"Text editor\"]", "Ctrl+Shift+S");

        // Wait for the Save As dialog
        var dialog = await _fw!.Browser.WaitForPageAsync(
            "Save As",
            timeout: TimeSpan.FromSeconds(10));

        Assert.NotNull(dialog);
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
}
```

## Gotchas

**Editor selector varies by Notepad version**
The tabbed Notepad (Win11 22H2+) uses `[name="Text editor"]`; older Win11 builds without tabs expose `#RichEditBox`. Classic Win10 Notepad uses `controltype:Edit`. Always inspect with [Accessibility Insights](https://accessibilityinsights.io/) or `inspect.exe` on your target machine.

**Packaged app — process is not `notepad.exe`**
The actual running process is under `C:\Program Files\WindowsApps\Microsoft.WindowsNotepad_*\`. The alias `notepad.exe` is a stub that launches the packaged app. Flawright handles this transparently from v0.2.9+; earlier versions required specifying `Aumid` directly.

**Multiple tabs**
Each open file is a separate `TabItem` in the tab bar. To get the content of a specific tab, click its `TabItem` first, then interact with the editor. There is no per-tab editor locator — the single editor panel updates when the active tab changes.

**Auto-save**
Win11 Notepad has an auto-save / auto-restore feature. If a previous test left content, Notepad may restore it on next launch. Open a new tab (`Ctrl+N`) or clear the editor at the start of each test that needs a clean state.

## Related docs

- [Selectors](../selectors.md) — full selector grammar
- [WinUI 3 guide](winui3.md) — general WinUI3 patterns
- [Multi-window apps guide](multi-window.md) — Save As dialog handling
