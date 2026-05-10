# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.5.0] - 2026-05-08

### Added

- Multi-window APIs on `IFlawrightBrowser`: `NewPageAsync`, `GetAllPagesAsync`, and `WaitForPageAsync(title)` for waiting until a named top-level window appears (`423946a`).
- `Flawright.AttachAsync` validated end-to-end with attach-aware dispose; new `_wasAttached` flag in `FlawrightBrowser` short-circuits process termination on dispose for attached browsers (`f7aab35`).
- `LocatorGetByRoleOptions.NameRegex` now properly wired — regex wins over `Name` when both are set (`f7aab35`).
- `FlawrightOptions.ScreenshotDirectory` honored: when no explicit path is given, screenshots are written to this directory with auto-generated `screenshot-{timestamp}-{guid}.{ext}` filenames; both `FlawrightPage.ScreenshotAsync` and `FlawrightLocator.ScreenshotAsync` create the directory if it is missing (`f7aab35`, `0cd3589`).
- `LocatorFilterOptions.HasName` and `HasNameRegex` — name-only filters that bypass the value/document fallback chain (`0cd3589`).
- `LaunchOptions.LaunchReadyTimeout` — bounds the new `ProcessReadyGuard` wait introduced to fix a CI launch race (`aafeea2`).
- `Flawright.Reqnroll` companion package validated end-to-end: `FlawrightSteps` (25 step bindings), `FlawrightReqnrollHooks`, `FlawrightReqnrollOptions`, tag conventions `@launch:`, `@aumid:`, `@attach:`, `@attachpid:`; two sample projects `Flawright.Reqnroll.NotepadDemo` and `Flawright.Reqnroll.CalculatorDemo` wired into CI (`d10c373`, `73301fa`).

### Fixed

- E2E launch-path race on busy CI runners: `FlaUI.EnumProcessModules` could fail with `Win32Exception` error 299. New `ProcessReadyGuard` waits for `Process.WaitForInputIdle` then polls `Process.Modules` until enumeration succeeds (`aafeea2`).
- E2E flake on deep locator chains (`TestAppLocatorChainTests.Locator_ThreeLevelChain_ResolvesNestedButtons`): added a `WaitForAsync` gate to allow the nested WPF tree to materialize before chain navigation (`cf8ee47`).
- Attached scenarios in Reqnroll teardown no longer send `WM_CLOSE` to the attached process; teardown skips `CloseAsync` entirely when the scenario was attached, and `_wasAttached` in dispose guards the rest (`182adc1`).
- `Reqnroll.json` BDD samples now include the required `bindingAssemblies` entry; docs updated to match (`73301fa`, `aee242a`).
- Coverage gate restored to ≥ 90% with focused unit tests for new code paths (`cf8ee47`, `0510a6a`).

### Changed

- `ProcessReadyGuard.DefaultModulesProbe` narrows exception catch — no longer masks unexpected errors beyond the known `Win32Exception` race (`0cd3589`).
- `LaunchApp_BothPathAndAumidSet_ThrowsArgumentException` test made truly async (was passing for the wrong reason) (`0cd3589`).
- TagParser redundant guard removed; `FlawrightLocator` selector interpolation made explicit (`182adc1`).
- `dotnet format` cleanup: line endings and naming convention fixes across the codebase (`b3b5068`).

[0.5.0]: https://github.com/JerrettDavis/Flawright/compare/v0.4.33...v0.5.0

## [0.4.0] - 2026-05-07

### Breaking Changes

- All `JerrettDavis.Flawright*` namespaces, assembly names, and project directory paths have been renamed to `Flawright*` to match the published NuGet package identity (`Flawright`, `Flawright.Reqnroll`).

**Source-level migration for library consumers:**

| Old | New |
|-----|-----|
| `using JerrettDavis.Flawright;` | `using Flawright;` |
| `using JerrettDavis.Flawright.Locator;` | `using Flawright.Locator;` |
| `using JerrettDavis.Flawright.CloseBehaviors;` | `using Flawright.CloseBehaviors;` |
| `using JerrettDavis.Flawright.InputModes;` | `using Flawright.InputModes;` |
| `using JerrettDavis.Flawright.Reqnroll;` | `using Flawright.Reqnroll;` |

**NuGet package IDs are unchanged** (`Flawright`, `Flawright.Reqnroll`) — no package reference updates required.

## [0.3.0] - 2026-05-07

