# Troubleshooting

## "Locator not found" (`FlawrightTimeoutException`)

Locator actions (such as `ClickAsync`, `FillAsync`, `WaitForAsync`, and assertions) throw `FlawrightTimeoutException` when an element is not found within the configured timeout.

**Check the selector.** Open [Accessibility Insights](https://accessibilityinsights.io/) or `inspect.exe` and verify the element exists and that its Name / AutomationId / ControlType matches your selector exactly. Names are case-sensitive.

**Check the timing.** The element may not yet be in the UIA tree when the call starts. The auto-waiting loop retries every 100ms (default) for up to 5 seconds (default). If your application is slow to render, increase the timeout:

```csharp
using JerrettDavis.Flawright.Locator; // for LocatorWaitForOptions

await page.Locator("#myButton").WaitForAsync(
    new LocatorWaitForOptions { Timeout = TimeSpan.FromSeconds(15) });
```

**Check the root.** `NewPageAsync` uses `GetMainWindow`, which returns the *main* window of the process. If the element lives in a secondary window (dialog, MDI child, context menu), use `WaitForPageAsync` to get a page for that window first:

```csharp
var dialog = await fw.Browser.WaitForPageAsync("Save As");
var fileNameBox = dialog.Locator("controltype:Edit");
await fileNameBox.Expect().ToBeVisibleAsync();
```

**Check virtualization.** ListView and TreeView controls with virtualization may not expose offscreen items in the UIA tree. Scroll the control to bring items into view before searching.

---

## "Unknown locator prefix" (`ArgumentException`)

The selector uses a prefix that `SelectorParser` does not recognize (e.g., `foo:bar`). Supported prefixes: `name:`, `text:`, `automationid:`, `class:`, `classname:`, `role:`, `controltype:`, `#`. See [Selectors](selectors.md) for the full grammar.

---

## High DPI issues

On displays with scaling above 100%, click coordinates synthesized by FlaUI may be off by a scale factor. Symptoms: clicks land in the wrong place or miss entirely.

**Check your process DPI awareness.** Add a `<dpiAware>` manifest entry or call `SetProcessDpiAwarenessContext` before launching the test runner. For .NET test projects, the simplest fix is to add an `app.manifest`:

```xml
<asmv3:application xmlns:asmv3="urn:schemas-microsoft-com:asm.v3">
  <asmv3:windowsSettings>
    <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">True/PM</dpiAware>
    <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
  </asmv3:windowsSettings>
</asmv3:application>
```

Reference it from your `.csproj`:

```xml
<ApplicationManifest>app.manifest</ApplicationManifest>
```

---

## Permission elevation (UAC)

**Symptom:** `Application.AttachOrLaunch` succeeds but `GetMainWindow` returns null or throws, or FlaUI cannot enumerate the window's children.

**Cause:** The application runs at a higher integrity level (elevated) than the test process. UIA's security model blocks a lower-integrity process from automating a higher-integrity window.

**Fix:** Run the test runner as administrator, or use a test agent that already runs elevated. In CI, configure the runner process to run under an administrator account. In local development, launch your IDE as administrator.

**Alternative:** If you control the application under test, remove the elevation requirement from its manifest for test builds.

---

## Running in CI

UI Automation requires a live interactive desktop session. A headless CI runner (no display, service account without a session) will fail.

**Options:**

- **Windows VM with auto-login:** Provision a Windows VM, enable automatic logon, and configure a self-hosted GitHub Actions runner as a service that starts in the user session. The desktop is always available.
- **Interactive Windows runner:** GitHub-hosted `windows-latest` runners have a desktop session available. Standard FlaUI / Flawright tests work on these runners without additional setup.
- **RDP with locked session workaround:** A locked (but logged-in) RDP session keeps the desktop session alive. Some teams use this on bare-metal CI boxes.

**Things that will not work:**

- Running under a service account with no interactive session
- Docker containers (even Windows containers do not expose a real desktop session to UI Automation)
- `RunAs` with a non-interactive account

---

## AppContainer apps (UWP / WinUI3)

UWP and sandboxed WinUI3 apps run in an AppContainer with restricted UIA access. Symptoms include:

- The application window appears in the UIA tree but children are inaccessible.
- `FindFirstDescendant` returns null even when the element is visible.
- Access is denied enumerating automation properties.

**Cause:** AppContainer apps require the test process to have the `uiAccess=true` attribute in its manifest and the process must be launched from a trusted location (e.g., `Program Files`). This is a Windows security requirement, not a Flawright limitation.

**Workaround for testing:** Use a signed test runner with `uiAccess=true` and place it in a trusted location, or test the app in a non-AppContainer packaging mode during development. The [FlaUI documentation](https://github.com/FlaUI/FlaUI) has more detail on UIAccess requirements.

---

## Flaky tests

**Symptom:** Tests pass locally but fail intermittently in CI, or pass on one machine and fail on another.

**Root causes and fixes:**

| Cause | Fix |
|---|---|
| Element appears after a delay | Increase the per-call or global timeout; the auto-wait loop handles most cases |
| Focus stolen by another window | Bring the application window to the foreground before clicking; avoid running other interactive processes during the test run |
| Control name changes on state | Use AutomationId instead of Name for volatile controls |
| Machine performance variance | Increase `DefaultTimeout` in `FlawrightOptions` for slow environments |
| DPI scaling | See High DPI section above |

**Isolation:** Run UI automation tests in their own test assembly, separate from unit tests. Configure the test runner to execute them sequentially (`xunit.runner.json` → `"parallelizeAssembly": false`). Parallel UI tests fight over keyboard focus and window Z-order.

---

## Leaked processes

If a test crashes before `DisposeAsync` is called, the launched application may keep running. Use `IAsyncLifetime` (xUnit) to ensure cleanup always fires:

```csharp
public class MyTests : IAsyncLifetime
{
    private Flawright? _fw;

    public async Task InitializeAsync()
    {
        _fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"
        });
    }

    public async Task DisposeAsync()
    {
        if (_fw != null)
            await _fw.DisposeAsync();
    }

    [Fact]
    public async Task MyTest()
    {
        var page = await _fw!.Browser.NewPageAsync();
        // ...
    }
}
```

`Flawright.DisposeAsync()` calls `Close()` on the process and then `Kill(entireProcessTree: true)` if the process has not yet exited — so even hard crashes in your app will not leave zombie processes.

---

## FlaUI Inspect vs. Accessibility Insights

Both tools browse the UIA tree, but they show slightly different views:

- **inspect.exe** (`C:\Program Files (x86)\Windows Kits\10\bin\<version>\x64\inspect.exe`) is low-level and shows raw UIA properties including automation patterns. Good for diagnosing why a click or fill is not working.
- **Accessibility Insights** is higher-level and formats properties for readability. Easier to navigate for finding AutomationIds and Names.

Use Accessibility Insights first; drop down to `inspect.exe` when you need the automation pattern details.

---

## "Process with an Id of N is not running"

**Symptom:** Test fails with `InvalidOperationException: Process with an Id of N is not running` during `WaitWhileMainHandleIsMissing` after launching a packaged (WinUI3) app via `notepad.exe`, `calc.exe`, or another AppExecutionAlias.

**Cause:** On Windows 11, `ApplicationPath = "notepad.exe"` starts an activator/broker stub process that immediately exits after activating the real packaged app. Earlier Flawright versions tracked the broker PID, which became invalid before the main window appeared.

**Fix:** Upgrade to Flawright **≥ 0.2.12**. The launcher now polls for the real packaged-app process (matched by package family name under `C:\Program Files\WindowsApps\`) and re-attaches FlaUI tracking to the live PID. No code changes needed.

---

## "Cannot find process with id" during dispose

**Symptom:** `DisposeAsync` throws `InvalidOperationException: No process is associated with this object` or `Cannot find process with id` when cleaning up after a test.

**Cause:** The `HasExited` check on the underlying `Process` object threw when the process handle was already disposed or was never associated with a real process (e.g., after attaching to a packaged app whose activation stub exited).

**Fix:** Upgrade to Flawright **≥ 0.2.9**. The `SafeProcessQueries` helper wraps `HasExited` in a try/catch that returns `true` (safe default for dispose paths) instead of propagating the exception.

---

## "Locator not found within Ns" — diagnosing with Inspect

When `FlawrightTimeoutException` occurs and you cannot figure out why the element is not found:

1. **Open Accessibility Insights for Windows** (or `inspect.exe`) while the application is running.
2. Hover over the element you expect to find — Accessibility Insights highlights it and shows its UIA properties.
3. Compare the `AutomationId`, `Name`, and `ControlType` shown in the tool against your selector.
4. Common mismatches:
   - Element has AutomationId `""` (empty) — use `name:` or `controltype:` instead.
   - Name is localized — your locale shows a different string than the selector.
   - Element is inside a virtualized container and is not yet in the tree — scroll the container into view.
5. Increase timeout as a diagnostic step: `WaitForAsync(new LocatorWaitForOptions { Timeout = TimeSpan.FromSeconds(30) })`. If it still fails after 30 seconds, the element is structurally absent, not just slow.

---

## Win10 vs Win11 selector mismatch

Several inbox Windows apps expose a different UIA tree on Windows 10 vs Windows 11 because Microsoft replaced them with WinUI3 packaged versions.

| App | Win10 selector | Win11 selector |
|---|---|---|
| Notepad editor | `controltype:Edit` | `[name="Text editor"]` (tabbed) or `#RichEditBox` |
| Calculator buttons | `#1`, `#plus`, `#equals` | `#num1Button`, `#plusButton`, `#equalButton` |

If tests pass locally (Win11) but fail on a Win10 CI runner or vice versa, the selector has an OS-version mismatch. Solutions:
- Use a version-agnostic selector where possible (`name:OK` works on both if the button text is the same).
- Branch the selector based on the OS version detected at runtime.
- Run tests only on the target OS they are written for.

See the [Win11 Notepad guide](guides/win11-notepad.md) and [Win11 Calculator guide](guides/win11-calculator.md) for the specific selectors for each version.

---

## AppContainer / sandbox attach failures

**Symptom:** Flawright finds the application window but all `Locator` calls return nothing, or `FindFirstDescendant` returns null even though the element is visible.

**Cause:** The app runs in an AppContainer (UWP or sandboxed WinUI3). The UIA cross-process boundary requires the test process to have `uiAccess=true` in its manifest and to run from a trusted path.

**Workaround:**
- Sign the test runner executable.
- Place it in a trusted location (e.g., `C:\Program Files\MyTestRunner\`).
- Add `<uiAccess>true</uiAccess>` to its application manifest.

This is a Windows security requirement. See [Troubleshooting — AppContainer apps](#appcontainer-apps-uwp--winui3) above for the full detail, and [UWP / Store apps guide](guides/uwp-store-apps.md).

---

## UAC integrity-level mismatch (elevated app + non-elevated test)

**Symptom:** Test process starts the elevated app, `NewPageAsync()` returns a page, but all locator actions time out. Or `GetAllPagesAsync()` finds a window with the right title but its element tree is empty.

**Cause:** The app runs at high integrity (administrator) and the test process runs at medium integrity. Windows blocks cross-integrity-level UIA access.

**Fix:** Run the test process elevated. See [Elevated apps guide](guides/elevated-apps.md) for options.

---

## Coverage gate failing in CI but passing locally

**Symptom:** The CI coverage check fails (e.g., `irongut/CodeCoverageSummary` reports < 90%) but running `dotnet test` locally shows > 90%.

**Root causes:**

- **Test ordering:** Coverage results depend on which tests run. If some tests are filtered out in CI (e.g., E2E tests skipped because there is no desktop session), the denominator changes.
- **Parallel vs sequential execution:** Local machines may run tests in a different order, causing different code paths to be hit.
- **Platform-specific exclusions:** Some code is compiled conditionally (`#if WINDOWS`) and may count differently in CI depending on the TFM.
- **Coverage tool version mismatch:** Different versions of `coverlet` or `dotnet-coverage` may count lines differently.

**Fixes:**
- Confirm CI and local use the same test filter (`--filter "Category!=E2E"` or similar).
- Check `coverlet` and `dotnet-coverage` version alignment in `Directory.Packages.props`.
- Run `dotnet test --collect:"XPlat Code Coverage"` locally with the same filter as CI to reproduce.
- Check `codecov.yml` for any path exclusion patterns that differ between environments.

---

## DocFX site warnings about unresolvable cross-references

**Symptom:** `docfx docs/docfx.json` prints warnings like:
```
[warning] Cross reference 'SomeType' not found.
```
or
```
[warning] Cannot resolve 'xref:SomeType'.
```

**Cause:** Most of these warnings are caused by attribute types (like `[GeneratedCode]`, `[ExcludeFromCodeCoverage]`, `[ObsoleteAttribute]`) from third-party assemblies (`System.CodeDom.Compiler`, `System.Diagnostics.CodeAnalysis`, `System.ComponentModel`) that DocFX cannot resolve because it does not have metadata for those assemblies in the docs build context.

**Impact:** These warnings are cosmetic — they do not affect the site content or navigation. The generated API docs are correct; only the type links for those attribute types are missing.

**Suppress or ignore:** These warnings are safe to ignore. If they clutter CI output, add the offending cross-reference names to the DocFX `filterConfig.yml` exclusion list or use `--warningsAsErrors` carefully to exclude known-false-positive patterns.

---

## AppContainer apps (UWP / WinUI3)

UWP and sandboxed WinUI3 apps run in an AppContainer with restricted UIA access. Symptoms include:

- The application window appears in the UIA tree but children are inaccessible.
- `FindFirstDescendant` returns null even when the element is visible.
- Access is denied enumerating automation properties.

**Cause:** AppContainer apps require the test process to have the `uiAccess=true` attribute in its manifest and the process must be launched from a trusted location (e.g., `Program Files`). This is a Windows security requirement, not a Flawright limitation.

**Workaround for testing:** Use a signed test runner with `uiAccess=true` and place it in a trusted location, or test the app in a non-AppContainer packaging mode during development. The [FlaUI documentation](https://github.com/FlaUI/FlaUI) has more detail on UIAccess requirements.
