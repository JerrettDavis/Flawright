using System.Diagnostics.CodeAnalysis;

namespace Flawright.Internals;

/// <summary>
/// Detects Windows AppExecutionAlias stubs (the 0-byte reparse points under
/// <c>%LOCALAPPDATA%\Microsoft\WindowsApps\</c>) and resolves them to their
/// Application User Model ID (AUMID) so that FlaUI can use
/// <c>Application.LaunchStoreApp</c> instead of the broken
/// <c>Application.AttachOrLaunch</c> path.
/// </summary>
/// <remarks>
/// On Windows 11, several inbox apps (Notepad, Calculator, Paint) are packaged
/// WinUI3 applications.  Their <c>notepad.exe</c>, <c>calc.exe</c>, and
/// <c>mspaint.exe</c> entries in <c>WindowsApps</c> are 0-byte sparse reparse
/// points — AppExecutionAlias stubs.  When launched via
/// <see cref="System.Diagnostics.Process"/>, the stub immediately exits after
/// activating the real packaged app, which means FlaUI ends up tracking a
/// dead process handle instead of the real application.
///
/// This resolver detects the stub and short-circuits to
/// <c>IApplicationLauncher.LaunchStoreApp</c> transparently.
/// </remarks>
internal static class AppExecutionAliasResolver
{
    /// <summary>
    /// Well-known AppExecutionAlias basename → AUMID mapping for the most
    /// common Windows-shipped packaged apps.  Keyed by the <c>.exe</c>
    /// filename (case-insensitive).
    /// </summary>
    internal static readonly Dictionary<string, string> KnownAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["notepad.exe"] = "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App",
            ["calc.exe"] = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",
            ["mspaint.exe"] = "Microsoft.Paint_8wekyb3d8bbwe!App",
        };

    /// <summary>
    /// Attempts to detect whether <paramref name="applicationPath"/> refers to a
    /// Windows AppExecutionAlias and, if so, resolves it to an AUMID.
    /// </summary>
    /// <param name="applicationPath">
    /// The value of <see cref="LaunchOptions.ApplicationPath"/>.
    /// May be a bare filename (<c>"notepad.exe"</c>), a rooted path, or
    /// anything in between.
    /// </param>
    /// <param name="aumid">
    /// When this method returns <see langword="true"/>, contains the resolved AUMID.
    /// Otherwise, contains an empty string.
    /// </param>
    /// <param name="windowsAppsDir">
    /// Optional override for the WindowsApps directory — used by unit tests to
    /// inject a fake path without touching the file system.  When
    /// <see langword="null"/>, defaults to
    /// <c>%LOCALAPPDATA%\Microsoft\WindowsApps</c>.
    /// </param>
    /// <param name="fileExists">
    /// Optional delegate for checking file existence — used by unit tests to
    /// avoid touching the real file system.  When <see langword="null"/>,
    /// defaults to <see cref="File.Exists"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the path resolved to a known AUMID;
    /// <see langword="false"/> if the caller should fall through to the standard
    /// Win32 launch path.
    /// </returns>
    internal static bool TryResolve(
        string applicationPath,
        [NotNullWhen(true)] out string? aumid,
        string? windowsAppsDir = null,
        Func<string, bool>? fileExists = null)
    {
        aumid = null;

        if (string.IsNullOrWhiteSpace(applicationPath))
            return false;

        fileExists ??= File.Exists;
        windowsAppsDir ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps");

        // Fast path: if the bare filename (or the filename portion of a path) is a
        // known alias, check whether the alias stub exists in the WindowsApps directory.
        // This handles the common case where PATH resolves "notepad.exe" to
        // C:\Windows\System32\notepad.exe before the WindowsApps alias — on Windows 11
        // the System32 stub also redirects to the packaged app, so the AUMID resolution
        // still applies as long as the WindowsApps alias exists on this system.
        var basename = Path.GetFileName(applicationPath);
        if (KnownAliases.TryGetValue(basename, out var knownAumid))
        {
            var aliasPath = Path.Combine(windowsAppsDir, basename);
            if (fileExists(aliasPath))
            {
                aumid = knownAumid;
                return true;
            }
        }

        // Slower path: resolve to a full path and check if it's within WindowsApps.
        var resolvedPath = ResolveOnPath(applicationPath, fileExists);
        if (resolvedPath == null)
            return false;

        // Only activate for paths that live inside the WindowsApps directory.
        if (!resolvedPath.StartsWith(windowsAppsDir, StringComparison.OrdinalIgnoreCase))
            return false;

        basename = Path.GetFileName(resolvedPath);
        if (!KnownAliases.TryGetValue(basename, out knownAumid))
            return false;   // Unknown alias — fall through; don't guess.

        aumid = knownAumid;
        return true;
    }

    /// <summary>
    /// Resolves <paramref name="path"/> to an absolute path, searching the
    /// <c>PATH</c> environment variable when no directory component is present.
    /// </summary>
    /// <returns>
    /// The absolute path if the file was found; <see langword="null"/> otherwise.
    /// </returns>
    internal static string? ResolveOnPath(string path, Func<string, bool>? fileExists = null)
    {
        fileExists ??= File.Exists;

        // Rooted path or explicitly relative — don't search PATH.
        if (Path.IsPathRooted(path) ||
            path.Contains(Path.DirectorySeparatorChar) ||
            path.Contains(Path.AltDirectorySeparatorChar))
        {
            return fileExists(path) ? Path.GetFullPath(path) : null;
        }

        // Bare name: search the PATH entries.
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(dir, path);
            if (fileExists(candidate))
                return Path.GetFullPath(candidate);

            // Also try appending ".exe" if not already present.
            if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                var candidateExe = Path.Combine(dir, path + ".exe");
                if (fileExists(candidateExe))
                    return Path.GetFullPath(candidateExe);
            }
        }

        return null;
    }
}
