# Installer Wizards

Installer wizards (MSI, Inno Setup, InstallShield, NSIS, WiX) are multi-step UIs where each step is a separate pane or window. Automating them is useful for smoke-testing deployment packages and integration testing of post-install state.

## Launching

Installers are standard executables. Launch them by path:

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = @"C:\Installers\MyApp-Setup.exe"
});
```

Some installers require arguments (e.g., to suppress auto-launch of the installed app after finishing):

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = @"C:\Installers\MyApp-Setup.exe",
    Arguments = new[] { "/NORUN" }   // Inno Setup custom flag
});
```

> **Elevation requirement**
>
> Most real-world installers require administrator privileges (UAC). Flawright must run from an elevated process to drive an elevated installer. See [Elevated apps guide](elevated-apps.md) for the setup. If the installer prompts UAC, the Consent UI (`consent.exe`) is a separate elevated process that Flawright cannot automate — the elevation must be pre-granted.

## Wizard navigation pattern

Each wizard page typically:
1. Has a unique heading or description visible on screen.
2. Has a "Next" / "Install" / "Finish" button to advance.
3. May have options to configure (radio buttons, checkboxes, text fields).

The general pattern for each step:

```csharp
// Assert you're on the expected step
await page.Locator("name:Expected Step Title").Expect().ToBeVisibleAsync();

// Optionally configure options on this step
// ...

// Advance to the next step
await page.ClickAsync("name:Next");
```

## Worked example: generic multi-step wizard

This example shows a generic wizard flow. Real installer control names vary — use Accessibility Insights against your installer to find the correct names.

```csharp
using JerrettDavis.Flawright;
using Xunit;

public class InstallerTests : IAsyncLifetime
{
    private Flawright? _fw;

    public async Task InitializeAsync()
    {
        // Must run from an elevated process if installer requires admin
        _fw = await Flawright.LaunchAsync(
            new LaunchOptions
            {
                ApplicationPath = @"C:\Installers\MyApp-Setup.exe"
            },
            new FlawrightOptions
            {
                // Installers can be slow to advance between pages
                DefaultTimeout = TimeSpan.FromSeconds(30)
            });
    }

    [Fact]
    public async Task Installer_CompletesSuccessfully()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Step 1: Welcome page
        await page.Locator("name:Welcome").Expect().ToBeVisibleAsync();
        await page.ClickAsync("name:Next");

        // Step 2: License agreement — accept and advance
        await page.Locator("name:License Agreement").Expect().ToBeVisibleAsync();

        // Select "I accept the agreement" radio button (Inno Setup naming)
        await page.ClickAsync("name:I accept the agreement");
        await page.ClickAsync("name:Next");

        // Step 3: Install location — accept default
        await page.Locator("name:Select Destination Location").Expect().ToBeVisibleAsync();
        await page.ClickAsync("name:Next");

        // Step 4: Begin installation
        await page.Locator("name:Ready to Install").Expect().ToBeVisibleAsync();
        await page.ClickAsync("name:Install");

        // Step 5: Wait for progress to complete — the "Finish" button appears
        // when installation is done. Give it extra time.
        await page.Locator("name:Finish").WaitForAsync(
            new JerrettDavis.Flawright.Locator.LocatorWaitForOptions
            {
                Timeout = TimeSpan.FromMinutes(5)
            });

        // Verify "Finish" button is visible (not grayed out)
        await page.Locator("name:Finish").Expect().ToBeEnabledAsync();

        // Finish — uncheck "Launch MyApp" if present
        var launchCheckbox = page.Locator("name:Launch MyApp");
        if (await launchCheckbox.IsVisibleAsync())
            await page.UncheckAsync("name:Launch MyApp");

        await page.ClickAsync("name:Finish");
    }

    public async Task DisposeAsync()
    {
        if (_fw != null)
            await _fw.DisposeAsync();
    }
}
```

## Detecting completion vs. error

