---
_layout: landing
---

# Flawright

Playwright-style async API for FlaUI Windows desktop application automation and E2E testing.

## Overview

Flawright brings the Playwright developer experience to Windows desktop automation: fluent locators, selector strings, auto-waiting, and a full `Expect()` assertion chain — built on top of FlaUI and UI Automation (UIA3).

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = "notepad.exe"
});
var page = await fw.Browser.NewPageAsync();
await page.FillAsync("#RichEditBox", "Hello from Flawright!");
await page.Locator("#RichEditBox").Expect().ToBeVisibleAsync();
```

## Documentation

- [Getting Started](getting-started.md) — install, prerequisites, first test
- [Selectors](selectors.md) — full selector grammar and gotchas
- [Auto-waiting](auto-waiting.md) — how Flawright waits for elements
- [Assertions](assertions.md) — `Expect()` chain reference
- [Examples](examples.md) — worked examples: Calculator, Notepad, File Explorer, installer wizard
- [BDD with Reqnroll](bdd.md) — Gherkin/BDD testing with the `Flawright.Reqnroll` companion package
- [Troubleshooting](troubleshooting.md) — common failure modes and fixes
- [API Reference](api/index.md) — auto-generated from XML doc comments

## Quick links

- [NuGet: Flawright](https://www.nuget.org/packages/Flawright)
- [NuGet: Flawright.Reqnroll](https://www.nuget.org/packages/Flawright.Reqnroll)
- [GitHub repository](https://github.com/JerrettDavis/Flawright)
- [CHANGELOG](https://github.com/JerrettDavis/Flawright/blob/main/CHANGELOG.md)
