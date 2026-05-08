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
