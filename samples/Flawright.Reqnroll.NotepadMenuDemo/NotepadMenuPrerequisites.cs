using Microsoft.Win32;
using Reqnroll;

namespace Flawright.Reqnroll.NotepadMenuDemo;

/// <summary>
/// Reqnroll hook that skips each NotepadMenu scenario when <c>notepad.exe</c> does not
/// resolve to a real executable or an installed packaged-app (WinUI3 Notepad) on
/// the current machine.
/// </summary>
/// <remarks>
/// Runs before the <see cref="FlawrightReqnrollHooks"/> initialisation (Order=-1)
/// so the scenario is skipped before Flawright attempts to launch the application.
/// On <c>windows-latest</c> CI runners (Windows Server 2025) the WinUI3 Notepad
/// is present, so these tests run. Developer machines that have not installed the
/// Store version of Notepad will see the scenario as skipped rather than failed.
/// </remarks>
[Binding]
internal static class NotepadMenuPrerequisites
{
    /// <summary>
    /// Skips the scenario when Windows Notepad (packaged WinUI3 or classic Win32)
    /// is not available on the current machine.
    /// </summary>
    [BeforeScenario(Order = -1)]
    public static void EnsureNotepadAvailable()
    {
        if (!IsNotepadAvailable())
        {
            throw new Xunit.SkipException(
                "Skipped: Windows Notepad is not installed on this machine. " +
                "Install it from the Microsoft Store or via winget install Microsoft.WindowsNotepad. " +
                "The scenario will run automatically once Notepad is available.");
        }
    }

    private static bool IsNotepadAvailable()
    {
        // Check 1: Is the packaged WinUI3 Notepad (Windows 11) installed?
        if (IsPackageFamilyInstalled("Microsoft.WindowsNotepad_8wekyb3d8bbwe"))
            return true;

        // Check 2: Is classic notepad.exe available on PATH (Windows 10 / Server)?
        var resolved = TryResolveOnPath("notepad.exe");
        if (resolved != null)
        {
            // Make sure it is not a 0-byte AppExecutionAlias stub for an
            // uninstalled package.
            var fi = new System.IO.FileInfo(resolved);
            return fi.Exists && fi.Length > 0;
        }

        return false;
    }

    private static bool IsPackageFamilyInstalled(string packageFamilyName)
    {
        const string SubKey =
            @"Software\Classes\Local Settings\Software\Microsoft\Windows\" +
            @"CurrentVersion\AppModel\Repository\Packages";

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SubKey);
            if (key == null) return false;

            var lastUnderscore = packageFamilyName.LastIndexOf('_');
            if (lastUnderscore <= 0) return false;

            var packageName = packageFamilyName[..lastUnderscore];
            var publisherId = packageFamilyName[(lastUnderscore + 1)..];

            return key.GetSubKeyNames()
                .Any(k => k.StartsWith(packageName + "_", StringComparison.OrdinalIgnoreCase)
                          && k.EndsWith("__" + publisherId, StringComparison.OrdinalIgnoreCase));
        }
#pragma warning disable CA1031 // Tolerant registry probe — exceptions here must not fail the skip check
        catch (Exception)
#pragma warning restore CA1031
        {
            return false;
        }
    }

    private static string? TryResolveOnPath(string exe)
    {
        try
        {
            var pathExt = Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT";
            var pathDirs = (Environment.GetEnvironmentVariable("PATH") ?? "")
                .Split(';', StringSplitOptions.RemoveEmptyEntries);

            var baseName = System.IO.Path.GetFileNameWithoutExtension(exe);
            var exts = pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var dir in pathDirs)
            {
                foreach (var ext in exts)
                {
                    var candidate = System.IO.Path.Combine(dir.Trim(), baseName + ext);
                    if (System.IO.File.Exists(candidate))
                        return candidate;
                }
            }
        }
#pragma warning disable CA1031 // Tolerant PATH walk — exceptions here must not fail the skip check
        catch (Exception)
#pragma warning restore CA1031
        {
            // swallow
        }

        return null;
    }
}
