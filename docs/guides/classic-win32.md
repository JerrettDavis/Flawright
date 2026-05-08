# Classic Win32

Classic Win32 applications include any app built directly against the Windows API — legacy Notepad (Win10 and earlier), Paint, Wordpad, older system tools, and third-party applications built with MFC, ATL, or raw Win32. UIA support in these apps is generally reasonable but less uniform than WPF or WinUI3.

## Launching

Win32 apps launch via the executable path:

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = @"C:\Windows\System32\notepad.exe",
    // Or just "notepad.exe" on Win10 (no alias redirection on Win10)
});
```

With command-line arguments:

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = "explorer.exe",
    Arguments = new[] { @"C:\Windows\System32" }
});
```

With a working directory:

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = @"C:\MyApp\myapp.exe",
    WorkingDirectory = @"C:\MyApp"
});
```

## Selector patterns

Win32 controls often lack AutomationId entirely. The most reliable selector approach for Win32 is `class:` (ClassName) combined with index selection via `.Nth()`.

### ClassName quick-reference for common Win32 controls

| Control type | Windows ClassName | Flawright selector |
|---|---|---|
| Single-line text box | `Edit` | `class:Edit` |
| Multi-line text box | `Edit` | `class:Edit` |
| Rich text (RichEdit20W, etc.) | `RichEdit20W` | `class:RichEdit20W` |
| Button (push, radio, checkbox) | `Button` | `class:Button` |
| Static text / label | `Static` | `class:Static` |
| List box | `ListBox` | `class:ListBox` |
| Combo box | `ComboBox` | `class:ComboBox` |
| List view | `SysListView32` | `class:SysListView32` |
| Tree view | `SysTreeView32` | `class:SysTreeView32` |
| Tab control | `SysTabControl32` | `class:SysTabControl32` |
| Toolbar | `ToolbarWindow32` | `class:ToolbarWindow32` |
| Status bar | `msctls_statusbar32` | `class:msctls_statusbar32` |
| Progress bar | `msctls_progress32` | `class:msctls_progress32` |
| Trackbar / slider | `msctls_trackbar32` | `class:msctls_trackbar32` |
| Date/time picker | `SysDateTimePick32` | `class:SysDateTimePick32` |
| Scroll bar | `ScrollBar` | `class:ScrollBar` |

> **ClassName is ControlType, not class**
>
> In UIA, `ClassName` is a property on the automation element, not the same as WPF's `class:` style name. It maps to the Win32 window class name registered with `RegisterClass`. The `class:` and `classname:` prefixes in Flawright both match on this UIA `ClassName` property.

### Combining ClassName with ControlType

Use the `>>` combinator to scope a search:

```csharp
// First Edit box inside the main dialog group
var firstInput = page.Locator("controltype:Group").Locator("class:Edit").First;

// Button with a specific name
var okButton = page.Locator("class:Button").Filter(new LocatorFilterOptions { HasText = "OK" });
```

### Fallback: ControlType selectors

When a Win32 app sets accessible names correctly (many do), use `name:`:

```csharp
await page.ClickAsync("name:Open");
await page.ClickAsync("name:Cancel");
```

## Worked example: classic Win10 Notepad

```csharp
using JerrettDavis.Flawright;
using JerrettDavis.Flawright.Locator;
using Xunit;

public class ClassicNotepadTests : IAsyncLifetime
{
    private Flawright? _fw;

    public async Task InitializeAsync()
    {
        // Win10 notepad.exe — classic Win32, no alias redirect
        _fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = @"C:\Windows\System32\notepad.exe"
        });
    }

    [Fact]
    public async Task TypeText_AppearsInEditor()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Classic Notepad: the editor is a raw "Edit" class control
        await page.FillAsync("controltype:Edit", "Hello from classic Notepad!");

        var text = await page.Locator("controltype:Edit").InnerTextAsync();
        Assert.Equal("Hello from classic Notepad!", text);
    }

    [Fact]
    public async Task MenuBar_HasFileMenu()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("controltype:MenuBar").Expect().ToBeVisibleAsync();

        var fileMenu = page.Locator("controltype:MenuItem")
            .Filter(new LocatorFilterOptions { HasText = "File" });

        await fileMenu.Expect().ToBeVisibleAsync();
    }

    public async Task DisposeAsync()
    {
        if (_fw != null)
            await _fw.DisposeAsync();
    }
}
```

## Gotchas

**Many Win32 apps have no AutomationId**
Win32 apps built without accessibility in mind often expose no AutomationId. Fall back to `class:` + `.Nth()`, or `name:` if the control has an accessible name. Use Accessibility Insights or `inspect.exe` to discover what UIA properties are available.

**inspect.exe vs Accessibility Insights**
For Win32 apps, `inspect.exe` (from the Windows SDK) often shows more raw Win32 detail — including the window ClassName directly. Open it at `C:\Program Files (x86)\Windows Kits\10\bin\<version>\x64\inspect.exe`. Accessibility Insights is higher-level and more readable but may elide some properties.

**Spy++ for window enumeration**
If neither tool shows useful properties, Spy++ (ships with Visual Studio) can enumerate all top-level windows and their Win32 ClassName values. This is useful for finding the right window when an app has unusual structure.

**MFC and dialog-based apps**
MFC dialog-based apps often expose controls only by their dialog resource ID (a numeric string like `"1001"`), which becomes the AutomationId via the UIA mapping. Use `#1001` or `automationid:1001`. The IDs are stable across runs if the resource file does not change.

**Some old apps use non-standard UIA providers**
Apps predating UIA (pre-Windows Vista) may expose only the MSAA (IAccessible) tree, not UIA. FlaUI reads MSAA-backed elements through the UIA-to-MSAA bridge, but results are less reliable. Consider using the application under test in a test-specific build with UIA attributes added.

## Related docs

- [Selectors](../selectors.md) — full selector grammar including `class:` and `controltype:`
- [Troubleshooting](../troubleshooting.md) — tips for elements that cannot be found
- [Elevated apps guide](elevated-apps.md) — if the Win32 app requires admin
