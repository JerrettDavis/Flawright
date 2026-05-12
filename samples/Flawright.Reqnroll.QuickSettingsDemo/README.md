# Flawright.Reqnroll.QuickSettingsDemo

Demonstrates BDD automation with Flawright and Reqnroll interacting with the Windows
Quick Settings flyout (opened via **Win+A**).

## What It Tests

The feature file (`Features/QuickSettings.feature`) demonstrates:

- Attaching to the Windows shell (`ShellExperienceHost.exe`) via `@attach:ShellExperienceHost`
- Triggering the Quick Settings flyout by sending `Win+A` (`Meta+A`) via `Keyboard.PressAsync`
- Waiting for the flyout panel to appear (`automationid:QuickSettingsView`)
- Clicking the Wi-Fi toggle button (`name:Wi-Fi`)
- Restoring the original toggle state after each interaction
- Listing available Wi-Fi networks (read-only — does not connect)

## How Quick Settings Works with Flawright

Quick Settings is a **system flyout** owned by `ShellExperienceHost.exe`. It cannot be
launched via `@launch:` or `@aumid:` because it is not a standalone application —
it is a surface rendered by the Windows shell on demand.

**The approach used in this demo:**

1. `@attach:ShellExperienceHost` — attach Flawright to the running shell process
2. `I press "Meta+A" globally` — send the `Win+A` chord to trigger the flyout
3. `WaitForSelector` — wait for the flyout panel to appear in the UIA tree
4. Interact with elements inside the flyout

The shell process (`ShellExperienceHost.exe`) owns the Quick Settings window, so
attaching to it gives Flawright access to the flyout's UIA subtree once it is open.

## Prerequisites

- Windows 11 (Quick Settings as described requires the Windows 11 shell)
- Full desktop shell running (`ShellExperienceHost.exe` process active)
- .NET 10 or later
- A physical or virtual Wi-Fi adapter (for Wi-Fi toggle scenarios)

## Running the Tests

```bash
dotnet test samples/Flawright.Reqnroll.QuickSettingsDemo
```

All scenarios auto-skip when `ShellExperienceHost.exe` is not running.

## Environment Caveats

**CI runners (windows-2025-vs2026)**: `ShellExperienceHost.exe` is **not present** on
Windows Server 2025 / Server Core. All three scenarios in this demo will skip on CI.
This is expected and by design — this demo targets interactive Windows 11 developer
machines only. The README and CI comments make this explicit.

**Windows 10**: The Quick Settings flyout existed in Windows 10 but was implemented
differently (Action Center, hosted in a different process). The `ShellExperienceHost`
attach strategy targets Windows 11. On Windows 10 the scenarios may skip if
`ShellExperienceHost` is absent or the flyout UIA tree differs significantly.

**Wi-Fi hardware required**: The Wi-Fi toggle and network list scenarios require a
Wi-Fi adapter to be present. On VMs without a Wi-Fi adapter the toggle button may
be absent or disabled. The feature file does not attempt to connect to any network —
it only reads state and toggles the toggle, restoring original state.

**Flyout selector fallbacks**: The Quick Settings flyout UIA tree changes across
Windows 11 feature updates. Known selector variations:

| Element | Primary selector | Fallback selector |
|---------|-----------------|-------------------|
| Flyout panel | `automationid:QuickSettingsView` | `name:Quick Settings` |
| Wi-Fi toggle | `name:Wi-Fi` | `automationid:WiFiButton` |
| Network picker | `name:Manage Wi-Fi connections` | `name:Wi-Fi network` |
| Bluetooth toggle | `name:Bluetooth` | `automationid:BluetoothButton` |

Use [Accessibility Insights](https://accessibilityinsights.io/downloads/) to inspect
the live UIA tree while the flyout is open.

**Destructive actions avoided**: The "List available networks" scenario reads the network
list without connecting. Connecting to a network requires credentials and is destructive
(it changes machine state). This scenario intentionally only asserts element visibility.

## APIs Used

| API | Purpose |
|-----|---------|
| `@attach:ShellExperienceHost` | Attach to the Windows shell process |
| `Keyboard.PressAsync("Meta+A")` | Trigger the Quick Settings flyout |
| `WaitForSelectorAsync` | Wait for flyout to open |
| `ClickAsync` | Toggle the Wi-Fi button |
| `WaitForTimeoutAsync` | Allow flyout animation to complete |
| `IsVisibleAsync` / `ToBeVisibleAsync` | Assert flyout elements are present |

## Reference

See the [main Flawright README](../../README.md) for selector syntax and the full API surface.
