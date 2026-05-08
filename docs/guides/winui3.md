# WinUI 3

WinUI 3 (Windows App SDK / Project Reunion) is the modern Microsoft UI framework for Windows desktop apps. It targets Windows 10 1809+ and is used for new Microsoft Store apps, Microsoft's own inbox apps (Notepad, Calculator, Clock, etc.), and third-party apps that want the Fluent Design system.

WinUI3 apps expose a UIA tree similar to WPF but with some structural differences due to the framework's hosting model.

## Launching

### Packaged apps (MSIX)

Most WinUI3 apps are distributed as MSIX packages. Launch them by AUMID:

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    Aumid = "YourApp.PackageFamilyName!App"
});
```

To find the AUMID for an installed app:

```powershell
Get-StartApps | Where-Object { $_.Name -like "*YourApp*" }
```

The `AppId` column is the AUMID.

For Microsoft inbox apps, the pattern is:
```
{Publisher}.{AppName}_{PublisherHash}!{EntryPoint}
```
For example: `Microsoft.WindowsNotepad_8wekyb3d8bbwe!App`

### Unpackaged WinUI3 apps

Apps built with WinUI3 but not packaged (no MSIX, running directly from build output) launch like any exe:

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = @"C:\MyApp\win-x64\MyApp.exe"
});
```

### WinAppSDK self-contained apps

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = @"C:\MyApp\MyApp.exe",
    WorkingDirectory = @"C:\MyApp"
});
```

## WinUI3 control → UIA mapping

| WinUI3 control | UIA ControlType | Notes |
|---|---|---|
| `TextBox` | `Edit` | |
| `RichEditBox` | `Document` | |
| `Button` | `Button` | |
| `CheckBox` | `CheckBox` | |
| `RadioButton` | `RadioButton` | |
| `ComboBox` | `ComboBox` | |
| `ListView` | `List` | Items: `ListItem` |
| `GridView` | `List` | Items: `ListItem` |
| `TreeView` | `Tree` | Items: `TreeItem` |
| `NavigationView` | `NavigationView` | See notes |
| `NavigationViewItem` | `ListItem` | Each nav item |
| `TabView` | `Tab` | |
| `TabViewItem` | `TabItem` | |
| `TextBlock` | `Text` | Read-only |
| `ScrollViewer` | `Pane` | |
| `Grid` / `StackPanel` | `Pane` | Layout containers |
| `Frame` | `Pane` | Page navigation frame |
| `Page` | `Pane` | Content within Frame |
| `MenuBar` | `MenuBar` | |
| `MenuBarItem` | `MenuItem` | |
| `AppBarButton` | `Button` | Toolbar-style buttons |
| `Slider` | `Slider` | |
| `ProgressBar` | `ProgressBar` | |
| `ProgressRing` | `ProgressBar` | Indeterminate variant |
| `InfoBar` | `StatusBar` | |
| `Window` | `Window` | |

## Selector patterns

```csharp
// By AutomationId (set via x:Name or AutomationProperties.AutomationId)
page.Locator("#SearchBox")
page.Locator("#PrimaryButton")

// By UIA Name (visible text / accessible name)
page.Locator("name:Search")
page.Locator("name:Settings")

// By ControlType
page.Locator("controltype:Edit")
page.Locator("controltype:Button")

// NavigationView items (each is a ListItem)
page.Locator("controltype:ListItem").Filter(new LocatorFilterOptions { HasText = "Home" })

// Scoped within a Frame/Page
var contentPane = page.Locator("#ContentFrame");
var button = contentPane.Locator("#ActionButton");
```

## Nested Pane wrappers

WinUI3 apps frequently wrap their content in layers of `Pane` elements due to the framework's hosting model:

```
Window
  └─ Pane (XAML Islands host)
       └─ Pane (ContentDialog host or page frame)
            └─ Pane (Page root)
                 └─ [your controls]
```

If a direct locator like `page.Locator("#MyButton")` doesn't find the element but you can see it in Accessibility Insights, check that the search is not blocked by a nested pane that's not part of the UIA subtree being searched.

Use chained locators to drill down:

```csharp
// If the button is inside a named frame
var frame = page.Locator("#MainContentFrame");
var button = frame.Locator("#MyButton");

// Or use the >> combinator
page.Locator("#MainContentFrame >> #MyButton")
```

## Worked example: settings-style app

```csharp
using Flawright;
using Flawright.Locator;
using Xunit;

public class WinUI3AppTests : IAsyncLifetime
{
    private Flawright? _fw;

    public async Task InitializeAsync()
    {
        _fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            Aumid = "com.example.MyApp_abc123xyz!App"
        });
    }

    [Fact]
    public async Task NavigationView_SelectsPage()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Click a NavigationViewItem by its text
        await page.ClickAsync("name:Settings");

        // The settings page should load inside the Frame
        await page.Locator("#SettingsPage").Expect().ToBeVisibleAsync();
    }

    [Fact]
    public async Task SearchBox_FiltersResults()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.FillAsync("#SearchBox", "filter text");

        // Wait for filtered results
        await page.Locator("controltype:ListItem").Expect().ToBeVisibleAsync();
    }

    [Fact]
    public async Task Dialog_Opens_And_Closes()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.ClickAsync("#OpenDialogButton");

        // ContentDialog appears as a Pane with a Window ancestor
        await page.Locator("#ContentDialog").Expect().ToBeVisibleAsync();

        // Close it
        await page.ClickAsync("name:Close");

        await page.Locator("#ContentDialog").Expect().ToBeHiddenAsync();
    }

    public async Task DisposeAsync()
    {
        if (_fw != null)
            await _fw.DisposeAsync();
    }
}
```

## Gotchas

**Packaged app startup time**
MSIX-packaged apps may take several seconds to start up on cold launch (especially on first run after installation). Use `FlawrightOptions.DefaultTimeout` or `LaunchOptions.StartupTimeout` to give the app time to initialize:

```csharp
await using var fw = await Flawright.LaunchAsync(
    new LaunchOptions { Aumid = "..." },
    new FlawrightOptions { DefaultTimeout = TimeSpan.FromSeconds(15) });
```

**ApplicationFrameHost**
Some WinUI3 apps (especially those that also support UWP mode) are hosted in `ApplicationFrameHost.exe`. Flawright handles this transparently for packaged apps launched via AUMID. See also: [UWP / Store apps guide](uwp-store-apps.md).

**ContentDialog is modal but not a separate window**
WinUI3 `ContentDialog` appears inside the app's existing window, not as a separate top-level window. Don't use `WaitForPageAsync` to find it — use `Locator` to find the dialog pane within the current page.

**NavigationView items may not have stable AutomationIds**
If `NavigationViewItem.AutomationId` is not set explicitly via `AutomationProperties.AutomationId`, items may only be findable by their visible text (`name:Home`). Check with Accessibility Insights.

**`Grid`/`StackPanel` in UIA**
Layout-only containers are `Pane` in UIA. They are rarely useful as selector anchors. Target named containers (`x:Name`) or use the `>>` combinator to scope within them.

## Related docs

- [UWP / Store apps guide](uwp-store-apps.md) — for legacy UWP / ApplicationFrameHost apps
- [Selectors](../selectors.md) — `>>` combinator and chained locators
- [Troubleshooting](../troubleshooting.md) — AppContainer sandbox issues
