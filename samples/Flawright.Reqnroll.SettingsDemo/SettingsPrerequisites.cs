using Microsoft.Win32;
using Reqnroll;

namespace Flawright.Reqnroll.SettingsDemo;

/// <summary>
/// Reqnroll hook that skips each Settings scenario when the Windows Settings packaged
/// app is not installed on the current machine (e.g. Windows Server SKUs).
/// </summary>
/// <remarks>
/// Runs before the <see cref="FlawrightReqnrollHooks"/> initialisation (Order=-1)
/// so the scenario is skipped before Flawright attempts to launch the application.
/// On <c>windows-latest</c> CI runners (Windows Server 2025) the Settings app package
/// may not be present; scenarios will be skipped rather than failed.
/// On Windows 10/11 developer machines the scenarios run normally.
/// </remarks>
[Binding]
internal static class SettingsPrerequisites
{
    private const string SettingsPackageFamilyName =
        "windows.immersivecontrolpanel_cw5n1h2txyewy";

    /// <summary>
    /// Skips the scenario when the Windows Settings packaged app is not available.
    /// </summary>
    [BeforeScenario(Order = -1)]
    public static void EnsureSettingsAvailable()
    {
        if (!IsSettingsAvailable())
        {
            throw new Xunit.SkipException(
                "Skipped: Windows Settings (windows.immersivecontrolpanel_cw5n1h2txyewy) " +
                "is not installed on this machine. " +
                "This app is present on Windows 10/11 but may be absent on Windows Server SKUs. " +
                "The scenario will run automatically once Settings is available.");
        }
    }

    private static bool IsSettingsAvailable()
    {
        // Check 1: Is the packaged Settings app registered in the current user's package store?
        if (IsPackageFamilyInstalled(SettingsPackageFamilyName))
            return true;

        // Check 2: Fallback — does SystemSettings.exe exist on this system?
        // SystemSettings.exe is the classic fallback on some Windows editions.
        var systemSettings = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "ImmersiveControlPanel",
            "SystemSettings.exe");

        return System.IO.File.Exists(systemSettings);
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
