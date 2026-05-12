# Flawright.Reqnroll.SettingsDemo

Demonstrates BDD automation with Flawright and Reqnroll against the Windows Settings app
(modern UIA-driven UI launched via AUMID).

## What It Tests

The feature file (`Features/Settings.feature`) demonstrates:

- Launching the Settings app via its Application User Model ID (AUMID)
- Navigating the Settings nav rail (`name:System` → `name:About`)
- Asserting that deep-page elements appear (`name:Device specifications`)
- Using the Settings search box by `automationid:SearchBox`
- Verifying search results appear as `controltype:ListItem` elements
- Using the Back button (`name:Back`) and verifying navigation returns to the previous page
- Generous use of `WaitForSelectorAsync` to handle Settings page-transition animations

## Prerequisites

- Windows 10 or Windows 11 (any edition that ships the full Settings app)
- .NET 10 or later
- Settings app installed (packaged AUMID: `windows.immersivecontrolpanel_cw5n1h2txyewy`)

## Running the Tests

```bash
dotnet test samples/Flawright.Reqnroll.SettingsDemo
```

Scenarios auto-skip when the Settings packaged app is not present (see below).

## Environment Caveats

**Windows Server SKUs**: The Settings app package is absent on Windows Server Core and
many CI runner images (including `windows-2025-vs2026`). All scenarios in this demo will
skip with a clear skip message rather than fail on those environments.

**Element automation IDs change across Windows builds**: Microsoft has changed the
automation IDs and names of Settings elements across Windows 10/11 feature updates.
The feature file uses a combination of `name:` and `automationid:` selectors to maximise
compatibility, with fallback selectors documented in comments:

| Element | Primary selector | Fallback selector |
|---------|-----------------|-------------------|
| Search box | `automationid:SearchBox` | `name:Find a setting` |
| Back button | `name:Back` | `automationid:BackButton` |
| System nav item | `name:System` | `automationid:SystemPage` |
| About entry | `name:About` | `name:About your PC` |

If a selector stops working after a Windows update, check the automation tree with
[Accessibility Insights for Windows](https://accessibilityinsights.io/downloads/) or
the [FlaUI Inspector](https://github.com/FlaUI/FlaUInspect).

**Page transition animations**: Settings uses animated transitions between pages. All
navigation steps are preceded by `WaitForSelector` to wait for the destination element
to appear before asserting. Increase `FlawrightOptions.DefaultTimeout` if running on
slower machines.

## APIs Used

| API | Purpose |
|-----|---------|
| `@aumid:` tag | Launch Settings by AUMID (packaged app) |
| `WaitForSelectorAsync` | Wait for Settings page transitions |
| `ClickAsync` | Navigate the Settings nav rail |
| `FillAsync` / `TypeAsync` | Enter search queries |
| `IsVisibleAsync` / `ToBeVisibleAsync` | Assert elements appear after navigation |
| `TitleAsync` | Read window title |
| `Keyboard.PressAsync` | Send keyboard shortcuts |

## Reference

See the [main Flawright README](../../README.md) for selector syntax and the full API surface.
