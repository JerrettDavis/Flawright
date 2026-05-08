using JerrettDavis.Flawright.Internals;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Internals;

/// <summary>
/// Unit tests for <see cref="PackagedAppResolver"/>.
///
/// All tests that exercise <see cref="PackagedAppResolver.WaitForPackagedAppProcess"/>
/// inject a <c>processSnapshotProvider</c> delegate so the real process list and
/// file system are never touched.
/// </summary>
public sealed class PackagedAppResolverTests
{
    // ─── GetPackageFamilyName ─────────────────────────────────────────────────

    [Fact]
    public void GetPackageFamilyName_AumidWithBang_ReturnsPrefix()
    {
        var pfn = PackagedAppResolver.GetPackageFamilyName(
            "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App");

        Assert.Equal("Microsoft.WindowsNotepad_8wekyb3d8bbwe", pfn);
    }

    [Fact]
    public void GetPackageFamilyName_AumidWithNoBang_ReturnsFullString()
    {
        const string NoBang = "Microsoft.WindowsNotepad_8wekyb3d8bbwe";
        var pfn = PackagedAppResolver.GetPackageFamilyName(NoBang);

        Assert.Equal(NoBang, pfn);
    }

    [Fact]
    public void GetPackageFamilyName_EmptyString_ReturnsEmptyString()
    {
        var pfn = PackagedAppResolver.GetPackageFamilyName(string.Empty);
        Assert.Equal(string.Empty, pfn);
    }

    [Fact]
    public void GetPackageFamilyName_MultipleExclamationMarks_ReturnsUpToFirstBang()
    {
        // Ensure only the first '!' is treated as the separator.
        var pfn = PackagedAppResolver.GetPackageFamilyName("Foo_abc!Bar!Baz");
        Assert.Equal("Foo_abc", pfn);
    }

    // ─── WaitForPackagedAppProcess — match found ──────────────────────────────

    [Fact]
    public void WaitForPackagedAppProcess_MatchingProcessPresent_ReturnsPid()
    {
        const int ExpectedPid = 1234;
        const string Pfn = "Microsoft.WindowsNotepad_8wekyb3d8bbwe";
        var matchingPath = @"C:\Program Files\WindowsApps\Microsoft.WindowsNotepad_8wekyb3d8bbwe_11.2501.26.0_x64__8wekyb3d8bbwe\Notepad\Notepad.exe";

        IEnumerable<(int, string?)> Snapshot() =>
        [
            (ExpectedPid, matchingPath)
        ];

        var pid = PackagedAppResolver.WaitForPackagedAppProcess(
            Pfn,
            TimeSpan.FromSeconds(1),
            processSnapshotProvider: Snapshot);

        Assert.Equal(ExpectedPid, pid);
    }

    [Fact]
    public void WaitForPackagedAppProcess_MultipleProcesses_ReturnsFirstMatch()
    {
        // The method should return the first matching PID it encounters.
        const string Pfn = "Microsoft.WindowsNotepad_8wekyb3d8bbwe";
        const string MatchingPath = @"C:\Program Files\WindowsApps\Microsoft.WindowsNotepad_8wekyb3d8bbwe_11.2501.26.0_x64__8wekyb3d8bbwe\Notepad.exe";
        const string UnrelatedPath = @"C:\Windows\System32\cmd.exe";

        // First snapshot: an unrelated process; second snapshot: unrelated + match.
        // With a short poll interval the method should find the match on the first
        // call to Snapshot that includes it.

        IEnumerable<(int, string?)> Snapshot() =>
        [
            (9000, UnrelatedPath),
            (8888, MatchingPath),
            (7777, MatchingPath)  // also matches, but 8888 comes first
        ];

        var pid = PackagedAppResolver.WaitForPackagedAppProcess(
            Pfn,
            TimeSpan.FromSeconds(1),
            processSnapshotProvider: Snapshot);

        Assert.Equal(8888, pid);
    }

    [Fact]
    public void WaitForPackagedAppProcess_MatchIsCaseInsensitive()
    {
        // The path comparison must be case-insensitive (Windows file system).
        const string Pfn = "Microsoft.WindowsNotepad_8wekyb3d8bbwe";
        const string UpperCasePath = @"C:\PROGRAM FILES\WINDOWSAPPS\MICROSOFT.WINDOWSNOTEPAD_8WEKYB3D8BBWE_11.0_X64__8WEKYB3D8BBWE\NOTEPAD.EXE";

        IEnumerable<(int, string?)> Snapshot() =>
        [
            (5555, UpperCasePath)
        ];

        var pid = PackagedAppResolver.WaitForPackagedAppProcess(
            Pfn,
            TimeSpan.FromSeconds(1),
            processSnapshotProvider: Snapshot);

        Assert.Equal(5555, pid);
    }

    // ─── WaitForPackagedAppProcess — no match ────────────────────────────────

