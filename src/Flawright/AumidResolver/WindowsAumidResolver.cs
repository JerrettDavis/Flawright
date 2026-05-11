using System.Diagnostics.CodeAnalysis;
using Flawright.Internals;
using Microsoft.Win32;

namespace Flawright.AumidResolver;

/// <summary>
/// Default <see cref="IAumidResolver"/> implementation that detects Windows
/// AppExecutionAlias stubs and System32 shell-launcher shims and maps them to
/// their corresponding packaged-app AUMIDs.
/// </summary>
/// <remarks>
/// <para>
/// Detection uses a two-tier strategy:
/// </para>
/// <list type="number">
///   <item>
///     <term>WindowsApps alias stub</term>
///     <description>
///     The file is a 0-byte reparse point under
///     <c>%LOCALAPPDATA%\Microsoft\WindowsApps\</c>.  Only files whose
///     basename appears in the built-in known-alias table
///     (<c>notepad.exe</c>, <c>calc.exe</c>, <c>mspaint.exe</c>) are
///     considered.
///     </description>
///   </item>
///   <item>
///     <term>Installed package (registry check)</term>
///     <description>
///     For known-alias basenames that do NOT have a WindowsApps alias stub
///     (e.g. <c>calc.exe</c> on machines where Calculator ships as a
///     System32 redirect), the resolver checks whether the corresponding
///     PackageFamilyName is registered in
///     <c>HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\
///     CurrentVersion\AppModel\Repository\Packages</c>.  If a matching
///     package key exists, the app is installed and the stub will activate it,
///     so the AUMID is returned.
///     </description>
///   </item>
/// </list>
/// <para>
/// If neither tier matches, <see cref="Resolve"/> returns a
/// <see cref="LaunchKind.Path"/> target so the caller falls through to the
/// standard <c>Application.AttachOrLaunch</c> path.
/// </para>
/// </remarks>
public sealed class WindowsAumidResolver : IAumidResolver
{
    // ── Registry constants ────────────────────────────────────────────────────

    /// <summary>
    /// HKCU sub-key under which per-user installed packages are registered.
    /// Key names are versioned PackageFullNames, e.g.
    /// <c>Microsoft.WindowsCalculator_11.2508.4.0_x64__8wekyb3d8bbwe</c>.
    /// </summary>
    private const string PackageRepositorySubKey =
        @"Software\Classes\Local Settings\Software\Microsoft\Windows\" +
        @"CurrentVersion\AppModel\Repository\Packages";

    // ── Test-seam delegates ───────────────────────────────────────────────────

    private readonly string? _windowsAppsDir;
    private readonly Func<string, bool>? _fileExists;
    private readonly Func<string, bool>? _packageFamilyInstalled;

    /// <summary>
    /// Optional callback fired when an alias or package is successfully resolved
    /// to an AUMID. Receives: originalPath, resolvedAumid, packageFamilyName.
    /// </summary>
    internal Action<string, string, string>? OnAliasResolved { get; set; }

    // ── Constructors ──────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the resolver with production defaults (real file system and
    /// registry).
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Production constructor; real-system behavior is exercised by E2E tests, not unit tests.")]
    public WindowsAumidResolver() { }

    /// <summary>
    /// Initialises the resolver with injected delegates for unit-test isolation.
    /// </summary>
    /// <param name="windowsAppsDir">
    /// Override for <c>%LOCALAPPDATA%\Microsoft\WindowsApps</c>.
    /// </param>
    /// <param name="fileExists">
    /// Override for <see cref="File.Exists(string)"/> — used to check for alias
    /// stub files without touching the real file system.
    /// </param>
    /// <param name="packageFamilyInstalled">
    /// Override for the registry-based package-installed check.  Receives the
    /// PackageFamilyName (e.g. <c>Microsoft.WindowsCalculator_8wekyb3d8bbwe</c>)
    /// and returns <see langword="true"/> when the package is installed.
    /// </param>
    internal WindowsAumidResolver(
        string? windowsAppsDir,
        Func<string, bool>? fileExists,
        Func<string, bool>? packageFamilyInstalled)
    {
        _windowsAppsDir = windowsAppsDir;
        _fileExists = fileExists;
        _packageFamilyInstalled = packageFamilyInstalled;
    }

