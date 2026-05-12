using Microsoft.Win32;
using Reqnroll;

namespace Flawright.Reqnroll.PaintDrawDemo;

/// <summary>
/// Reqnroll hook that skips each PaintDraw scenario when MS Paint
/// (<c>mspaint.exe</c> / <c>Microsoft.Paint_8wekyb3d8bbwe</c>) is not installed
/// on the current machine.
/// </summary>
/// <remarks>
/// Runs before the <see cref="FlawrightReqnrollHooks"/> initialisation (Order=-1)
/// so the scenario is skipped before Flawright attempts to launch the application.
/// On <c>windows-latest</c> CI runners (Windows Server 2025) the modern Paint app
/// may not be installed, so all PaintDraw scenarios will be skipped rather than
/// failed. On Windows 11 developer machines with Paint installed the scenarios run.
/// </remarks>
[Binding]
internal static class PaintDrawPrerequisites
{
    private const string PaintPackageFamilyName = "Microsoft.Paint_8wekyb3d8bbwe";

    /// <summary>
    /// Skips the scenario when MS Paint (packaged modern app) is not installed.
    /// </summary>
    [BeforeScenario(Order = -1)]
    public static void EnsurePaintAvailable()
    {
        if (!IsPaintAvailable())
        {
            throw new Xunit.SkipException(
                "Skipped: MS Paint (Microsoft.Paint_8wekyb3d8bbwe) is not installed on this machine. " +
                "Install it with: winget install Microsoft.Paint --silent. " +
                "The scenario will run automatically once Paint is available.");
        }
    }

    private static bool IsPaintAvailable()
    {
        // Check 1: Is the packaged modern Paint (Windows 11) installed?
        if (IsPackageFamilyInstalled(PaintPackageFamilyName))
            return true;

        // Check 2: Is classic mspaint.exe available on PATH (older Windows)?
        // Classic mspaint.exe is a real Win32 executable, not a 0-byte alias.
        var resolved = TryResolveOnPath("mspaint.exe");
        if (resolved != null)
        {
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
