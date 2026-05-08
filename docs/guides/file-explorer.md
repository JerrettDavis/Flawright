# File Explorer

Windows File Explorer (`explorer.exe`) is a unique automation target: it's both a Win32 shell window and a host for various UIA-accessible controls. The main challenge is that the file list is virtualized — only the items currently visible on screen are materialized in the UIA tree.

## Launching

Launch File Explorer pointing at a specific path:

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = "explorer.exe",
    Arguments = new[] { @"C:\Users\Public\Documents" }
});
```

Launch at the default path (Quick Access):

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = "explorer.exe"
});
```

> **AUMID for Explorer**
>
> The AUMID `Microsoft.Windows.Explorer` technically exists but launching Explorer by AUMID opens a new Explorer window without navigating to a path. Prefer `ApplicationPath = "explorer.exe"` with `Arguments` for test use.

## Selector cheat sheet

| Element | Selector | Notes |
|---|---|---|
| Address bar (text) | `[name*="Address"]` or `controltype:Edit` | The address bar edit control |
| Navigation breadcrumb | `#breadcrumbBar` | May vary by Windows version |
| File list (the pane) | `controltype:List` | Container for file/folder items |
| File / folder item | `controltype:ListItem` | Individual items |
| Navigation pane (left panel) | `controltype:Tree` | Folder tree |
| Navigation pane item | `controltype:TreeItem` | Individual tree node |
| Search box | `controltype:Edit` | When in the search bar |
| View menu button | `name:View` | Toolbar button |
| Up button (parent folder) | `name:Up to` | Navigate up one level |
| Refresh button | `name:Refresh` | |
| New folder button | `name:New folder` | |
| Home tab / Command bar | `controltype:ToolBar` | Command bar at top |

## Virtualized list

The File Explorer file list uses virtualization — only items that are currently scrolled into view appear in the UIA tree. This has two consequences:

1. `CountAsync()` returns the count of *visible* items, not total items.
2. Items that are offscreen have no UIA representation and cannot be found by selector.

To interact with an offscreen item, scroll the list first:

```csharp
// Scroll into view before searching
var list = page.Locator("controltype:List");

// Scroll to the bottom to force materialization
// (No direct scroll API — use keyboard: End key in the list, or use ScrollIntoViewIfNeededAsync
//  on an element you can already find, then search again)
await list.ClickAsync();
await page.PressAsync("controltype:List", "End");

// Now search for the item at the bottom
var lastItem = page.Locator("controltype:ListItem").Filter(
    new LocatorFilterOptions { HasText = "zzz-last-file.txt" });
await lastItem.Expect().ToBeVisibleAsync();
```

Alternatively, use `ScrollIntoViewIfNeededAsync()` on an element near the target, then re-query.

## Worked example: count files and navigate

```csharp
using JerrettDavis.Flawright;
using JerrettDavis.Flawright.Locator;
using Xunit;

public class FileExplorerTests : IAsyncLifetime
{
    private Flawright? _fw;

    public async Task InitializeAsync()
    {
        _fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "explorer.exe",
            Arguments = new[] { @"C:\Windows" }
        });
    }

    [Fact]
    public async Task FileList_ShowsItems()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Wait for the file list to populate
        await page.Locator("controltype:ListItem").Expect().ToBeVisibleAsync();

        var visibleCount = await page.Locator("controltype:ListItem").CountAsync();
        Assert.True(visibleCount > 0, "Expected items in C:\\Windows");
    }

    [Fact]
    public async Task NavigateTo_Subfolder()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Double-click the System32 folder to navigate into it
        var system32 = page.Locator("controltype:ListItem")
            .Filter(new LocatorFilterOptions { HasText = "System32" });

        await system32.Expect().ToBeVisibleAsync();
        await system32.DoubleClickAsync();   // enter the folder via double-click

        // Verify we navigated — address bar should contain System32
        // Wait for the file list to repopulate
        await page.Locator("controltype:ListItem").Expect().ToBeVisibleAsync();
    }

    [Fact]
    public async Task AddressBar_ShowsCurrentPath()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // The address bar edit control contains the current path
        var addressBar = page.Locator("controltype:Edit");
        var path = await addressBar.InnerTextAsync();

        Assert.Contains("Windows", path);
    }

    [Fact]
    public async Task NavigationPane_HasTreeItems()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Left panel navigation tree
        var treeItemCount = await page.Locator("controltype:TreeItem").CountAsync();
        Assert.True(treeItemCount > 0, "Navigation pane should have tree items");
    }

    public async Task DisposeAsync()
    {
        if (_fw != null)
            await _fw.DisposeAsync();
    }
}
```

## Gotchas

**Virtualized list — count is not total count**
`CountAsync()` on the file list returns only the count of currently-rendered (visible) items. To count all items in a folder, use a different approach — e.g., read the status bar text which typically shows total item count, or use `System.IO.Directory.GetFiles()` in your test and compare.

**Multiple Explorer windows**
If Explorer is already open when your test launches, `NewPageAsync()` may return an existing window rather than the newly launched one. Use `GetAllPagesAsync()` to enumerate all Explorer windows and find the one with the expected path, or close existing Explorer windows before the test.

**Explorer window title**
The Explorer window title is the current folder name (e.g., "Windows" for `C:\Windows`). Use `WaitForPageAsync("Windows")` to target a specific window if multiple Explorer windows are open.

**File list population delay**
On network shares or slow storage, the file list may take several seconds to populate. Use `WaitForSelectorAsync` or a `WaitForAsync` loop to wait for at least one item before proceeding.

**Thumbnail generation**
When Explorer is set to icon or thumbnail view, it generates thumbnails in the background. This can cause the list to reflowand items to shift position during automation. Prefer Details view for tests:

```csharp
// Switch to Details view via the View menu or Ctrl+Shift+6 keyboard shortcut
await page.ClickAsync("name:View");
await page.ClickAsync("name:Details");
```

**Context menus**
Right-clicking a file item opens a context menu. Win11 uses a new context menu that may have different structure from Win10. If you need to test right-click operations, verify the menu item names with Accessibility Insights on your target OS version.

## Related docs

- [Selectors](../selectors.md) — `controltype:ListItem`, `controltype:TreeItem` patterns
- [Multi-window apps guide](multi-window.md) — multiple Explorer window handling
- [Troubleshooting](../troubleshooting.md) — virtualized list notes
