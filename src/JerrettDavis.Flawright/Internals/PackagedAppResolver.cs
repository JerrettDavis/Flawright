using System.ComponentModel;
using System.Diagnostics;

namespace JerrettDavis.Flawright.Internals;

/// <summary>
/// Resolves the runtime PID of a packaged Windows app from its AUMID/PackageFamilyName by
/// enumerating processes whose main module path is under the package install directory.
/// </summary>
/// <remarks>
/// On Windows 11, <c>IApplicationActivationManager::ActivateApplication</c> (invoked by
/// <c>Application.LaunchStoreApp</c>) returns the PID of an activator/broker process that
/// exits within ~1 second after handing off to the actual app process.  Polling for the
/// real process under <c>C:\Program Files\WindowsApps\&lt;PFN&gt;_*\</c> and re-attaching
/// FlaUI's tracking to that live PID prevents the downstream
/// <c>WaitWhileMainHandleIsMissing</c> from failing with
/// "Process with an Id of N is not running".
/// </remarks>
internal static class PackagedAppResolver
{
    /// <summary>
    /// Extracts the PackageFamilyName from an AUMID.
    /// </summary>
    /// <param name="aumid">
    /// An Application User Model ID such as
    /// <c>Microsoft.WindowsNotepad_8wekyb3d8bbwe!App</c>.
    /// </param>
    /// <returns>
    /// The portion before the first <c>!</c>.  If there is no <c>!</c>, the full
    /// string is returned unchanged.
    /// </returns>
    /// <example>
    /// <code>
    /// GetPackageFamilyName("Microsoft.WindowsNotepad_8wekyb3d8bbwe!App")
    ///     == "Microsoft.WindowsNotepad_8wekyb3d8bbwe"
    /// </code>
    /// </example>
    public static string GetPackageFamilyName(string aumid)
    {
        if (string.IsNullOrEmpty(aumid))
            return aumid;

        var bang = aumid.IndexOf('!');
        return bang < 0 ? aumid : aumid[..bang];
    }

    /// <summary>
    /// Polls for a running process whose main module path is under the WindowsApps
    /// directory of the given PackageFamilyName.
    /// </summary>
    /// <param name="packageFamilyName">
    /// The PackageFamilyName portion of the AUMID (everything before <c>!</c>).
    /// </param>
    /// <param name="timeout">
    /// Maximum time to wait before giving up and returning <c>0</c>.
    /// </param>
    /// <param name="pollInterval">
    /// Time between each enumeration pass.  Defaults to 100 ms.
    /// </param>
    /// <param name="processSnapshotProvider">
    /// Optional injectable snapshot provider for testability.  Each call must
    /// return a sequence of <c>(Pid, MainModulePath)</c> tuples representing the
    /// currently running processes.  <c>MainModulePath</c> may be <see langword="null"/>
    /// when the module path cannot be read (e.g. access denied on a protected process).
    /// When <see langword="null"/>, defaults to the real <see cref="Process.GetProcesses()"/>
    /// enumeration with a tolerant <see cref="Process.MainModule"/> read.
    /// </param>
    /// <returns>
    /// The PID of the first matching process found within <paramref name="timeout"/>,
    /// or <c>0</c> if no match was found before the deadline.
    /// </returns>
    public static int WaitForPackagedAppProcess(
        string packageFamilyName,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        Func<IEnumerable<(int Pid, string? MainModulePath)>>? processSnapshotProvider = null)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(100);
        var snapshot = processSnapshotProvider ?? DefaultSnapshot;
        var deadline = DateTime.UtcNow + timeout;

        // The package install directory path looks like:
        //   C:\Program Files\WindowsApps\<PackageName>_<Version>_<Arch>__<PublisherId>\...
        //
        // The PackageFamilyName (PFN) is "<PackageName>_<PublisherId>" — note the single
        // underscore between name and publisher in the PFN, but a DOUBLE underscore before
        // the publisher in the directory name.  We must match on both halves of the PFN
        // rather than on the PFN string directly.
        //
        // Strategy: split PFN on the LAST underscore to get (packageName, publisherId),
        // then require both:
        //   path contains  \WindowsApps\<packageName>_        (starts of dir name)
        //   path contains  __<publisherId>\  OR  __<publisherId>  (end of dir name)
        var lastUnderscore = packageFamilyName.LastIndexOf('_');
        string packageNameMarker;
        string publisherMarker;

        if (lastUnderscore > 0)
        {
            var pkgName = packageFamilyName[..lastUnderscore];
            var publisherId = packageFamilyName[(lastUnderscore + 1)..];
            packageNameMarker = $"\\WindowsApps\\{pkgName}_";
            publisherMarker = $"__{publisherId}";
        }
        else
        {
            // No underscore — treat the whole PFN as a package-name prefix.
            packageNameMarker = $"\\WindowsApps\\{packageFamilyName}_";
            publisherMarker = string.Empty;
        }

        while (DateTime.UtcNow < deadline)
        {
            foreach (var (pid, path) in snapshot())
            {
                if (path != null &&
                    path.Contains(packageNameMarker, StringComparison.OrdinalIgnoreCase) &&
                    (publisherMarker.Length == 0 ||
                     path.Contains(publisherMarker, StringComparison.OrdinalIgnoreCase)))
                {
                    return pid;
                }
            }

            Thread.Sleep(interval);
        }

        return 0;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static IEnumerable<(int Pid, string? MainModulePath)> DefaultSnapshot()
    {
        var processes = Process.GetProcesses();
        var results = new List<(int, string?)>(processes.Length);

        foreach (var p in processes)
        {
            try
            {
                results.Add((p.Id, TryGetMainModulePath(p)));
            }
            finally
            {
                p.Dispose();
            }
        }

        return results;
    }

    private static string? TryGetMainModulePath(Process p)
    {
        try
        {
            return p.MainModule?.FileName;
        }
        catch (Win32Exception)
        {
            // Access denied — protected process; skip it.
            return null;
        }
        catch (InvalidOperationException)
        {
            // Process exited between GetProcesses() and the MainModule read.
            return null;
        }
    }
}
