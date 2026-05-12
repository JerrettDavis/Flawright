# Flawright.Reqnroll.ExplorerDemo

Demonstrates BDD automation with Flawright and Reqnroll against Windows File Explorer
(`explorer.exe`).

## What It Tests

The feature file (`Features/Explorer.feature`) demonstrates:

- Launching `explorer.exe` via `@launch:explorer.exe`
- Bringing the Explorer window to the front after launch (`BringToFrontAsync`)
- Verifying the window title contains "File Explorer"
- Navigating to a folder via the address bar using the `Alt+D` keyboard shortcut
- Interacting with the Explorer search box (`automationid:SearchBox`)
- Pressing `Enter` to submit a search query
- Reading the window title after navigation to confirm the folder changed

## Prerequisites

- Windows 10 or Windows 11 (any edition with the full desktop shell)
- .NET 10 or later
- Explorer shell running (`explorer.exe` desktop process active)

## Running the Tests

```bash
dotnet test samples/Flawright.Reqnroll.ExplorerDemo
```

Scenarios auto-skip when Explorer is unavailable or the Windows shell is not running
(see below).

## Environment Caveats

**Windows Server Core / headless environments**: `explorer.exe` may exist as a binary
but the shell (desktop) process is not started. The prerequisite hook checks both the
binary's presence _and_ that the `explorer` process is running. On Server Core or
headless CI runners all scenarios will skip rather than fail.

**Explorer's automation tree is notoriously inconsistent**: The UIA element hierarchy
exposed by File Explorer changes significantly across Windows versions and cumulative
updates. Use [Accessibility Insights](https://accessibilityinsights.io/downloads/) or
[FlaUInspect](https://github.com/FlaUI/FlaUInspect) to verify selector names on your
build. Common fallback selectors:

| Element | Primary selector | Fallback selector |
|---------|-----------------|-------------------|
| Address bar | `name:Address band toolbar` | `automationid:Address` |
| Search box | `automationid:SearchBox` | `name:Search Box` |
| Navigation pane | `name:Navigation Pane` | `automationid:NavPane` |
| File list | `automationid:ItemsView` | `name:Items View` |

**`BringToFrontAsync` after launch**: Explorer windows sometimes open in the background,
especially when launched programmatically via Flawright. The feature file calls
`BringToFrontAsync` immediately after the Background step to ensure the window is active
before any interaction.

**Address bar navigation**: Rather than clicking on the breadcrumb (which requires
knowing the exact element label), the demo uses the `Alt+D` keyboard shortcut to focus
the address bar reliably across Windows builds. This matches how power users navigate
Explorer and avoids brittle name-based breadcrumb selectors.

## APIs Used

| API | Purpose |
|-----|---------|
| `@launch:explorer.exe` | Launch File Explorer |
| `BringToFrontAsync` | Bring Explorer window to foreground |
| `TitleAsync` / `WindowTitleShouldContain` | Verify folder navigation |
| `WaitForSelectorAsync` | Wait for UI elements to appear |
| `ClickAsync` | Interact with toolbar and search box |
| `FillAsync` | Enter text in the address/search bar |
| `Keyboard.PressAsync` | Send `Alt+D`, `Enter` shortcuts globally |
| `WaitForTimeoutAsync` | Allow Explorer rendering time |

## Reference

See the [main Flawright README](../../README.md) for selector syntax and the full API surface.
