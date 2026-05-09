using Microsoft.Win32;
using Reqnroll;

namespace Flawright.Reqnroll.CalculatorDemo;

/// <summary>
/// Reqnroll hook that skips each Calculator scenario when the Windows Calculator
/// UWP package is not installed on the current machine.
/// </summary>
/// <remarks>
/// Runs before the <see cref="FlawrightReqnrollHooks"/> initialisation (Order=-1)
/// so the scenario is skipped before Flawright attempts to launch the application.
/// On <c>windows-latest</c> CI runners (Windows Server 2025) the Windows Calculator
/// UWP package is typically not present, so all Calculator scenarios will be skipped
/// rather than failed. On Windows 11 developer machines with Calculator installed the
/// scenarios run normally.
/// </remarks>
[Binding]
internal static class CalculatorPrerequisites
{
    private const string CalculatorPackageFamilyName = "Microsoft.WindowsCalculator_8wekyb3d8bbwe";

    /// <summary>
    /// Skips the scenario when the Windows Calculator UWP package is not installed.
    /// </summary>
    [BeforeScenario(Order = -1)]
    public static void EnsureCalculatorAvailable()
    {
        if (!IsPackageFamilyInstalled(CalculatorPackageFamilyName))
        {
            throw new Xunit.SkipException(
                "Skipped: Windows Calculator (Microsoft.WindowsCalculator_8wekyb3d8bbwe) " +
                "is not installed on this machine. " +
                "Install it with: winget install Microsoft.WindowsCalculator --silent. " +
                "The scenario will run automatically once Calculator is available.");
        }
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
}