    [Fact]
    public void WaitForPackagedAppProcess_NoMatchingProcess_ReturnsZero()
    {
        const string Pfn = "Microsoft.WindowsNotepad_8wekyb3d8bbwe";
        const string UnrelatedPath = @"C:\Windows\System32\notepad.exe";  // not under WindowsApps

        IEnumerable<(int, string?)> Snapshot() =>
        [
            (1000, UnrelatedPath)
        ];

        var pid = PackagedAppResolver.WaitForPackagedAppProcess(
            Pfn,
            TimeSpan.FromMilliseconds(150), // very short timeout
            pollInterval: TimeSpan.FromMilliseconds(50),
            processSnapshotProvider: Snapshot);

        Assert.Equal(0, pid);
    }

    [Fact]
    public void WaitForPackagedAppProcess_EmptySnapshot_ReturnsZero()
    {
        const string Pfn = "Microsoft.WindowsNotepad_8wekyb3d8bbwe";

        IEnumerable<(int, string?)> EmptySnapshot() => [];

        var pid = PackagedAppResolver.WaitForPackagedAppProcess(
            Pfn,
            TimeSpan.FromMilliseconds(150),
            pollInterval: TimeSpan.FromMilliseconds(50),
            processSnapshotProvider: EmptySnapshot);

        Assert.Equal(0, pid);
    }

    [Fact]
    public void WaitForPackagedAppProcess_SkipsNullModulePaths()
    {
        // Processes where MainModulePath is null (access denied) must be skipped.
        const string Pfn = "Microsoft.WindowsNotepad_8wekyb3d8bbwe";

        IEnumerable<(int, string?)> Snapshot() =>
        [
            (2000, null),   // null = access denied
            (3000, null)    // another protected process
        ];

        var pid = PackagedAppResolver.WaitForPackagedAppProcess(
            Pfn,
            TimeSpan.FromMilliseconds(150),
            pollInterval: TimeSpan.FromMilliseconds(50),
            processSnapshotProvider: Snapshot);

        Assert.Equal(0, pid);
    }

    // ─── WaitForPackagedAppProcess — polling behaviour ────────────────────────

    [Fact]
    public void WaitForPackagedAppProcess_PollsMultipleTimes()
    {
        // The snapshot starts empty and returns a match on the second call.
        // This verifies the retry / polling loop fires more than once.
        const string Pfn = "Microsoft.WindowsCalculator_8wekyb3d8bbwe";
        const string MatchPath = @"C:\Program Files\WindowsApps\Microsoft.WindowsCalculator_8wekyb3d8bbwe_10.0_x64__8wekyb3d8bbwe\Calculator.exe";

        var callCount = 0;

        IEnumerable<(int, string?)> Snapshot()
        {
            callCount++;
            if (callCount < 2)
                return [];   // first call: no match yet

            return [(4242, MatchPath)];
        }

        var pid = PackagedAppResolver.WaitForPackagedAppProcess(
            Pfn,
            TimeSpan.FromSeconds(2),
            pollInterval: TimeSpan.FromMilliseconds(20),
            processSnapshotProvider: Snapshot);

        Assert.Equal(4242, pid);
        Assert.True(callCount >= 2, $"Expected at least 2 snapshot calls; got {callCount}");
    }

    [Fact]
    public void WaitForPackagedAppProcess_TimeoutExpires_ReturnsZero()
    {
        // The match never appears — method must return 0 after the timeout, not hang.
        const string Pfn = "Fake.Package_abc123";

        IEnumerable<(int, string?)> NeverMatches() => [(9999, @"C:\Windows\System32\cmd.exe")];

        var pid = PackagedAppResolver.WaitForPackagedAppProcess(
            Pfn,
            TimeSpan.FromMilliseconds(200),
            pollInterval: TimeSpan.FromMilliseconds(50),
            processSnapshotProvider: NeverMatches);

        Assert.Equal(0, pid);
    }

    // ─── WaitForPackagedAppProcess — does not match on partial PFN ───────────

    [Fact]
    public void WaitForPackagedAppProcess_PathWithSimilarButDifferentPfn_DoesNotMatch()
    {
        // "Microsoft.WindowsNotepadPro_8wekyb3d8bbwe" must NOT match a search for
        // "Microsoft.WindowsNotepad_8wekyb3d8bbwe" — the marker always ends with '_'.
        const string TargetPfn = "Microsoft.WindowsNotepad_8wekyb3d8bbwe";
        const string WrongPath = @"C:\Program Files\WindowsApps\Microsoft.WindowsNotepadPro_8wekyb3d8bbwe_1.0_x64__8wekyb3d8bbwe\app.exe";

        IEnumerable<(int, string?)> Snapshot() =>
        [
            (6666, WrongPath)
        ];

        var pid = PackagedAppResolver.WaitForPackagedAppProcess(
            TargetPfn,
            TimeSpan.FromMilliseconds(150),
            pollInterval: TimeSpan.FromMilliseconds(50),
            processSnapshotProvider: Snapshot);

        Assert.Equal(0, pid);
    }
}
