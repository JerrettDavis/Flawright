# Flawright.Reqnroll.NotepadDemo

Demonstrates BDD automation with Flawright and Reqnroll against Windows Notepad.

## Prerequisites

- Windows 10 or later (classic Win32 Notepad) or Windows 11 with packaged WinUI3 Notepad
- .NET 10 or later
- Notepad installed (available by default on Windows)

## Running the Tests

```bash
dotnet test samples/Flawright.Reqnroll.NotepadDemo
```

The test scenarios will auto-skip if Notepad is not installed on the system.

## What It Tests

The feature file (`Features/Notepad.feature`) demonstrates:

- Filling text into the Notepad Edit control via `ValuePattern`
- Clearing text and verifying the control is empty
- Verifying the window title contains "Notepad"
- Typing character-by-character into the Edit control
- Taking screenshots during test execution

## Selectors

The samples use `class:Edit` to reference the classic Edit control in Notepad. This selector works on both packaged WinUI3 Notepad (Windows 11) and classic Win32 Notepad (Windows 10 / Server).

## Reference

See the [main Flawright README](../../README.md) for more information about selectors and the Flawright framework.
