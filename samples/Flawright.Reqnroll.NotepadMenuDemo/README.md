# Flawright.Reqnroll.NotepadMenuDemo

Demonstrates BDD automation with Flawright and Reqnroll, showcasing **classic menu navigation
and message-box (dialog) handling** with Windows Notepad.

## Prerequisites

- Windows 10 or later (classic Win32 Notepad) or Windows 11 with packaged WinUI3 Notepad
- .NET 10 or later
- Notepad installed (available by default on Windows; WinUI3 version via Microsoft Store or winget)

## Running the Tests

```bash
dotnet test samples/Flawright.Reqnroll.NotepadMenuDemo
```

The test scenarios will auto-skip if Notepad is not installed on the system.

## What It Tests

The feature file (`Features/NotepadMenu.feature`) demonstrates:

- Clicking the **File menu** and asserting that menu items (`New tab`, `Open...`, `Save`) are visible
- Dismissing the open menu with the **Escape** key and asserting items are hidden
- Opening the **Edit menu**, clicking **Select All**, and then **Copy**
- Typing text, sending **Ctrl+W** to trigger the unsaved-changes dialog, asserting the dialog appears,
  and clicking the **Don't save** button to dismiss it

## Selectors

| Selector | Target |
|---|---|
| `name:File` | File menu item in the WinUI3 Notepad ribbon |
| `name:Edit` | Edit menu item in the WinUI3 Notepad ribbon |
| `name:New tab` | New Tab item in the open File menu |
| `name:Select all` | Select All item in the open Edit menu |
| `name:Copy` | Copy item in the open Edit menu |
| `class:Edit` | Classic Win32 Edit control (text area) |
| `name:Don't save` | Discard button in the unsaved-changes dialog (Win11 casing) |

**Note on menu selectors:** The WinUI3 Notepad exposes menu items via UIA `Name` properties.
If running on a build where `name:File` does not resolve, use the built-in keyboard step instead:

```gherkin
When I press "Alt+F" globally
```

**Note on unsaved-changes dialog:** The dialog is triggered by `Ctrl+W` (close tab) on
modern WinUI3 Notepad. Classic Win32 Notepad does not bind `Ctrl+W` — in that case the
scenario will time out waiting for the dialog. This scenario is documented as targeting
modern Notepad specifically.

## Custom Steps

This sample adds the following steps in `NotepadMenuStepDefinitions.cs`:

| Step | Purpose |
|---|---|
| `When I trigger unsaved-changes close on Notepad` | Sends Ctrl+W and waits for the dialog |
| `Then the unsaved-changes dialog should be visible` | Polls for the Don't save button (Win11 + Win10 casings) |
| `When I click the "…" button in the dialog` | Clicks a named dialog button, trying Win11 then Win10 casing |

## Flawright APIs Used

- `IFlawrightPage.ClickAsync(selector)` — menu clicks and dialog button clicks
- `IFlawrightPage.FillAsync(selector, value)` — typing text into the edit control
- `IFlawrightPage.Keyboard.PressAsync(key)` — global key presses (Escape, Ctrl+W)
- `IFlawrightPage.WaitForSelectorAsync(selector)` — waiting for the dialog to appear
- `IFlawrightPage.WaitForTimeoutAsync(ms)` — brief pause after triggering close
- `IFlawrightLocator.IsVisibleAsync()` — checking dialog button visibility
- `IFlawrightLocator.Expect().ToBeVisibleAsync()` — asserting menu items are visible
- `IFlawrightLocator.Expect().ToBeHiddenAsync()` — asserting menu items are dismissed

## Reference

See the [main Flawright README](../../README.md) for more information about selectors and the Flawright framework.