### Added
- Per-app-type guides under `docs/guides/`: Win11 Notepad, Win11 Calculator, classic Win32, WinForms, WPF, WinUI 3, UWP/Store apps, File Explorer, multi-window apps, installer wizards, elevated/admin apps.
- `docs/versioning.md` — API stability & semver policy.
- `docs/performance.md` — performance guidance and threading model.
- Expanded `docs/troubleshooting.md` with Win11-specific scenarios and integrity-level mismatch (UAC) guidance.
- New companion package: `Flawright.Reqnroll` for Reqnroll/Gherkin BDD testing.
  - 25 built-in step bindings covering click/fill/type/keyboard/check/select/wait/assert.
  - Tag-driven scenario configuration (`@launch:`, `@aumid:`, `@attach:`, `@attachpid:`).
  - BoDi-based DI; `IFlawright`/`IFlawrightBrowser`/`IFlawrightPage` injectable into custom bindings.
  - Per-scenario lifecycle (fresh app instance per scenario).
- Sample projects under `samples/Flawright.Reqnroll.NotepadDemo/` and `samples/Flawright.Reqnroll.CalculatorDemo/` demonstrating end-to-end Gherkin-driven tests.
- `docs/bdd.md` — full BDD documentation including tag reference, step library, and DI patterns.
- Codecov integration: `codecov.yml` config, `codecov/codecov-action@v6` upload in CI (both `pr-checks` and `release` jobs), badge in README.
- GitHub issue templates (`bug_report`, `feature_request`, `config`) in `.github/ISSUE_TEMPLATE/`.
- Pull request template (`.github/pull_request_template.md`) with Flawright-specific checklist.
- `CODEOWNERS` file assigning `@JerrettDavis` to all paths.
- `labeler.yml` workflow + `.github/labeler.yml` auto-labeler config (paths to labels, plus PR size labels via `codelytv/pr-size-labeler`).
- `pr-validation.yml` workflow with dry-run pack and per-test PR check via `EnricoMi/publish-unit-test-result-action@v2`.
- `global.json` SDK pin (`10.0.107`, `rollForward: latestFeature`).
- `PackageReleaseNotes` property in `JerrettDavis.Flawright.csproj` pointing to CHANGELOG.
- DocFX-generated documentation site published to GitHub Pages at https://jerrettdavis.github.io/Flawright/
- API reference auto-generated from XML doc comments via DocFX metadata extraction.
- New `docs.yml` workflow with `validate-docs` (PRs — builds site and uploads preview artifact) and `publish-docs` (push to main / workflow_dispatch — deploys to GitHub Pages) jobs.
- Documentation badge in README linking to the live site.

### Changed
- README badge row reordered to match the house-style ceiling row (NuGet | Downloads | CI | CodeQL | Codecov | License | .NET). Added `<!-- DOCS_BADGE_PLACEHOLDER -->` for Wave 3-Bravo.
- Pinned `dotnet/nbgv` to commit SHA `b944774b6878ef950cc14d1a72bf9c0ffafbb839` in `ci.yml` (was `@master`).
- Bumped repo version to `0.3` in `version.json`.

## [0.2.14] - 2026-05-07

### Changed
- Application launching is now fully async end-to-end. `IApplicationLauncher.{Launch,LaunchStoreApp,Attach,AttachByName}` (internal) return `Task<IApplicationHandle>`. The chain `FlawrightBrowser.EnsureInitializedAsync` → `LaunchApp` → `LaunchStoreApp` → `WaitForPackagedAppProcess` no longer blocks the calling thread for up to 5 seconds during packaged-app launch; the entire blocking section now runs inside a single `Task.Run` and the process-poll loop uses `await Task.Delay`.
- `FlawrightLocator.Filter(LocatorFilterOptions { Has, HasNot })` now properly awaits the inner-locator count instead of using `GetAwaiter().GetResult()`. Removes a latent deadlock risk for external `IFlawrightLocator` implementations.

### Fixed
- `ScreenshotAsync` now uses `File.WriteAllBytesAsync` and properly observes the cancellation token (the method is still a stub for the file payload — Wave D will add real screenshot capture).

### Cosmetic
- Removed pointless `await Task.CompletedTask` tail-awaits in several locator action methods (`TypeAsync`, `PressSequentiallyAsync`, `PressAsync`, `FocusAsync`, `BlurAsync`, `DragToAsync`).

## [0.2.12] - 2026-05-07

