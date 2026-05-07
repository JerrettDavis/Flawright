# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Initial public scaffolding: `.gitignore`, `.editorconfig`, `.gitattributes`, `Directory.Build.props`, `Directory.Packages.props`, `version.json`
- Central Package Management (CPM) via `Directory.Packages.props`
- Nerdbank.GitVersioning for deterministic, git-tag-driven versioning with `version.json`
- Microsoft.SourceLink.GitHub for symbol server / debugger integration
- Meziantou.Analyzer for code-quality enforcement across all projects
- Playwright-flavored static entry point: `Flawright.LaunchAsync(LaunchOptions, FlawrightOptions?, CancellationToken)` and `AttachAsync` — a single call replaces the old two-step `CreateAsync` / `LaunchAsync` pattern
- `FlawrightBrowser` (`IFlawrightBrowser`): `NewPageAsync`, `GetAllPagesAsync`, `WaitForPageAsync`
- `FlawrightPage` (`IFlawrightPage`): `ClickAsync`, `FillAsync`, `TypeAsync`, `PressAsync`, `CheckAsync`, `UncheckAsync`, `SelectOptionAsync`, `WaitForSelectorAsync`, `ScreenshotAsync` (returns `byte[]`)
- `FlawrightLocator` (`IFlawrightLocator`): `FirstAsync`, `NthAsync`, `AllAsync`, `CountAsync`, `Filter`, `ClickAsync`, `FillAsync`, `Expect`
- `FlawrightElement` (`IFlawrightElement`): full element action surface including `TextAsync` (ValuePattern → TextPattern → Name fallback), `IsCheckedAsync`, `HoverAsync`, `FocusAsync`, `ScrollIntoViewIfNeededAsync`, `GetAttributeAsync`
- `FlawrightAssertions` / `FlawrightNotAssertions`: `ToBeVisibleAsync`, `ToBeHiddenAsync`, `ToBeEnabledAsync`, `ToBeDisabledAsync`, `ToHaveTextAsync`, `ToHaveValueAsync`, `ToBeCheckedAsync`, `ToHaveCountAsync`; full `Not.*` counterpart for every assertion
- `FlawrightOptions` record: `DefaultTimeout` (5 s), `DefaultRetryInterval` (100 ms), `ScreenshotDirectory`
- `FlawrightTimeoutException` (inherits `TimeoutException`) with `Selector` and `Timeout` properties
- `AssertionException` for count-assertion failures
- `SelectorParser` — full selector grammar: `#id`, `[attr=val]`, `name:`, `text:`, `automationid:`, `class:` / `classname:`, `role:` / `controltype:`, bare-string Name fallback; 35+ ControlType aliases; unknown prefix → `ArgumentException`; `xpath:` → `NotSupportedException`
- `KeyParser` — Playwright-style key/chord strings: single keys (`Enter`, `Escape`, `F1`–`F12`, A–Z, 0–9, navigation keys), modifier chords (`Ctrl+S`, `Ctrl+Shift+T`, `Alt+F4`)
- `AutoWait` internal polling loop: transient-exception swallowing, `CancellationToken` propagation, configurable timeout and retry interval
- `InternalsVisibleTo` for `JerrettDavis.Flawright.UnitTests` to enable white-box unit testing of `SelectorParser`, `KeyParser`, and `AutoWait`
- Unit tests: `SelectorParserTests` (35+ cases for `ParseControlType`, routing/exception paths), `KeyParserTests` (guard conditions, valid key/chord recognition), `AutoWaitTests` (first-try success, polling, timeout, cancellation, transient-exception recovery), `FlawrightTimeoutExceptionTests`, `AssertionExceptionTests`, `FlawrightOptionsTests`
- E2E tests (Notepad, Calculator) converted from `IDisposable` to `IAsyncLifetime` for reliable process cleanup
- Pinned `System.Drawing.Common` to 9.0.0 to avoid CVE in the transitive FlaUI dependency on 5.0.2
- GitHub Actions CI: `pr-checks` workflow (restore → format-verify → build → unit-test) and `release` workflow (NBGV pack → NuGet + GitHub Packages push)
- Full documentation suite: index, getting-started, selectors, auto-waiting, assertions, examples, troubleshooting — all updated to the Wave 2A API surface

### Changed

- Entry point simplified from two-step `CreateAsync` + `LaunchAsync` pattern to single static `Flawright.LaunchAsync(LaunchOptions, FlawrightOptions?)`
- `LaunchOptions.Path` renamed to `LaunchOptions.ApplicationPath` for clarity
- `ScreenshotAsync` now returns `byte[]` (was `void` / file-only in earlier scaffolding)