    // ── IAumidResolver ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public LaunchTarget Resolve(string applicationPath)
    {
        if (string.IsNullOrWhiteSpace(applicationPath))
            return new LaunchTarget(LaunchKind.Path, applicationPath);

        if (AppExecutionAliasResolver.TryResolve(
                applicationPath,
                out var aumid,
                _windowsAppsDir,
                _fileExists))
        {
            var pfn = GetPackageFamilyName(aumid);
            OnAliasResolved?.Invoke(applicationPath, aumid, pfn);
            return new LaunchTarget(LaunchKind.Aumid, aumid);
        }

        // Second-tier: for known-alias basenames not found via the WindowsApps
        // stub check (e.g. calc.exe shipped as a System32 redirect on some Win11
        // builds), probe the per-user package registry to confirm the package is
        // actually installed.
        var basename = Path.GetFileName(applicationPath);
        if (AppExecutionAliasResolver.KnownAliases.TryGetValue(basename, out var knownAumid))
        {
            var pfn = GetPackageFamilyName(knownAumid);
            var isInstalled = _packageFamilyInstalled != null
                ? _packageFamilyInstalled(pfn)
                : IsPackageFamilyInstalled(pfn);

            if (isInstalled)
            {
                OnAliasResolved?.Invoke(applicationPath, knownAumid, pfn);
                return new LaunchTarget(LaunchKind.Aumid, knownAumid);
            }
        }

        return new LaunchTarget(LaunchKind.Path, applicationPath);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> when a package with the given AUMID
    /// (or PackageFamilyName) appears to be installed for the current user.
    /// </summary>
    /// <param name="aumid">
    /// Full AUMID (e.g. <c>Microsoft.WindowsCalculator_8wekyb3d8bbwe!App</c>)
    /// or a PackageFamilyName (e.g. <c>Microsoft.WindowsCalculator_8wekyb3d8bbwe</c>).
    /// </param>
    /// <remarks>
    /// Exposed as <c>internal</c> so <c>Flawright.E2ETests.RequiresAppFactAttribute</c>
    /// can reuse the same registry-walk logic without duplication.
    /// </remarks>
    [ExcludeFromCodeCoverage(Justification = "Entry point for registry-backed check; E2E only.")]
    internal static bool IsPackageAumidInstalled(string aumid)
    {
        var pfn = GetPackageFamilyName(aumid);
        return IsPackageFamilyInstalled(pfn);
    }

    /// <summary>
    /// Extracts the PackageFamilyName from an AUMID (the portion before <c>!</c>).
    /// </summary>
    private static string GetPackageFamilyName(string aumid)
    {
        var bang = aumid.IndexOf('!');
        return bang < 0 ? aumid : aumid[..bang];
    }

    /// <summary>
    /// Checks <c>HKCU\...\AppModel\Repository\Packages</c> for any installed
    /// package matching <paramref name="packageFamilyName"/>.
    /// </summary>
    /// <param name="packageFamilyName">
    /// PackageFamilyName, e.g. <c>Microsoft.WindowsCalculator_8wekyb3d8bbwe</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if at least one versioned package key is found;
    /// <see langword="false"/> otherwise (or on any registry access failure).
    /// </returns>
    /// <remarks>
    /// The registry key name format is
    /// <c>&lt;PackageName&gt;_&lt;Version&gt;_&lt;Arch&gt;__&lt;PublisherId&gt;</c>,
    /// e.g. <c>Microsoft.WindowsCalculator_11.2508.4.0_x64__8wekyb3d8bbwe</c>.
    /// The PackageFamilyName (PFN) is <c>&lt;PackageName&gt;_&lt;PublisherId&gt;</c>.
    /// We split on the last underscore to get the package name and publisher ID,
    /// then match keys that start with <c>&lt;PackageName&gt;_</c> and end with
    /// <c>__&lt;PublisherId&gt;</c>.
    /// </remarks>
    [ExcludeFromCodeCoverage(Justification = "Walks the real HKCU registry; covered by E2E tests, not unit tests. The unit-testable seam is the _packageFamilyInstalled delegate parameter on the internal constructor.")]
    private static bool IsPackageFamilyInstalled(string packageFamilyName)
    {
#pragma warning disable CA1031 // Tolerant registry probe — never throw from a resolver
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PackageRepositorySubKey);
            if (key == null)
                return false;

            // Split PFN into package name and publisher ID on the last underscore.
            // PFN format: "<PackageName>_<PublisherId>"
            // Registry key format: "<PackageName>_<Version>_<Arch>__<PublisherId>"
            var lastUnderscore = packageFamilyName.LastIndexOf('_');
            if (lastUnderscore <= 0)
                return false;

            var pkgNamePrefix = packageFamilyName[..lastUnderscore] + "_";
            var publisherSuffix = "__" + packageFamilyName[(lastUnderscore + 1)..];

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                if (subKeyName.StartsWith(pkgNamePrefix, StringComparison.OrdinalIgnoreCase) &&
                    subKeyName.EndsWith(publisherSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
#pragma warning restore CA1031
    }
}