### Fixed
- Win11 packaged-app launches no longer fail with "Process with an Id of N is not running" during `WaitWhileMainHandleIsMissing`. After `Application.LaunchStoreApp` returns, the launcher now polls for the actual app process (matched by package family name under `C:\Program Files\WindowsApps\<PFN>_*\`) and re-attaches FlaUI's tracking to that live PID. The activator/broker that `IApplicationActivationManager::ActivateApplication` returns is correctly recognized as a transient and replaced by the long-running app process.
- `AppExecutionAliasResolver.TryResolve` now uses a fast path that checks for the existence of the WindowsApps alias stub (e.g. `%LOCALAPPDATA%\Microsoft\WindowsApps\notepad.exe`) regardless of PATH order. Previously, if `C:\Windows\System32\notepad.exe` appeared before the WindowsApps directory on `%PATH%`, the resolver would find it first, see it wasn't inside WindowsApps, and fall back to `AttachOrLaunch` — causing the same crash on Windows 11 systems with the default PATH. Both `notepad.exe`, `calc.exe`, and `mspaint.exe` are covered.

## [0.2.9] - 2026-05-07

### Added
- Transparent AppExecutionAlias auto-resolution: `LaunchOptions.ApplicationPath = "notepad.exe"` on Windows 11 now automatically launches the packaged Notepad via `LaunchStoreApp("Microsoft.WindowsNotepad_8wekyb3d8bbwe!App")`, eliminating the need to manually specify `Aumid` and preventing FlaUI from binding to the dead alias-stub process.
- New internal `AppExecutionAliasResolver` with a curated lookup for the most common Windows-shipped packaged-app aliases (`notepad.exe`, `calc.exe`, `mspaint.exe`).
- New internal `SafeProcessQueries` helper that wraps `HasExited` in a tolerant try/catch; covered by unit tests that document the contract.

### Fixed
- `FlaUiApplicationHandle.HasExited` no longer throws `InvalidOperationException` when the underlying `Process` handle has been disposed or was never associated with a real process — it now returns `true` (the safe default for dispose paths). Fixes `InvalidOperationException: No process is associated with this object.` on second run.
- README and `/docs` Notepad examples updated to use selectors that work on Windows 11 Notepad (WinUI3 `#RichEditBox` AutomationId), with a note documenting the Win10 classic Notepad difference (`controltype:Edit`).
- `ReadmeQuickstartTests.cs` compile-time snapshots updated to match the new Win11-friendly selectors so CI continues to catch API drift.

## [0.2.8] - 2026-05-07

### Added
- `ScreenshotAsync(string path, CancellationToken ct = default)` convenience overload on `IFlawrightLocator` and `IFlawrightPage` — matches the canonical README quickstart example and removes the friction of constructing a `LocatorScreenshotOptions` for the common save-to-path case.

### Fixed
- README and `/docs` code samples updated to compile against the v0.2 API after the Phase 2 rewrite: `FirstAsync()` → sync `.First`, `NthAsync(n)` → sync `.Nth(n)`, `Filter(lambda)` → `Filter(new LocatorFilterOptions { HasText = ... })`.
- Added `ReadmeQuickstartTests.cs` with compile-time snapshots of every code sample from `README.md` and `/docs/*.md`, preventing future API drift from going undetected in CI.

## [0.2.0] - 2026-05-07

### Added

#### Backend Abstraction
- `IElementBackend` — seam for the UI-Automation element tree; enables unit testing without a real UIA session
- `IApplicationLauncher` — seam for launching and attaching to processes; `FlaUiApplicationLauncher` is the production implementation
- `IInputBackend` — seam for keyboard/mouse dispatch; `FlaUiInputBackend` is the production implementation
- `IConditionTranslator` — seam for translating parsed selector tokens to UIA property conditions

#### Modern Windows App Support
- `LaunchOptions.Aumid` — launch packaged (UWP / WinUI 3 / store) apps by Application User Model ID
- `AttachOptions.ProcessName` — attach to a running process by basename (with or without `.exe`)
- `AttachOptions.Index` — disambiguate among multiple instances of the same process (zero-based, ordered by PID ascending)

#### Locator API — Playwright .NET Parity
- `IFlawrightLocator.First` / `Last` / `Nth(int)` — synchronous index-based narrowing, returning `IFlawrightLocator` (replaces the removed `FirstAsync`/`NthAsync`)
- `IFlawrightLocator.Locator(string)` — chained sub-locator for composing selectors
- `IFlawrightLocator.GetByRole(AriaRole, LocatorGetByRoleOptions?)` — locate by ARIA role with optional name/state filters
- `IFlawrightLocator.GetByLabel(string, LocatorGetByLabelOptions?)` — locate by accessible label (exact or contains)
- `IFlawrightLocator.GetByText(string, LocatorGetByTextOptions?)` — locate by visible text (exact or contains)
- `IFlawrightLocator.GetByTestId(string)` — locate by `data-testid` / AutomationId
- `IFlawrightLocator.GetByPlaceholder(string, LocatorGetByPlaceholderOptions?)` — locate by placeholder text
- `IFlawrightLocator.GetByTitle(string, LocatorGetByTitleOptions?)` — locate by title attribute
- `IFlawrightLocator.Filter(LocatorFilterOptions)` — narrow a locator set with `Has`, `HasNot`, `HasText`, `HasTextRegex`, `HasNotText`, `HasNotTextRegex`, `Visible` predicates
- `IFlawrightLocator.And(IFlawrightLocator)` — intersect two locators (AND composition)
- `IFlawrightLocator.Or(IFlawrightLocator)` — union two locators (OR composition)

#### Locator Action Surface
- `PressAsync(string, LocatorPressOptions?)` — send a Playwright-style key chord
- `TypeAsync(string, LocatorTypeOptions?)` — type text character-by-character
- `PressSequentiallyAsync(string, LocatorPressSequentiallyOptions?)` — type with per-character delay
- `CheckAsync(LocatorCheckOptions?)` / `UncheckAsync(LocatorUncheckOptions?)` / `SetCheckedAsync(bool, LocatorSetCheckedOptions?)` — checkbox control
- `HoverAsync(LocatorHoverOptions?)` — move the cursor over the element
- `FocusAsync()` / `BlurAsync()` — focus and blur the element
- `DragToAsync(IFlawrightLocator, LocatorDragToOptions?)` — drag-and-drop to a target locator
- `SelectOptionAsync(string[], LocatorSelectOptionOptions?)` — set selected items in a list control
- `ClearAsync(LocatorClearOptions?)` — clear an editable field
- `ScrollIntoViewIfNeededAsync()` — scroll the element into the visible viewport
- `HighlightAsync()` — visually highlight the element for debugging

#### Locator Read Surface
- `IsVisibleAsync(CancellationToken)` / `IsHiddenAsync(CancellationToken)` — visibility checks
- `IsEnabledAsync(CancellationToken)` / `IsDisabledAsync(CancellationToken)` — enabled-state checks
- `IsCheckedAsync(CancellationToken)` / `IsEditableAsync(CancellationToken)` — state checks
- `InnerTextAsync(CancellationToken)` / `TextContentAsync(CancellationToken)` — text extraction
- `InputValueAsync(CancellationToken)` — read the current value of an input control
- `GetAttributeAsync(string, CancellationToken)` — read a named UIA property or custom attribute
- `BoundingBoxAsync(CancellationToken)` — get the element's bounding rectangle

#### Wait API
- `WaitForAsync(LocatorWaitForOptions?)` — poll until the element reaches a target `WaitForState` (`Visible`, `Hidden`, `Attached`, `Detached`)

#### Full Assertion Surface (`FlawrightAssertions` / `FlawrightNotAssertions`)
- `ToBeFocusedAsync` — assert the element has keyboard focus
- `ToBeAttachedAsync` — assert the element is present in the element tree
- `ToBeEditableAsync` — assert the element is editable
- `ToBeEmptyAsync` — assert the element has no text content
- `ToContainTextAsync(string/Regex, AssertionsToContainTextOptions?)` — assert text is a substring
- `ToHaveAttributeAsync(string, string/Regex, AssertionsToHaveAttributeOptions?)` — assert a UIA attribute value
- `ToHaveIdAsync(string/Regex, AssertionsToHaveIdOptions?)` — assert AutomationId
- `ToHaveClassAsync(string/Regex, AssertionsToHaveClassOptions?)` — assert ClassName/class property
- `ToHaveRoleAsync(AriaRole, AssertionsToHaveRoleOptions?)` — assert the element's ARIA role
- `ToHaveAccessibleNameAsync(string/Regex, AssertionsToHaveAccessibleNameOptions?)` — assert accessible name
- Regex overloads on `ToHaveTextAsync` and `ToHaveValueAsync`
- Auto-wait `ToHaveCountAsync(int, AssertionsToHaveCountOptions?)` — polls until count matches
- Full `.Not.*` counterpart for every assertion above via `IFlawrightNotAssertions`

#### Page Assertions (`IFlawrightPageAssertions`)
- `IFlawrightPageAssertions` interface with `ToHaveTitleAsync(string, PageAssertionsToHaveTitleOptions?)` and `ToHaveTitleAsync(Regex, PageAssertionsToHaveTitleOptions?)`
- `FlawrightPageAssertions` concrete implementation
- `.Not` property returning `IFlawrightPageAssertions` with negated semantics

#### Static Assertions Entry Point
- `AssertionsStatic.Expect(IFlawrightLocator)` — returns `IFlawrightAssertions` bound to the locator
- `AssertionsStatic.Expect(IFlawrightPage)` — returns `IFlawrightPageAssertions` bound to the page

#### Mouse and Keyboard Sub-APIs
- `IFlawrightMouse` on `IFlawrightPage.Mouse` — `MoveAsync`, `ClickAsync`, `DoubleClickAsync`, `DownAsync`, `UpAsync`, `WheelAsync`
- `IFlawrightKeyboard` on `IFlawrightPage.Keyboard` — `PressAsync`, `TypeAsync`, `DownAsync`, `UpAsync`, `InsertTextAsync`

#### AriaRole Enum and Mapper
- `AriaRole` enum with 74 values covering the full ARIA 1.2 role taxonomy
- `AriaRoleMapper` — maps 47 roles to UIA `ControlType` values; 35 web-only roles throw `NotSupportedException` with a descriptive message (no silent fallback to `Custom`)

#### Selector Grammar Extensions
- `>>` combinator for direct child-of-locator composition
- Substring attribute operators: `[name*=value]` (contains), `[name^=value]` (starts-with), `[name$=value]` (ends-with), `[name~=value]` (word-in-list)
- Quote-aware tokenizer: attribute values may be single- or double-quoted

#### Infrastructure
- Unit test coverage gate at 90% line rate (enabled in CI via `irongut/CodeCoverageSummary@v1.3.0`)
- Coverage measured by coverlet with `ExcludeByAttribute=ExcludeFromCodeCoverageAttribute`
- All option/DTO record types annotated `[ExcludeFromCodeCoverage]` (no business logic)

### Changed

- `IFlawrightLocator.First` / `Last` / `Nth(int)` are now synchronous properties/methods returning `IFlawrightLocator` instead of `Task<IFlawrightElement>` — matches Playwright .NET v1.40+ API shape
- `SelectorParser` extended with substring attribute operators and `>>` combinator; unknown prefix now throws `ArgumentException` (was silently ignored in v0.1)
- `AriaRoleMapper` web-only roles now throw `NotSupportedException` instead of mapping to `ControlType.Custom`
- `FlawrightBrowser.NewPageAsync` — resolves the application's root element via `IApplicationLauncher`, delegating all UIA access through the backend seam
- `FlawrightOptions.DefaultTimeout` default lowered from 30 s to 5 s for faster test feedback
- Internal `AutoWait` loop now propagates `OperationCanceledException` immediately (no retry on cancellation)
- All source files use `CRLF` line endings to match `.editorconfig` and `.gitattributes` settings

### Removed

- `LaunchOptions.ForceAsyncio` — was never used; removed without replacement
- `IFlawrightLocator.FirstAsync` — replaced by synchronous `First` property
- `IFlawrightLocator.NthAsync(int)` — replaced by synchronous `Nth(int)` method
- Parameterless `ClickAsync()` / `FillAsync()` overloads on `IFlawrightElement` and `IFlawrightLocator` (callers must supply a value/options explicitly)

### Fixed

- `LaunchOptions.ApplicationPath` and `Aumid` are now validated as mutually exclusive at browser-init time; setting both throws `ArgumentException`
- `LaunchOptions.WorkingDirectory` combined with `Aumid` now throws `ArgumentException` (was silently ignored)
- `WaitWhileMainHandleIsMissing = false` now throws `FlawrightTimeoutException` when the handle does not appear, instead of returning a null element
- Store-app `Dispose` no longer kills the process tree (correct behaviour: the packaged app manages its own lifecycle)
- `MA0009` (Regex without timeout) — all `new Regex(...)` calls in test files now supply `TimeSpan.FromSeconds(1)` as the third argument
- `CA1859` (return concrete type for performance) — test helper methods now declare concrete return types

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
