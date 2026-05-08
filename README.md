# Flawright

A Playwright-flavored API for FlaUI: write Windows desktop UI tests that read like web tests.

[![NuGet](https://img.shields.io/nuget/v/Flawright.svg)](https://www.nuget.org/packages/Flawright)
[![Downloads](https://img.shields.io/nuget/dt/Flawright.svg)](https://www.nuget.org/packages/Flawright)
[![CI](https://github.com/JerrettDavis/Flawright/actions/workflows/ci.yml/badge.svg)](https://github.com/JerrettDavis/Flawright/actions/workflows/ci.yml)
[![CodeQL](https://github.com/JerrettDavis/Flawright/actions/workflows/codeql.yml/badge.svg)](https://github.com/JerrettDavis/Flawright/actions/workflows/codeql.yml)
[![codecov](https://codecov.io/gh/JerrettDavis/Flawright/graph/badge.svg)](https://codecov.io/gh/JerrettDavis/Flawright)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512bd4)](https://dotnet.microsoft.com)
[![Documentation](https://img.shields.io/badge/docs-online-blue.svg)](https://jerrettdavis.github.io/Flawright/)

## Why Flawright?

Raw FlaUI gets the job done, but every test is a wall of boilerplate:

- **Raw FlaUI** requires manually building `ConditionFactory`, calling `FindFirstDescendant`, casting to the right pattern, and guarding every call against null — before you've clicked a single button.
- **Playwright** proved that a fluent locator API with auto-waiting produces tests that are shorter, more readable, and less flaky. Flawright brings that model to Windows desktop automation.
- **Selector strings** keep tests decoupled from the UI tree. `page.Locator("name:Save")` reads at a glance; `_cf.ByName("Save", PropertyConditionFlags.None)` does not.
- **Async throughout.** Every action returns a `Task`, composing naturally with `async`/`await` test frameworks.
- **Auto-waiting.** Locator operations retry with a configurable polling interval until the element appears or the timeout expires — no manual `Task.Delay` loops needed.
- **No plumbing to own.** `Flawright.LaunchAsync(options)` — one call and you are writing assertions, not scaffolding.

## Install

```bash
dotnet add package Flawright
```

**Prerequisites**

- Windows 10 or later (UI Automation requires a desktop session)
- .NET 10.0
- The target application must be accessible via UI Automation (UIA3). Use [Accessibility Insights](https://accessibilityinsights.io/) or the built-in `inspect.exe` to verify.

## Quickstart

Launch Notepad, type some text, and assert it landed:

```csharp
using JerrettDavis.Flawright;

await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = "notepad.exe"   // auto-resolves to AUMID on Windows 11
});

var page = await fw.Browser.NewPageAsync();

// Fill the editor — use the AutomationId of the editor control
await page.FillAsync("#RichEditBox", "Hello from Flawright!");

// Assert the text is present
await page.Locator("#RichEditBox").Expect().ToBeVisibleAsync();

// Take a screenshot — returns PNG bytes; also saves to file if path is given
byte[] png = await page.ScreenshotAsync(@"C:\temp\notepad.png");
```

> **Windows 10 vs Windows 11 Notepad**
>
> Different Windows versions ship different Notepad implementations:
>
> - **Windows 11 Notepad** (WinUI3, packaged app): editor `AutomationId` is `RichEditBox` — use `#RichEditBox`.
> - **Classic Windows 10 Notepad** (Win32): editor `ControlType` is `Edit` — use `controltype:Edit`.
>
> When in doubt, inspect the live UI tree with [Accessibility Insights for Windows](https://accessibilityinsights.io/) or [FlaUI Inspect](https://github.com/FlaUI/FlaUI/wiki/Tools#flauiinspect) to find the right selector for your system.
>
> On Windows 11, `ApplicationPath = "notepad.exe"` is automatically redirected to
> `Application.LaunchStoreApp("Microsoft.WindowsNotepad_8wekyb3d8bbwe!App")` so FlaUI
> binds to the real packaged app instead of the short-lived alias stub.

To attach to an already-running process instead of launching a new one:

```csharp
await using var fw = await Flawright.AttachAsync(new AttachOptions
{
    ProcessId = 12345
});
```

## Selector Syntax

Selectors are strings with an optional `prefix:` followed by a value. Without a prefix, the string is treated as a `name:` match against the element's UIA Name property.

| Prefix | Matches | Example |
|---|---|---|
| *(no prefix)* | UIA Name (smart fallback) | `"Save"` |
| `text:` | UIA Name property | `"text:Save"` |
| `name:` | UIA Name property (alias for `text:`) | `"name:Save"` |
| `#` | AutomationId (CSS shorthand) | `"#btn_save"` |
| `automationid:` | AutomationId (explicit form) | `"automationid:btn_save"` |
| `class:` or `[class=...]` | ClassName | `"class:Button"` |
| `role:` or `[role=...]` | UIA ControlType | `"role:Button"` |
| `controltype:` | UIA ControlType (alias for `role:`) | `"controltype:Edit"` |
| `[name=...]` | UIA Name (attribute syntax) | `"[name=OK]"` |

**Supported control type values** for `controltype:` / `role:`: `button`, `checkbox`, `combobox`, `dropdown`, `edit`, `textbox`, `input`, `list`, `listitem`, `menu`, `menubar`, `menuitem`, `radiobutton`, `tab`, `tabitem`, `text`, `label`, `window`, `group`, `image`, `link`, `hyperlink`, `progressbar`, `scrollbar`, `slider`, `spinner`, `statusbar`, `table`, `toolbar`, `tooltip`, `tree`, `treeitem`, `separator`, `pane`, `document`, `header`, `headeritem`.

Any unrecognized value maps to `ControlType.Custom`.

## API Overview

### `Flawright` — entry point

Call `Flawright.LaunchAsync` (static, one step) to launch an application. The returned `Flawright` instance owns the browser and disposes it on `DisposeAsync`.

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = "notepad.exe"
});
```

To customize timeouts and screenshot output:

```csharp
await using var fw = await Flawright.LaunchAsync(
    new LaunchOptions { ApplicationPath = "notepad.exe" },
    new FlawrightOptions
    {
        DefaultTimeout      = TimeSpan.FromSeconds(10),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(50),
        ScreenshotDirectory  = @"C:\TestOutput"
    });
```

### `IFlawrightBrowser` — the application

Wraps a running process. Access via `fw.Browser`. Call `NewPageAsync` to get the main window, `GetAllPagesAsync` to enumerate all top-level windows, or `WaitForPageAsync` to wait for a window by title.

```csharp
var page = await fw.Browser.NewPageAsync();

// All top-level windows
var pages = await fw.Browser.GetAllPagesAsync();

// Wait for a specific window to appear
var dialog = await fw.Browser.WaitForPageAsync("Save As", timeout: TimeSpan.FromSeconds(10));
```

### `IFlawrightPage` — a window

Corresponds to a top-level window. The primary surface for interacting with the application.

```csharp
// Click a button by name
await page.ClickAsync("name:OK");

// Fill a text box via ValuePattern (fast, single shot)
await page.FillAsync("controltype:Edit", "some text");

// Type character-by-character (realistic key events for reactive controls)
await page.TypeAsync("controltype:Edit", "hello");

// Press a key or chord
await page.PressAsync("controltype:Edit", "Ctrl+S");

// Check / uncheck a toggle
await page.CheckAsync("controltype:CheckBox");
await page.UncheckAsync("controltype:CheckBox");

// Select a combo box option by value
await page.SelectOptionAsync("controltype:ComboBox", "Option A");

// Wait for an element to appear and return it
var el = await page.WaitForSelectorAsync("name:Loading Complete");

// Create a locator for chaining
var locator = page.Locator("#username");

// Window title
var title = await page.TitleAsync();
```

### `IFlawrightLocator` — a lazy element query

A locator is a reusable description of how to find an element. It does not execute until you call an action on it. This lets you define locators once and assert them multiple times. All resolution is auto-waited.

```csharp
using JerrettDavis.Flawright.Locator; // for LocatorFilterOptions

var saveButton = page.Locator("name:Save");

// Resolves the element (auto-waited) and clicks it
await saveButton.ClickAsync();

// Get the first match (sync — returns a new locator narrowed to the first element)
var firstLocator = saveButton.First;

// Count matching elements (no wait — returns current count)
var count = await page.Locator("controltype:Button").CountAsync();

// Get the nth match (0-indexed), sync — returns a new narrowed locator
var second = page.Locator("controltype:ListItem").Nth(1);

// Get all matching elements (auto-waits for at least one)
var all = await page.Locator("controltype:ListItem").AllAsync();

// Filter locator results by text content
var filtered = page.Locator("controltype:ListItem")
    .Filter(new LocatorFilterOptions { HasText = "Save" });

// Enter the assertion chain
await saveButton.Expect().ToBeEnabledAsync();
```

### `IFlawrightElement` — a resolved element

Returned by `AllAsync` (or via `ElementHandleAsync` for advanced use). Exposes actions on the concrete UIA element.

```csharp
// Click
await element.ClickAsync();

// Double-click
await element.DoubleClickAsync();

// Fill (ValuePattern — fast value set)
await element.FillAsync("new value");

// Read text (ValuePattern → TextPattern → Name fallback)
var text = await element.TextAsync();

// State checks
bool visible = await element.IsVisibleAsync();
bool enabled = await element.IsEnabledAsync();
bool checked_ = await element.IsCheckedAsync();

// Mouse / focus
await element.HoverAsync();
await element.FocusAsync();
await element.ScrollIntoViewIfNeededAsync();

// Read a UIA attribute by name
var id = await element.GetAttributeAsync("AutomationId");
```

### `IFlawrightAssertions` — expect chain

Returned by `locator.Expect()`. All methods auto-wait and throw `FlawrightTimeoutException` on timeout.

```csharp
// Visibility
await page.Locator("name:Submit").Expect().ToBeVisibleAsync();
await page.Locator("name:Hidden").Expect().ToBeHiddenAsync();

// Enabled state
await page.Locator("controltype:Button").Expect().ToBeEnabledAsync();
await page.Locator("controltype:Button").Expect().ToBeDisabledAsync();

// Text content
await page.Locator("name:Result").Expect().ToHaveTextAsync("42");

// ValuePattern value (edit controls)
await page.Locator("controltype:Edit").Expect().ToHaveValueAsync("hello");

// Toggle / checkbox state
await page.Locator("controltype:CheckBox").Expect().ToBeCheckedAsync();

// Count
await page.Locator("controltype:ListItem").Expect().ToHaveCountAsync(5);

// Negation — each positive assertion has a .Not counterpart
await page.Locator("name:Spinner").Expect().Not.ToBeVisibleAsync();
await page.Locator("controltype:Button").Expect().Not.ToBeDisabledAsync();
```

### Screenshots

```csharp
// Returns PNG bytes only
byte[] png = await page.ScreenshotAsync();

// Saves to a file and returns the bytes
byte[] png = await page.ScreenshotAsync(@"C:\temp\screenshot.png");

// Auto-saves to FlawrightOptions.ScreenshotDirectory when no path is given
// and ScreenshotDirectory is configured
```

### Multi-window

```csharp
// Main window
var page = await fw.Browser.NewPageAsync();

// All current top-level windows
var pages = await fw.Browser.GetAllPagesAsync();

// Wait for a dialog/window to appear by title substring
var saveDialog = await fw.Browser.WaitForPageAsync("Save As");
```

## Differences from Playwright (web)

| Concept | Playwright (web) | Flawright (desktop) |
|---|---|---|
| Browser | Chromium / Firefox / WebKit | A Windows process (EXE) |
| Page | Browser tab or window | Top-level Window (UIA `Window`) |
| Locator | CSS / XPath / ARIA roles | UIA properties (Name, AutomationId, ControlType) |
| Selector syntax | `#id`, `.class`, `role=button` | `#id`, `name:`, `controltype:`, `text:`, `class:`, `role:` |
| Headless mode | Supported | Not applicable — requires a display |
| Platform | Cross-platform | Windows only |
| Networking | Intercept, mock, HAR | Not applicable |
| JavaScript | `page.evaluate()` | Not applicable |

## Comparison to Raw FlaUI

The same task — clicking the "OK" button in a dialog — raw FlaUI vs. Flawright:

**Raw FlaUI**

```csharp
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

var app = Application.Launch("myapp.exe");
using var automation = new UIA3Automation();
var window = app.GetMainWindow(automation);

var cf = new ConditionFactory(automation.PropertyLibrary);
var button = window.FindFirstDescendant(cf.ByName("OK"));
if (button == null)
    throw new Exception("OK button not found");

button.AsButton().Invoke();
```

**Flawright**

```csharp
using JerrettDavis.Flawright;

await using var fw = await Flawright.LaunchAsync(new LaunchOptions { ApplicationPath = "myapp.exe" });
var page = await fw.Browser.NewPageAsync();

await page.ClickAsync("name:OK");
```

## Behavior-driven testing with Reqnroll

Flawright ships a companion package `Flawright.Reqnroll` for writing Gherkin/BDD tests:

```bash
dotnet add package Flawright.Reqnroll
```

```gherkin
@launch:notepad.exe
Scenario: Type and verify text in Notepad
    Given I have the application in focus
    When I fill "[name=\"Text editor\"]" with "Hello from Flawright!"
    Then "[name=\"Text editor\"]" should contain "Hello"
```

See [docs/bdd.md](docs/bdd.md) for the full step reference, tag forms, and DI patterns.

## Documentation

Full documentation site: [jerrettdavis.github.io/Flawright](https://jerrettdavis.github.io/Flawright/)

- [Getting Started](docs/getting-started.md)
- [Selector Syntax](docs/selectors.md)
- [Auto-waiting](docs/auto-waiting.md)
- [Assertions](docs/assertions.md)
- [Examples](docs/examples.md)
- [BDD with Reqnroll](docs/bdd.md)
- [Per-app guides](docs/guides/) — Win11 Notepad, Calculator, File Explorer, classic Win32, WinForms, WPF, WinUI3, UWP/Store, multi-window, installer wizards, elevated apps
- [Versioning & Stability](docs/versioning.md)
- [Performance Guide](docs/performance.md)
- [Troubleshooting](docs/troubleshooting.md)
- [API Reference](https://jerrettdavis.github.io/Flawright/api/)

## Project Layout

```
Flawright/
├── src/
│   ├── JerrettDavis.Flawright/          # Core library
│   │   ├── Flawright.cs                  # Entry point (LaunchAsync / AttachAsync)
│   │   ├── FlawrightBrowser.cs           # Application wrapper (IFlawrightBrowser)
│   │   ├── FlawrightPage.cs              # Window wrapper (IFlawrightPage)
│   │   ├── FlawrightLocator.cs           # Lazy element query (IFlawrightLocator)
│   │   ├── FlawrightElement.cs           # Resolved element (IFlawrightElement)
│   │   ├── FlawrightAssertions.cs        # Assertion chain (IFlawrightAssertions)
│   │   ├── FlawrightOptions.cs           # Global options (timeout, retry, screenshot dir)
│   │   ├── FlawrightTimeoutException.cs  # Timeout exception
│   │   ├── Interfaces.cs                 # All public interfaces
│   │   ├── AutoWait.cs                   # Internal polling loop
│   │   ├── Selectors/SelectorParser.cs   # Selector string → FlaUI condition
│   │   └── Input/KeyParser.cs            # Key/chord string → FlaUI keyboard input
│   └── JerrettDavis.Flawright.Reqnroll/  # BDD companion package
│       ├── FlawrightReqnrollHooks.cs     # BeforeScenario / AfterScenario lifecycle
│       ├── FlawrightReqnrollOptions.cs   # Global BDD options (default app path, timeout)
│       ├── FlawrightSteps.cs             # 25 built-in step bindings
│       └── TagParser.cs                  # @launch / @aumid / @attach tag parsing
├── samples/
│   ├── Flawright.Reqnroll.NotepadDemo/   # Gherkin-driven Notepad tests
│   └── Flawright.Reqnroll.CalculatorDemo/# Gherkin-driven Calculator tests
├── tests/
│   ├── JerrettDavis.Flawright.UnitTests/ # Unit tests (SelectorParser, KeyParser, AutoWait)
│   └── JerrettDavis.Flawright.E2ETests/  # E2E tests (Notepad, Calculator)
└── docs/                                 # Extended documentation
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE)
