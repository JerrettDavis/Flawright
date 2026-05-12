using Reqnroll;

namespace Flawright.Reqnroll.QuickSettingsDemo;

/// <summary>
/// Reqnroll hook that skips each Quick Settings scenario when the Windows shell
/// (<c>ShellExperienceHost.exe</c>) is not running on the current machine.
/// </summary>
/// <remarks>
/// <para>
/// Runs before the <see cref="FlawrightReqnrollHooks"/> initialisation (Order=-1)
/// so the scenario is skipped before Flawright attempts to attach to the shell process.
/// </para>
/// <para>
/// Quick Settings is a system flyout owned by <c>ShellExperienceHost.exe</c>.
/// It cannot be launched via <c>@launch:</c> or <c>@aumid:</c> — it is triggered
/// by sending the <c>Win+A</c> keyboard chord to the desktop. The test uses
/// <c>@attach:ShellExperienceHost</c> to attach to the running shell process.
/// </para>
/// <para>
/// These scenarios are expected to skip on <c>windows-2025-vs2026</c> CI runners
/// because <c>ShellExperienceHost.exe</c> is not present on Windows Server Core.
/// This is by design — Quick Settings requires the full Windows 11 shell.
/// </para>
/// </remarks>
[Binding]
internal static class QuickSettingsPrerequisites
{
    private const string ShellHostProcessName = "ShellExperienceHost";

    /// <summary>
    /// Skips the scenario when <c>ShellExperienceHost.exe</c> is not running,
    /// which indicates the Quick Settings flyout is not available.
    /// </summary>
    [BeforeScenario(Order = -1)]
    public static void EnsureShellExperienceHostAvailable()
    {
        if (!IsShellExperienceHostRunning())
        {
            throw new Xunit.SkipException(
                "Skipped: ShellExperienceHost.exe is not running on this machine. " +
                "Quick Settings scenarios require the full Windows 11 shell " +
                "(ShellExperienceHost.exe must be active). " +
                "This process is absent on Windows Server Core and headless CI runners. " +
                "The scenario will run automatically on a Windows 11 desktop machine.");
        }
    }

    private static bool IsShellExperienceHostRunning()
    {
        try
        {
            var processes = System.Diagnostics.Process.GetProcessesByName(ShellHostProcessName);
            return processes.Length > 0;
        }
#pragma warning disable CA1031 // Tolerant process probe — exceptions here must not fail the skip check
        catch (Exception)
#pragma warning restore CA1031
        {
            return false;
        }
    }
}
