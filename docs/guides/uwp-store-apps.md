# UWP / Store Apps

Universal Windows Platform (UWP) apps are the pre-WinUI3 Store app format. They run in an AppContainer sandbox and are hosted by `ApplicationFrameHost.exe`, which means the process you see in Task Manager is not the app's own process. Examples include Settings, Mail, Photos, Maps, Microsoft Store, and older Windows-inbox apps.

Many UWP apps are still shipping and actively maintained. New Microsoft development prefers WinUI3, but UWP remains common for high-security or sandboxed scenarios.

## Finding the AUMID

UWP apps must be launched by AUMID — there is no standalone executable path. Use PowerShell to find the AUMID of an installed app:

```powershell
# List all Start-menu apps with their AUMIDs
Get-StartApps | Format-Table Name, AppId

# Filter by name
Get-StartApps | Where-Object { $_.Name -like "*Settings*" }
```

Or use the Windows Package Manager:

```powershell
# List packages with their family names
Get-AppxPackage | Select-Object Name, PackageFamilyName, Version
```

The AUMID format is `{PackageFamilyName}!{ApplicationId}`. The ApplicationId is usually the entry point defined in the app's `Package.appxmanifest`.

### Common UWP AUMIDs (Windows 11)

| App | AUMID |
|---|---|
| Settings | `windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel` |
| Mail | `microsoft.windowscommunicationsapps_8wekyb3d8bbwe!microsoft.windowslive.mail` |
| Photos | `microsoft.windows.photos_8wekyb3d8bbwe!app` |
| Microsoft Store | `microsoft.windowsstore_8wekyb3d8bbwe!app` |
| Maps | `microsoft.windowsmaps_8wekyb3d8bbwe!app` |
| Calculator (older) | `microsoft.windowscalculator_8wekyb3d8bbwe!app` |
| Weather | `microsoft.bingweather_8wekyb3d8bbwe!app` |

> **These AUMIDs may vary by Windows version and update.** Always verify with `Get-StartApps` on your target machine before writing tests.

## Launching

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    Aumid = "windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel"
});
```

The `LaunchOptions.ApplicationPath` property cannot be used for UWP/sandboxed apps — they have no standalone `.exe`. `LaunchOptions.Aumid` is mandatory.

## ApplicationFrameHost

UWP apps are hosted in `ApplicationFrameHost.exe` (a shell process). The app's actual UI runs in a separate process inside the AppContainer. Flawright transparently attaches to the real app process after launch via AUMID — you do not need to account for the host process in your selectors.

What this means in practice:
- `fw.Browser.NewPageAsync()` returns the app's main window, not the ApplicationFrameHost chrome.
- The title bar provided by the shell (window chrome, min/max/close buttons) is controlled by ApplicationFrameHost and is not part of the app's UIA tree.
- Selector resolution starts at the app's root pane, below the shell chrome.

## Selector patterns

UWP controls use the same WinUI-based UIA mappings as WinUI3 controls (see [WinUI 3 guide](winui3.md) for the full table). UIA AutomationId comes from `AutomationProperties.AutomationId` in XAML:

```csharp
// Typical UWP patterns
page.Locator("controltype:Button")
page.Locator("name:Settings")
page.Locator("#SearchBox")    // If AutomationId is set
page.Locator("controltype:ListItem").Nth(0)
```

## Worked example: Settings app

Navigating Windows Settings to a specific page:

```csharp
using Flawright;
using Xunit;

public class SettingsAppTests : IAsyncLifetime
{
    private Flawright? _fw;

    public async Task InitializeAsync()
    {
        _fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            Aumid = "windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel"
        });
    }

    [Fact]
    public async Task SettingsApp_Opens()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Settings should have a navigation pane on the left
        await page.Locator("controltype:NavigationView").Expect().ToBeVisibleAsync();
    }

    [Fact]
    public async Task SearchSettings_FindsResults()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Find the search box at the top of Settings
        // AutomationId may vary by Windows version — use Accessibility Insights to verify
        await page.FillAsync("controltype:Edit", "bluetooth");

        // Results should appear
        await page.Locator("controltype:ListItem").Expect().ToBeVisibleAsync();
    }

    public async Task DisposeAsync()
    {
        if (_fw != null)
            await _fw.DisposeAsync();
    }
}
```

## AppContainer sandbox limitations

UWP apps run in an AppContainer with restricted permissions. This affects automation in two ways:

1. **UIA cross-process boundary**: The test process must have the `uiAccess=true` attribute in its manifest, and it must run from a trusted location (e.g., `Program Files`). Without this, the UIA tree is visible but element children may be inaccessible.

2. **File system restrictions**: AppContainer apps cannot read from arbitrary file paths. Tests that involve file operations (opening files via the app's file picker) must work through the app's own dialogs.

See [Troubleshooting — AppContainer apps](../troubleshooting.md#appcontainer-apps-uwp--winui3) for the workaround details.

## Gotchas

**Settings app UIA structure changes between Windows versions**
Microsoft updates the Settings app structure frequently. Tests against Settings (or other Microsoft-managed apps) may break when Windows updates. Use ControlType-based selectors with visible text as a fallback rather than relying on AutomationIds that may not be stable across Windows versions.

**Photos app and similar media apps**
Media apps that display photos or videos may use DirectComposition surfaces that are not part of the UIA tree. UI elements like toolbars and buttons are accessible; the media surface itself is not.

**Launch time**
UWP apps can take several seconds to start cold. Use a generous `DefaultTimeout`:

```csharp
await using var fw = await Flawright.LaunchAsync(
    new LaunchOptions { Aumid = "..." },
    new FlawrightOptions { DefaultTimeout = TimeSpan.FromSeconds(20) });
```

**Multiple instances**
Some UWP apps (Photos, Mail) support multiple windows. Use `GetAllPagesAsync()` to enumerate them and `WaitForPageAsync(title)` to wait for a specific one.

## Related docs

- [WinUI 3 guide](winui3.md) — WinUI3-specific details (many overlap)
- [Elevated apps guide](elevated-apps.md) — if UAC is involved
- [Troubleshooting](../troubleshooting.md) — AppContainer sandbox notes
