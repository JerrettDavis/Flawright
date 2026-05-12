using Reqnroll;

namespace Flawright.Reqnroll.ExplorerDemo;

/// <summary>
/// Reqnroll hook that skips each Explorer scenario when <c>explorer.exe</c> is not
/// available or cannot be resolved to a real executable on the current machine.
/// </summary>
/// <remarks>
/// Runs before the <see cref="FlawrightReqnrollHooks"/> initialisation (Order=-1)
/// so the scenario is skipped before Flawright attempts to launch the application.
/// On interactive developer machines <c>explorer.exe</c> is always present.
/// On Windows Server Core installations (no desktop shell) it may be absent or
/// non-functional; scenarios will be skipped rather than failed.
/// </remarks>
[Binding]
internal static class ExplorerPrerequisites
{
    /// <summary>
    /// Skips the scenario when <c>explorer.exe</c> is not available or the Windows
    /// shell is not running (e.g. headless/Server Core environments).
    /// </summary>
    [BeforeScenario(Order = -1)]
    public static void EnsureExplorerAvailable()
    {
        if (!IsExplorerAvailable())
        {
            throw new Xunit.SkipException(
                "Skipped: explorer.exe is not available or the Windows shell is not running. " +
                "File Explorer scenarios require a full Windows desktop shell (Windows 10/11). " +
                "The scenario will run automatically on a supported machine.");
        }
    }

    private static bool IsExplorerAvailable()
    {
        // Check 1: Does explorer.exe exist in the Windows directory?
        var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var explorerPath = System.IO.Path.Combine(windowsDir, "explorer.exe");
        if (!System.IO.File.Exists(explorerPath))
            return false;

        // Check 2: Is the Windows shell (explorer.exe desktop process) actually running?
        // On Server Core the binary exists but the shell is not started — launching a
        // new explorer.exe window may fail or produce an unusable automation target.
        try
        {
            var shellProcesses = System.Diagnostics.Process.GetProcessesByName("explorer");
            return shellProcesses.Length > 0;
        }
#pragma warning disable CA1031 // Tolerant process probe — exceptions here must not fail the skip check
        catch (Exception)
#pragma warning restore CA1031
        {
            // If we cannot enumerate processes, assume shell is unavailable.
            return false;
        }
    }
}
