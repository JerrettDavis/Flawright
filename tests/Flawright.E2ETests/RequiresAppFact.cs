using Flawright.AumidResolver;
using Flawright.Internals;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// A custom xUnit <see cref="FactAttribute"/> that skips the test at runtime
/// when a prerequisite application is not available on the current machine.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute instead of plain <c>[Fact]</c> for any test that
/// exercises a system app (Calculator, Notepad, etc.) that may not be
/// installed on every machine — in particular, <c>windows-latest</c>
/// (Windows Server 2025) CI runners do not ship UWP inbox apps.
/// </para>
/// <para>
/// When the prerequisite is absent, xUnit skips the test and reports a
/// human-readable reason in the TRX output and console.  The test will
/// run automatically once the prerequisite is met — no workflow changes
/// are required.
/// </para>
/// <example>
/// <code>
/// // Skip when Calculator's AppX package is absent:
/// [RequiresAppFact(Aumid = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App")]
/// public async Task Calculator_ClickButton() { ... }
///
/// // Skip when notepad.exe resolves to a stub for an uninstalled package:
/// [RequiresAppFact(ExePath = "notepad.exe")]
/// public async Task Notepad_TypeText() { ... }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresAppFactAttribute : FactAttribute
{
    private string? _skipOverride;

    /// <summary>
    /// Application User Model ID of the AppX package that must be installed.
    /// For example <c>"Microsoft.WindowsCalculator_8wekyb3d8bbwe!App"</c>.
    /// When set, the registry is probed for the matching PackageFamilyName.
    /// </summary>
    public string? Aumid { get; init; }

    /// <summary>
    /// Bare filename or full path of the executable that must be available.
    /// For example <c>"notepad.exe"</c>.
    /// When set, PATH is searched; if the resolved file is a WindowsApps
    /// AppExecutionAlias stub, the backing AppX package is also verified.
    /// </summary>
    public string? ExePath { get; init; }

    /// <inheritdoc/>
    public override string? Skip
    {
        get => _skipOverride ?? CheckPrerequisite();
        set => _skipOverride = value;
    }

    // ── Prerequisite check ────────────────────────────────────────────────────

    private string? CheckPrerequisite()
    {
        if (Aumid != null && !IsPackageInstalled(Aumid))
        {
            return $"Skipped: AppX package for AUMID '{Aumid}' is not installed on this machine. " +
                   $"Install it with: winget install <package-id>  (or Add-AppxPackage). " +
                   $"The test will run automatically once the package is available.";
        }

        if (ExePath != null && !IsExeAvailable(ExePath))
        {
            return $"Skipped: executable '{ExePath}' is not available on this machine (not found on PATH, " +
                   $"or it is an AppExecutionAlias stub whose backing AppX package is not installed). " +
                   $"Install the corresponding application and the test will run automatically.";
        }

        return null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> when the AppX package identified by
    /// <paramref name="aumid"/> is registered for the current user.
    /// </summary>
    /// <remarks>
    /// Delegates to <see cref="WindowsAumidResolver.IsPackageAumidInstalled"/>
    /// so the registry-walk logic lives in exactly one place.
    /// </remarks>
    private static bool IsPackageInstalled(string aumid) =>
        WindowsAumidResolver.IsPackageAumidInstalled(aumid);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="exe"/> resolves to a
    /// real executable (not an unresolvable AppExecutionAlias stub).
    /// </summary>
    /// <remarks>
    /// <list type="number">
    ///   <item>If the exe resolves to a path outside <c>WindowsApps</c>, it is
    ///   treated as a genuine Win32 binary — returns <see langword="true"/>.</item>
    ///   <item>If it resolves into <c>WindowsApps</c> (an AppExecutionAlias stub),
    ///   the corresponding AppX package is probed via
    ///   <see cref="WindowsAumidResolver.IsPackageAumidInstalled"/>.</item>
    ///   <item>If the exe is not found on PATH at all, returns
    ///   <see langword="false"/>.</item>
    /// </list>
    /// </remarks>
    private static bool IsExeAvailable(string exe)
    {
        // Let AppExecutionAliasResolver handle both the PATH walk and the
        // WindowsApps detection in one step.
        if (AppExecutionAliasResolver.TryResolve(exe, out var aumid))
        {
            // It IS an alias stub — verify the backing package is installed.
            return WindowsAumidResolver.IsPackageAumidInstalled(aumid);
        }

        // Not a known alias stub — check whether the exe exists on PATH.
        var resolved = AppExecutionAliasResolver.ResolveOnPath(exe);
        return resolved != null;
    }
}

/// <summary>
/// A custom xUnit <see cref="TheoryAttribute"/> that skips the test at runtime
/// when a prerequisite application is not available on the current machine.
/// </summary>
/// <remarks>
/// Mirrors <see cref="RequiresAppFactAttribute"/> for parameterised tests.
/// See that type's documentation for full usage guidance.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresAppTheoryAttribute : TheoryAttribute
{
    private string? _skipOverride;

    /// <summary>
    /// Application User Model ID of the AppX package that must be installed.
    /// </summary>
    public string? Aumid { get; init; }

    /// <summary>
    /// Bare filename or full path of the executable that must be available.
    /// </summary>
    public string? ExePath { get; init; }

    /// <inheritdoc/>
    public override string? Skip
    {
        get => _skipOverride ?? CheckPrerequisite();
        set => _skipOverride = value;
    }

    private string? CheckPrerequisite()
    {
        if (Aumid != null && !IsPackageInstalled(Aumid))
        {
            return $"Skipped: AppX package for AUMID '{Aumid}' is not installed on this machine. " +
                   $"Install it with: winget install <package-id>  (or Add-AppxPackage). " +
                   $"The test will run automatically once the package is available.";
        }

        if (ExePath != null && !IsExeAvailable(ExePath))
        {
            return $"Skipped: executable '{ExePath}' is not available on this machine (not found on PATH, " +
                   $"or it is an AppExecutionAlias stub whose backing AppX package is not installed). " +
                   $"Install the corresponding application and the test will run automatically.";
        }

        return null;
    }

    private static bool IsPackageInstalled(string aumid) =>
        WindowsAumidResolver.IsPackageAumidInstalled(aumid);

    private static bool IsExeAvailable(string exe)
    {
        if (AppExecutionAliasResolver.TryResolve(exe, out var aumid))
            return WindowsAumidResolver.IsPackageAumidInstalled(aumid);

        var resolved = AppExecutionAliasResolver.ResolveOnPath(exe);
        return resolved != null;
    }
}
