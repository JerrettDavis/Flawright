# Running Flawright E2E Tests in CI

This guide covers how to run Flawright E2E tests reliably on GitHub Actions `windows-latest` runners and explains the UWP app availability constraints you will encounter.

## The problem: UWP apps are not pre-installed on CI runners

GitHub Actions `windows-latest` (Windows Server 2025 / 10.0.26100) does **not** ship UWP / Store packaged apps such as Calculator, Notepad (Store version), or Paint by default.

Concretely:
- `Get-AppxPackage Microsoft.WindowsCalculator` returns nothing.
- `C:\Windows\System32\calc.exe` exists but is a broker stub that activates the UWP package via `IApplicationActivationManager`. When the package is absent the stub launches and exits immediately (~200 ms) without producing a main window.
- Flawright detects this scenario and throws `FlawrightLaunchException` with an actionable message rather than the cryptic `System.Exception: Process with an Id of N is not running` that FlaUI surfaces internally.

Classic Win32 Notepad (`C:\Windows\System32\notepad.exe`) **is** present as a real executable and does not require an extra install step.

## Installing UWP apps before running E2E tests

Add a `winget install` step immediately before your E2E test step. Example for Calculator:

```yaml
- name: Install UWP Calculator (windows-latest does not ship it)
  shell: pwsh
  run: |
    Write-Host "Installing Microsoft.WindowsCalculator via winget..."
    winget install --id Microsoft.WindowsCalculator --silent `
      --accept-source-agreements --accept-package-agreements `
      --disable-interactivity
    if ($LASTEXITCODE -ne 0) {
      Write-Error "winget install failed with exit code $LASTEXITCODE"
      exit 1
    }
    # Verify the install registered. AUMID resolver looks in HKCU; if that's
    # empty after install, the package may need explicit user-context registration.
    $pkg = Get-AppxPackage Microsoft.WindowsCalculator
    if (-not $pkg) {
      Write-Error "Microsoft.WindowsCalculator did not register for the current user after winget install."
      exit 1
    }
    Write-Host "Installed: $($pkg.PackageFullName) at $($pkg.InstallLocation)"

- name: Run E2E tests
  run: dotnet test tests/YourProject.E2ETests/YourProject.E2ETests.csproj ...
```

`--disable-interactivity` prevents winget from rendering progress bars or prompting in a non-TTY session, which can hang the runner.

The verification step (`Get-AppxPackage`) catches the uncommon case where winget exits 0 but the package is not yet registered for the current user context (can happen on some runner configurations).

## Other UWP apps

For apps other than Calculator, find the winget package ID with:

```powershell
winget search <app-name>
```

Common apps and their IDs:

| App | winget ID |
|-----|-----------|
| Calculator | `Microsoft.WindowsCalculator` |
| Paint | `Microsoft.Paint` |
| Notepad (Store version) | `Microsoft.WindowsNotepad` |

> **Note:** Classic Win32 Notepad (`C:\Windows\System32\notepad.exe`) is already present on CI runners — no install step needed for Notepad-based tests.

## Understanding FlawrightLaunchException

When you pass `ApplicationPath = "calc.exe"` on a machine where Calculator is not installed, Flawright's `WindowsAumidResolver` maps the path to the Calculator AUMID via the known-alias table. The AUMID launch path then polls for a real Calculator process. If the broker stub exits before a real packaged-app process appears, Flawright throws `FlawrightLaunchException`.

The exception message looks like:

```
Application 'calc.exe' launched but exited within 250ms with no main window.
This typically means the executable is an App Execution Alias stub for an
uninstalled UWP package on this machine.

To resolve:
- Install the target package on this machine (e.g. for Calculator:
  'winget install Microsoft.WindowsCalculator --silent').
- Or pass an explicit AUMID via LaunchOptions.Aumid.
- Or supply a custom IAumidResolver via LaunchOptions.AumidResolver
  for non-default app mappings.
```

If you see this in CI, add the appropriate `winget install` step as shown above.

## Related docs

- [Win11 Calculator guide](win11-calculator.md) — Calculator-specific selectors and worked examples
- [UWP / Store apps guide](uwp-store-apps.md) — AUMID-based launch for other UWP apps
- [Troubleshooting](../troubleshooting.md) — general diagnostic guidance
