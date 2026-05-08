# Elevated Apps

Some Windows applications run at administrator integrity level — Task Manager, Registry Editor, Computer Management (`compmgmt.msc`), MMC consoles, Device Manager, and most installers. Automating these apps with Flawright requires the test process itself to run elevated.

## The integrity-level boundary

Windows enforces a security boundary between processes running at different integrity levels:

- **Medium integrity** — standard user processes (the default for most applications)
- **High integrity** — elevated / administrator processes (UAC-prompted or runas)
- **System integrity** — Windows system services

**A lower-integrity process cannot automate a higher-integrity UI.** UI Automation's `FindFirstDescendant` and related calls return nothing (or throw access-denied) across the boundary. This is an OS-level restriction, not a Flawright limitation.

Symptoms of an integrity-level mismatch:
- `NewPageAsync()` returns a page, but all `Locator` calls time out with `FlawrightTimeoutException`.
- `GetAllPagesAsync()` returns a page with the correct title, but its element tree is empty.
- `FindFirstDescendant` (in raw FlaUI) returns null for the elevated window.

## Solution: run the test process elevated

The simplest fix is to run your test runner process at high integrity (as administrator). Options:

### Local development

Run your IDE (Visual Studio, Rider, VS Code) as administrator. When you launch tests from the IDE, the test host inherits the elevated token.

Alternatively, run the `dotnet test` command from an elevated PowerShell or Command Prompt:

```powershell
# Elevated PowerShell (right-click → "Run as administrator")
cd C:\git\MyTests
dotnet test --configuration Release
```

### CI: GitHub Actions

GitHub-hosted `windows-latest` runners do not run as administrator by default. Use a self-hosted runner configured to run as an administrator account:

1. Install the runner under an administrator account.
2. Configure the runner as a service that starts in the administrator user session.
3. Ensure auto-logon is configured so the desktop session is always available.

Or use a step that launches the tests in an elevated subprocess:

```yaml
- name: Run elevated UI tests
  shell: pwsh
  run: |
    Start-Process pwsh `
      -ArgumentList '-NonInteractive', '-Command', 'dotnet test --configuration Release' `
      -Verb RunAs `
      -Wait
```

> **Note:** `Start-Process -Verb RunAs` triggers a UAC prompt on interactive desktops. In CI with a non-interactive session or an auto-elevated account, it may work silently. Test your CI configuration carefully.

### Elevated task (Windows Task Scheduler)

For CI environments that cannot easily elevate, configure a scheduled task that runs with highest privileges. The task runs `dotnet test` and outputs results to a file or a shared path that the CI pipeline reads.

## Known limitation: Consent UI

When an application requires a UAC prompt, `consent.exe` runs in the secure desktop at system integrity level. **No automation tool can drive `consent.exe`** — not Flawright, not any UIA-based tool. The secure desktop is deliberately isolated.

Workaround: ensure the test process is already running elevated so the application it launches also starts elevated without triggering a UAC prompt. Use `CreateProcess` with the `TOKEN_IMPERSONATION` approach, or configure the application's manifest to require `requireAdministrator` and run everything from an already-elevated context.

## Apps that require elevation examples

| App | Integrity level | Typical reason |
|---|---|---|
| Task Manager (full features) | High | Enumerate all processes; set priorities |
| Registry Editor (`regedit.exe`) | High | Write to HKLM keys |
| Computer Management | High | MMC with admin snap-ins |
| Device Manager | High | Driver management |
| Services console | High | Start/stop/configure services |
| Disk Management | High | Partition operations |
| Most MSI installers | High | Write to `Program Files`, `HKLM` |
| Sysinternals Process Explorer | High (optional) | Full process tree |

## Worked example: Registry Editor

> **This example requires the test process to run elevated.**

```csharp
using JerrettDavis.Flawright;
using Xunit;

public class RegistryEditorTests : IAsyncLifetime
{
    private Flawright? _fw;

    public async Task InitializeAsync()
    {
        // regedit.exe requires the test process to be elevated
        _fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = @"C:\Windows\System32\regedit.exe"
        });
    }

    [Fact]
    public async Task RegistryEditor_Opens()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Registry Editor has a Tree on the left and a List on the right
        await page.Locator("controltype:Tree").Expect().ToBeVisibleAsync();
    }

    [Fact]
    public async Task NavigateToHKLM()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var hklmNode = page.Locator("controltype:TreeItem")
            .Filter(new JerrettDavis.Flawright.Locator.LocatorFilterOptions
            {
                HasText = "HKEY_LOCAL_MACHINE"
            });

        await hklmNode.Expect().ToBeVisibleAsync();
        await hklmNode.ClickAsync();
    }

    public async Task DisposeAsync()
    {
        if (_fw != null)
            await _fw.DisposeAsync();
    }
}
```

If the test process is not elevated, `controltype:Tree` will time out with `FlawrightTimeoutException` even though Registry Editor visually appears.

## Checking elevation status

Add a guard to your test setup to fail fast with a descriptive error if the test process is not elevated:

```csharp
using System.Security.Principal;

public static bool IsRunningAsAdministrator()
{
    using var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
}

// In your test fixture InitializeAsync:
if (!IsRunningAsAdministrator())
    throw new InvalidOperationException(
        "This test class requires the test runner to run as administrator. " +
        "Launch your IDE or terminal elevated, then re-run.");
```

## Alternatives

**Remove elevation from the app under test (for test builds)**
If you control the application, remove `requireAdministrator` from the manifest in test builds and replace with `asInvoker`. Run the app and the test process at the same medium integrity. This is the cleanest option for CI.

**UIAccess**
`uiAccess=true` in the test runner manifest allows a non-elevated process to automate elevated windows, but it requires the process to be launched from a trusted location (`Program Files`) and to be signed. This is rarely worth the complexity for test projects.

## Related docs

- [Installer wizards guide](installer-wizards.md) — elevated installer automation
- [UWP / Store apps guide](uwp-store-apps.md) — AppContainer sandbox (different issue)
- [Troubleshooting](../troubleshooting.md) — permission elevation section
