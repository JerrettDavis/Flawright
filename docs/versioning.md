# Versioning & API Stability

## Current version status

Flawright is currently in the **pre-1.0 phase** (`0.x`). The public API is functional and used in production tests, but the project has not yet committed to strict semantic versioning across minor releases.

## Pre-1.0 policy (current)

During the `0.x` series:

- **Minor version bumps** (`0.2 → 0.3`) may include breaking API changes. Each breaking change is documented in [CHANGELOG.md](https://github.com/JerrettDavis/Flawright/blob/main/CHANGELOG.md) under the relevant version section.
- **Patch releases** within a minor version (`0.3.0 → 0.3.1`) contain only bug fixes — no breaking changes.
- **Deprecations** in `0.x` are marked `[Obsolete]` but may be removed in the next minor version without a separate removal-only release.

When upgrading between minor versions, check the CHANGELOG for any `### Changed` or `### Removed` entries that affect the public API surface.

## 1.0+ policy (planned)

From version 1.0 onward, Flawright will follow [Semantic Versioning 2.0.0](https://semver.org/):

- **Major** (`1.x → 2.0`): Breaking changes to the public API surface.
- **Minor** (`1.0 → 1.1`): Backward-compatible additions (new methods, new options properties, new exception types that extend existing ones).
- **Patch** (`1.0.0 → 1.0.1`): Backward-compatible bug fixes only.

## Public API surface

The public API is everything declared `public` in the following namespaces:

- `Flawright`
- `Flawright.Locator`
- `Flawright.Reqnroll` (companion package)

Everything marked `internal` — including backend implementations (`FlaUiApplicationLauncher`, `UiaElementBackend`, `FlaUiInputBackend`, `FlaUiApplicationHandle`, `AutoWait`, `SelectorParser`, `KeyParser`) — is **not** part of the public contract and may change or be removed in any release.

### Specifically included in the public contract

- All `public interface` types: `IFlawright`, `IFlawrightBrowser`, `IFlawrightPage`, `IFlawrightLocator`, `IFlawrightElement`, `IFlawrightAssertions`, `IFlawrightNotAssertions`, `IFlawrightPageAssertions`, `IFlawrightMouse`, `IFlawrightKeyboard`
- All `public record` / `public class` option types: `LaunchOptions`, `AttachOptions`, `FlawrightOptions`, and all `Locator*Options` records
- `FlawrightTimeoutException` and `AssertionException`
- `AriaRole` enum and `AssertionsStatic`
- Static entry points: `Flawright.LaunchAsync` and `Flawright.AttachAsync`
- `FlawrightReqnrollOptions` and all Reqnroll step bindings (companion package)

### Not part of the public contract

- Anything in a namespace containing `.Internal`, `.Backend`, `.Backends`, or `.Impl`
- Any type annotated `[ExcludeFromCodeCoverage]` that is also `internal`
- Test utilities and helpers in the `tests/` directory

## Deprecation policy

When a public API member is deprecated:

1. It is annotated with `[Obsolete("Use X instead. Will be removed in 0.N+1.", error: false)]`.
2. The CHANGELOG documents the deprecation and the replacement.
3. The member is removed no sooner than the *next minor* version (pre-1.0) or the *next major* version (post-1.0).

Example: if `FirstAsync()` is deprecated in `0.2.x`, it will be removed no sooner than `0.3.0`.

## LTS / older version support

Flawright does **not** currently maintain LTS releases or backport fixes to older minor versions. Users on older versions should upgrade to the latest release.

If upgrading is not immediately possible, open an issue on GitHub describing the constraint — backport consideration is evaluated case by case.

## How to read the version number

Flawright uses [Nerdbank.GitVersioning (NBGV)](https://github.com/dotnet/Nerdbank.GitVersioning) for version generation. The version follows the pattern:

```
0.MAJOR.MINOR[-buildhash]
```

- **0** — fixed pre-1.0 major
- **MAJOR** — set in `version.json` (currently `3`)
- **MINOR** — auto-incremented from the git commit height
- **-buildhash** — short git SHA appended in non-official builds (builds outside the `release` CI job)

Official NuGet releases (built by the `release` CI job on tag push) produce clean versions like `0.3.0`. Development builds from `dotnet build` locally produce versions like `0.3.42-gabcdef12`.

To check the version of an installed package:

```powershell
dotnet list package
```

Or inspect the assembly version attribute:

```csharp
var version = typeof(Flawright.Flawright).Assembly
    .GetName()
    .Version;
```

## Changelog

The full history of changes is in [CHANGELOG.md](https://github.com/JerrettDavis/Flawright/blob/main/CHANGELOG.md). Each release section lists:
- `### Added` — new public API
- `### Changed` — modified behavior or API (may include breaking changes in `0.x`)
- `### Fixed` — bug fixes
- `### Removed` — removed API

Read the `### Changed` and `### Removed` sections when upgrading.