**Completion**: The "Finish" button is enabled and visible. The page heading text changes to "Completing the ... Setup Wizard" (Inno Setup) or similar.

**Error during installation**: The installer typically shows an error dialog (a separate window) or the progress bar stops and an error message appears on the page.

Check for errors:

```csharp
// After clicking "Install", poll for either Finish or an error
var finishLocator = page.Locator("name:Finish");
var errorLocator = page.Locator("name:Error");

// Wait for either to appear
await Task.WhenAny(
    finishLocator.WaitForAsync(new LocatorWaitForOptions { Timeout = TimeSpan.FromMinutes(5) }),
    errorLocator.WaitForAsync(new LocatorWaitForOptions { Timeout = TimeSpan.FromMinutes(5) })
);

var hasError = await errorLocator.IsVisibleAsync();
Assert.False(hasError, "Installer reported an error");
```

## Installer-specific patterns

### MSI (Windows Installer UI)

MSI installers use a standard Windows Installer UI or a custom UI. The standard UI has well-known control names. Custom UIs vary. Typical control names:

- "Back" / "Next" / "Cancel" / "Finish" — navigation buttons
- "I accept the terms in the License Agreement" — EULA radio button

MSI installers typically run in a separate `msiexec.exe` process. Launch `msiexec.exe` with the `/i` flag:

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = "msiexec.exe",
    Arguments = new[] { "/i", @"C:\Installers\MyApp.msi" }
});
```

### Inno Setup

Inno Setup is a Pascal-based open-source installer framework. Control names follow a consistent pattern across builds:
- Navigation: "Next >", "< Back", "Cancel", "Finish"
- EULA radio: "I accept the agreement" / "I do not accept the agreement"
- Dir select: "Browse..."
- Progress page: "Installing..."

### InstallShield

InstallShield installers often have a custom WPF or Win32 UI. Control AutomationIds are set by the project author and vary widely. Use Accessibility Insights against your specific build.

### NSIS

NSIS installers use a Win32 dialog. Controls are typically exposed by ControlType — the installer window has `Edit` controls for paths, `Button` controls for navigation. AutomationIds are the dialog resource IDs (numeric strings). Inspect with `inspect.exe` for exact IDs.

## Gotchas

**UAC elevation barrier**
The most common failure: the installer requires elevation, but the test process is not elevated. The installer's main window simply does not appear — Flawright times out waiting for `NewPageAsync`. Fix by running the test runner process as administrator. See [Elevated apps guide](elevated-apps.md).

**Consent UI (`consent.exe`) cannot be automated**
If the installer triggers a UAC prompt, `consent.exe` appears as a separate elevated process in the secure desktop. No automation tool can drive this — not Flawright, not any other UIA-based tool. Options: pre-grant elevation (run the test process as admin), or use a task-sequence tool that can accept the prompt at the OS level.

**Installer window title changes during installation**
Some installers change their window title as they progress (e.g., "Installing..." then "Setup Complete"). Use `WaitForPageAsync` with a partial title match, or use the existing `page` object (it tracks the window by handle, not title).

**Progress bar timing**
`ProgressBar` elements report a value via the `RangeValue` pattern. Do not poll the progress bar to determine completion — wait for the "Finish" button to appear instead.

**Separate windows for sub-steps**
Some installers open secondary dialogs (e.g., "Select components", "Confirm overwrite"). Handle these with `WaitForPageAsync`:

```csharp
// After clicking a button that may open a secondary dialog
var overwriteDialog = await fw.Browser.WaitForPageAsync("Confirm", timeout: TimeSpan.FromSeconds(5));
if (overwriteDialog != null)
    await overwriteDialog.ClickAsync("name:Yes");
```

## Related docs

- [Elevated apps guide](elevated-apps.md) — elevation and UAC setup
- [Multi-window apps guide](multi-window.md) — secondary dialog handling
- [Auto-waiting](../auto-waiting.md) — configuring timeouts for slow installers
