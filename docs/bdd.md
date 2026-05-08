# BDD Testing with Reqnroll (`Flawright.Reqnroll`)

The `Flawright.Reqnroll` companion package lets you write Gherkin/BDD tests against Windows desktop applications using [Reqnroll](https://reqnroll.net/) (the .NET successor to SpecFlow).

---

## Quick start

### 1. Install the package

```bash
dotnet add package Flawright.Reqnroll
dotnet add package Reqnroll.xUnit       # or Reqnroll.NUnit / Reqnroll.MsTest
dotnet add package Reqnroll.Tools.MsBuild.Generation
dotnet add package Microsoft.NET.Test.Sdk
```

### 2. Add `reqnroll.json`

Create `reqnroll.json` at your project root:

```json
{
  "$schema": "https://reqnroll.net/schemas/reqnroll-config-2.0.json",
  "bindingCulture": { "name": "en-US" },
  "trace": { "traceSuccessfulSteps": false }
}
```

### 3. Write a feature file

```gherkin
@launch:notepad.exe
Feature: Notepad smoke test

Scenario: Type and verify text
    Given I have the application in focus
    When  I fill "[name=\"Text editor\"]" with "Hello from Flawright!"
    Then  "[name=\"Text editor\"]" should contain "Hello"
```

### 4. Run

```bash
dotnet test
```

No bindings file needed — `Flawright.Reqnroll` ships 25 ready-to-use step bindings.

---

## Tag reference

Tags on a feature or scenario tell `Flawright.Reqnroll` how to start (or attach to) the application for each scenario. One tag per scenario is usually enough.

| Tag | Effect | Example |
|-----|--------|---------|
| `@launch:<path>` | Launch by executable path (Win32 or alias) | `@launch:notepad.exe` |
| `@aumid:<aumid>` | Launch a Store/UWP/WinUI3 app by AUMID | `@aumid:Microsoft.WindowsCalculator_8wekyb3d8bbwe!App` |
| `@attach:<name>` | Attach to a running process by name | `@attach:notepad` |
| `@attachpid:<pid>` | Attach to a running process by PID | `@attachpid:12345` |

**Tag precedence (highest to lowest):** `@attachpid:` > `@attach:` > `@aumid:` > `@launch:` > `FlawrightReqnrollOptions.DefaultAumid` > `FlawrightReqnrollOptions.DefaultApplicationPath`.

Feature-level tags apply to every scenario in the feature. Scenario-level tags override feature-level ones.

> **Windows 11 Notepad note:** `@launch:notepad.exe` auto-resolves to `LaunchStoreApp("Microsoft.WindowsNotepad_8wekyb3d8bbwe!App")` on Windows 11 via Flawright's built-in `AppExecutionAliasResolver`. The Win11 editor selector is `[name="Text editor"]` (not `controltype:Edit`).

---

## Built-in step reference

### Window focus

| Step | Regex |
|------|-------|
| `Given I have the application in focus` | `I have the application in focus` |
| `When I bring the application to the front` | `I bring the application to the front` |

### Mouse actions

| Step | Example |
|------|---------|
| `When I click "<selector>"` | `When I click "name:OK"` |
| `When I double-click "<selector>"` | `When I double-click "name:My Item"` |
| `When I right-click "<selector>"` | `When I right-click "name:My File"` |

### Text input

| Step | Example |
|------|---------|
| `When I fill "<selector>" with "<value>"` | `When I fill "#RichEditBox" with "Hello!"` |
| `When I type "<text>" into "<selector>"` | `When I type "hello" into "#RichEditBox"` |
| `When I clear "<selector>"` | `When I clear "#SearchBox"` |

### Keyboard

| Step | Example |
|------|---------|
| `When I press "<key>" globally` | `When I press "Ctrl+S" globally` |
| `When I press "<key>" on "<selector>"` | `When I press "Enter" on "#Submit"` |
| `When I type "<text>" globally` | `When I type "Hello World" globally` |

### Element state

| Step | Example |
|------|---------|
| `When I focus "<selector>"` | `When I focus "#SearchBox"` |
| `When I hover over "<selector>"` | `When I hover over "name:Help"` |

### Toggle / checkbox

| Step | Example |
|------|---------|
| `When I check "<selector>"` | `When I check "name:Dark Mode"` |
| `When I uncheck "<selector>"` | `When I uncheck "name:Notifications"` |

### Selection

| Step | Example |
|------|---------|
| `When I select "<value>" from "<selector>"` | `When I select "Dark" from "name:Theme"` |

### Wait

| Step | Example |
|------|---------|
| `When I wait for <N> milliseconds` | `When I wait for 500 milliseconds` |
| `When I wait for selector "<selector>"` | `When I wait for selector "name:Ready"` |

### Drag and drop

| Step | Example |
|------|---------|
| `When I drag "<source>" to "<target>"` | `When I drag "name:File" to "name:Folder"` |

### Assertions

| Step | Example |
|------|---------|
| `Then "<selector>" should be visible` | `Then "name:Save" should be visible` |
| `Then "<selector>" should be hidden` | `Then "name:Spinner" should be hidden` |
| `Then "<selector>" should be enabled` | `Then "name:Submit" should be enabled` |
| `Then "<selector>" should be disabled` | `Then "name:Submit" should be disabled` |
| `Then "<selector>" should be checked` | `Then "name:Remember Me" should be checked` |
| `Then "<selector>" should contain "<text>"` | `Then "#Editor" should contain "Hello"` |
| `Then "<selector>" should have text "<text>"` | `Then "name:Status" should have text "Ready"` |
| `Then "<selector>" should have value "<value>"` | `Then "#Amount" should have value "42"` |
| `Then "<selector>" should be empty` | `Then "#SearchBox" should be empty` |
| `Then "<selector>" should have count <N>` | `Then "controltype:ListItem" should have count 3` |
| `Then the window title should be "<title>"` | `Then the window title should be "Notepad"` |
| `Then the window title should contain "<text>"` | `Then the window title should contain "Notepad"` |

---

## DI / sharing state between bindings

`IFlawright`, `IFlawrightBrowser`, and `IFlawrightPage` are registered into Reqnroll's BoDi container before any step runs. Inject them via constructor in your own `[Binding]` class:

```csharp
using JerrettDavis.Flawright;
using Reqnroll;

[Binding]
public class MyCustomSteps
{
    private readonly IFlawrightPage _page;

    public MyCustomSteps(IFlawrightPage page)
    {
        _page = page;
    }

    [When(@"I save with keyboard shortcut")]
    public async Task SaveAsync()
    {
        await _page.Keyboard.PressAsync("Ctrl+S");
    }
}
```

The page is disposed automatically after each scenario — you never call `DisposeAsync` on it yourself.

---

## Overriding built-in steps

Define a step with the same regex in your own `[Binding]` class. Reqnroll resolves the most specific match. To replace the built-in fill step with one that also logs:

```csharp
[When(@"I fill ""([^""]*)"" with ""([^""]*)""")]
public async Task FillAndLogAsync(string selector, string value)
{
    Console.WriteLine($"[FILL] {selector} <- {value}");
    await _page.FillAsync(selector, value);
}
```

---

## Global defaults with `FlawrightReqnrollOptions`

Register options once in a `[BeforeTestRun]` hook to set a default app path and timeout for the whole test run:

```csharp
using JerrettDavis.Flawright;
using JerrettDavis.Flawright.Reqnroll;
using Reqnroll;
using Reqnroll.BoDi;

[Binding]
public static class TestRunHooks
{
    [BeforeTestRun]
    public static void Configure(IObjectContainer container)
    {
        container.RegisterInstanceAs(new FlawrightReqnrollOptions
        {
            DefaultApplicationPath = @"C:\MyApp\MyApp.exe",
            FlawrightOptions = new FlawrightOptions
            {
                DefaultTimeout       = TimeSpan.FromSeconds(15),
                DefaultRetryInterval = TimeSpan.FromMilliseconds(50)
            }
        });
    }
}
```

Any scenario without an explicit tag will then use `DefaultApplicationPath` as the launch target.

---

## `reqnroll.json` — minimal config

```json
{
  "$schema": "https://reqnroll.net/schemas/reqnroll-config-2.0.json",
  "bindingCulture": { "name": "en-US" },
  "trace": { "traceSuccessfulSteps": false }
}
```

---

## Limitations

- **Interactive desktop session required.** UI Automation only works in a real desktop session. On headless CI servers, a virtual display or Remote Desktop session is required. Use a Windows runner in GitHub Actions with `runs-on: windows-latest`.
- **One scenario = one application instance.** Each scenario gets a fresh app launch (for `@launch:` / `@aumid:`) or attaches fresh (for `@attach:` / `@attachpid:`). Scenarios are not parallelised against the same app instance.
- **No xUnit parallel execution.** Disable xUnit parallelism in your project by adding `[assembly: CollectionBehavior(DisableTestParallelization = true)]` or setting `parallelizeTestCollections: false` in `xunit.runner.json` to prevent multiple scenarios from fighting over the same desktop.
- **Win11 selector differences.** Windows 11 ships WinUI3 rewrites of Notepad and Calculator. Their selectors differ from the classic Win32 versions. Always inspect with [Accessibility Insights for Windows](https://accessibilityinsights.io/) or FlaUI Inspect to get the correct selectors for your target system.
