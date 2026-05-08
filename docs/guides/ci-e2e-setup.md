# Running Flawright E2E Tests in CI

## The recommended pattern: ship a deterministic test target

The most reliable way to run E2E tests in CI is to **ship your own WPF (or WinForms) test application with the repo** and target it exclusively for the bulk of your test suite.

This eliminates every source of environment-driven flake:

- No dependency on UWP / Store apps that may not be installed on the runner.
- No `winget install` steps that can fail, hang, or return 0 while not actually registering the package.
- Fully deterministic behaviour on every machine — developer laptop, Windows Server 2025 runner, and Windows 11 Pro alike.

Flawright itself ships a controlled WPF test target at `tests/Flawright.E2ETests.TestApp/` and exercises it via `TestAppTests`. The pattern is documented below.

### Step 1: Create the test app project

Add a minimal WPF app to your repo under `tests/YourProject.E2ETests.TestApp/`:

```xml
<!-- YourProject.E2ETests.TestApp.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <OutputType>WinExe</OutputType>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
</Project>
```

Expose every Flawright action surface you want to test via controls with deterministic `AutomationProperties.AutomationId` values:

```xml
<!-- MainWindow.xaml (excerpt) -->
<Button x:Name="btnClick"
        Content="Click Me"
        AutomationProperties.AutomationId="btnClick"
        AutomationProperties.Name="Click Me"
        Click="BtnClick_Click" />

<TextBlock x:Name="lblOutput"
           AutomationProperties.AutomationId="lblOutput"
           Text="" />
```

### Step 2: Wire up deployment in the E2E test project

Add a `ProjectReference` and a custom target that copies the test app output alongside the test binaries:

```xml
<!-- YourProject.E2ETests.csproj -->
<ItemGroup>
  <ProjectReference Include="..\YourProject.E2ETests.TestApp\YourProject.E2ETests.TestApp.csproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
    <SkipGetTargetFrameworkProperties>true</SkipGetTargetFrameworkProperties>
  </ProjectReference>
</ItemGroup>

<Target Name="CopyTestAppOutput" AfterTargets="Build">
  <ItemGroup>
    <TestAppOutput Include="..\YourProject.E2ETests.TestApp\bin\$(Configuration)\net10.0-windows\**\*.*" />
  </ItemGroup>
  <Copy SourceFiles="@(TestAppOutput)"
        DestinationFolder="$(OutputPath)TestApp\%(RecursiveDir)"
        SkipUnchangedFiles="true" />
</Target>
```

### Step 3: Resolve the path in tests

```csharp
private static readonly string TestAppPath =
    Path.Combine(AppContext.BaseDirectory, "TestApp", "YourProject.E2ETests.TestApp.exe");
```

### Step 4: CI workflow

```yaml
- name: Build test app
  run: >
    dotnet build tests/YourProject.E2ETests.TestApp/YourProject.E2ETests.TestApp.csproj
    --configuration Release
    --no-restore
    /p:ContinuousIntegrationBuild=true

- name: Build E2E
  run: >
    dotnet build tests/YourProject.E2ETests/YourProject.E2ETests.csproj
    --configuration Release
    --no-restore
    /p:ContinuousIntegrationBuild=true

- name: Run E2E tests
  run: >
    dotnet test tests/YourProject.E2ETests/YourProject.E2ETests.csproj
    --configuration Release
    --no-build
    --logger "trx;LogFileName=e2e-results.trx"
    --results-directory ./TestResults
```

No `winget install` step. No package availability check. Tests that target the test app always run.

---

## Opt-in system-app tests with `RequiresAppFact`

For tests that genuinely need a system application (to validate Notepad-specific selectors, Calculator-specific UI, etc.) use `RequiresAppFactAttribute`. It extends `[Fact]` and skips the test at runtime — with a specific, human-readable reason — when the prerequisite is not met.

```csharp
// Skip when Calculator's AppX package is not installed:
[RequiresAppFact(Aumid = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App")]
public async Task Calculator_ClickButton()
{
    var fw = await Flawright.LaunchAsync(
        new LaunchOptions { ApplicationPath = "calc.exe" },
        new FlawrightOptions { InputMode = new VirtualInputMode() });
    // ...
}

// Skip when notepad.exe is not a real executable (or its AppX stub's package is absent):
[RequiresAppFact(ExePath = "notepad.exe")]
public async Task Notepad_TypeText()
{
    // ...
}
```

When the prerequisite is absent the test runner reports:

```
Skipped: AppX package for AUMID 'Microsoft.WindowsCalculator_8wekyb3d8bbwe!App' is
not installed on this machine. Install it with: winget install <package-id> (or
Add-AppxPackage). The test will run automatically once the package is available.
```

This is "most visible": the TRX and console output name the exact app, explain why it was skipped, and state how to fix it. No vague "environment check failed" messages.

For parameterised tests use `RequiresAppTheoryAttribute` (mirrors `[Theory]`).

### How `RequiresAppFact` checks prerequisites

| Property | Check |
|----------|-------|
| `Aumid` | Walks `HKCU\...\AppModel\Repository\Packages` for a key matching the PackageFamilyName extracted from the AUMID. |
| `ExePath` | Searches PATH; if the resolved path is inside `WindowsApps` (an AppExecutionAlias stub), additionally verifies the backing AppX package via the same registry walk. |

The logic is shared with `WindowsAumidResolver.IsPackageAumidInstalled` — a single registry-walk implementation in the production code.

---

## Why relying on system apps in CI is a flake source

`windows-latest` (Windows Server 2025 / 10.0.26100) is a **server SKU**.  Server SKUs do not ship UWP inbox apps:

- `Get-AppxPackage Microsoft.WindowsCalculator` returns nothing.
- `C:\Windows\System32\calc.exe` is a broker stub; when Calculator's AppX package is absent it launches and exits immediately (~200 ms) producing no window.
- `winget install Microsoft.WindowsCalculator` on Server 2025 consistently fails to find the package ID — the winget source index downloads but the Calculator package is not available for Server SKUs.

Attempting to work around this (different package IDs, `--source` flags, `Add-AppxPackage` with a sideload URL) is whack-a-mole: each workaround has its own failure mode, and the symptom changes across runner image updates.

**The correct answer is not a better `winget` invocation — it is a test target that is not a system app.**

---

## Understanding `FlawrightLaunchException`

When `ApplicationPath = "calc.exe"` is used on a machine where Calculator's AppX package is absent, Flawright maps the path to the Calculator AUMID via its known-alias table.  The AUMID launch path polls for a real Calculator process.  If the broker stub exits before a packaged-app process appears, Flawright throws `FlawrightLaunchException`:

```
Application 'calc.exe' launched but exited within 250ms with no main window.
This typically means the executable is an App Execution Alias stub for an
uninstalled UWP package on this machine.

To resolve:
- Install the target package on this machine.
- Or pass an explicit AUMID via LaunchOptions.Aumid.
- Or supply a custom IAumidResolver via LaunchOptions.AumidResolver.
```

With `RequiresAppFact`, this exception is never reached — the test skips before `InitializeAsync` is called.

---

## Related docs

- [Win11 Calculator guide](win11-calculator.md) — Calculator-specific selectors and worked examples
- [UWP / Store apps guide](uwp-store-apps.md) — AUMID-based launch for other UWP apps
- [Troubleshooting](../troubleshooting.md) — general diagnostic guidance
