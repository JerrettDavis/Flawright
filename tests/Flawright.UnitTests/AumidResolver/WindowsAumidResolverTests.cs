using Flawright.AumidResolver;
using Xunit;

namespace Flawright.UnitTests.AumidResolver;

/// <summary>
/// Unit tests for <see cref="WindowsAumidResolver"/>.
///
/// All tests inject fake <c>windowsAppsDir</c>, <c>fileExists</c>, and
/// <c>packageFamilyInstalled</c> delegates so they never touch the real file
/// system or registry.
/// </summary>
public sealed class WindowsAumidResolverTests
{
    private const string FakeWindowsApps = @"C:\FakeLocalAppData\Microsoft\WindowsApps";

    // ─── Tier 1: WindowsApps alias stub ──────────────────────────────────────

    [Theory]
    [InlineData("notepad.exe", "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App")]
    [InlineData("calc.exe", "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App")]
    [InlineData("mspaint.exe", "Microsoft.Paint_8wekyb3d8bbwe!App")]
    public void Resolve_AliasStubInWindowsApps_ReturnsAumid(string alias, string expectedAumid)
    {
        var aliasPath = Path.Combine(FakeWindowsApps, alias);

        // Stub exists in WindowsApps; package check not needed.
        var resolver = new WindowsAumidResolver(
            windowsAppsDir: FakeWindowsApps,
            fileExists: p => string.Equals(p, aliasPath, StringComparison.OrdinalIgnoreCase),
            packageFamilyInstalled: _ => false);

        var target = resolver.Resolve(aliasPath);

        Assert.Equal(LaunchKind.Aumid, target.Kind);
        Assert.Equal(expectedAumid, target.Value);
    }

    [Fact]
    public void Resolve_AliasStubInWindowsApps_BareNameWithWindowsAppsPath_ReturnsAumid()
    {
        // Bare "notepad.exe" resolved to WindowsApps path
        var aliasPath = Path.Combine(FakeWindowsApps, "notepad.exe");

        var resolver = new WindowsAumidResolver(
            windowsAppsDir: FakeWindowsApps,
            fileExists: p => string.Equals(p, aliasPath, StringComparison.OrdinalIgnoreCase),
            packageFamilyInstalled: _ => false);

        var target = resolver.Resolve(aliasPath);

        Assert.Equal(LaunchKind.Aumid, target.Kind);
        Assert.Equal("Microsoft.WindowsNotepad_8wekyb3d8bbwe!App", target.Value);
    }

    // ─── Tier 2: Registry / installed-package fallback ───────────────────────

    [Theory]
    [InlineData("calc.exe", "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App")]
    [InlineData("notepad.exe", "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App")]
    [InlineData("mspaint.exe", "Microsoft.Paint_8wekyb3d8bbwe!App")]
    public void Resolve_NoAliasStub_PackageInstalled_ReturnsAumid(string alias, string expectedAumid)
    {
        // Alias stub does NOT exist in WindowsApps (fileExists always false)
        // but the package is registered.
        var resolver = new WindowsAumidResolver(
            windowsAppsDir: FakeWindowsApps,
            fileExists: _ => false,
            packageFamilyInstalled: _ => true);

        var target = resolver.Resolve(alias);

        Assert.Equal(LaunchKind.Aumid, target.Kind);
        Assert.Equal(expectedAumid, target.Value);
    }

    [Fact]
    public void Resolve_CalcExeFromSystem32_PackageInstalled_ReturnsAumid()
    {
        // calc.exe resolved from C:\Windows\System32 — not under WindowsApps.
        const string System32Calc = @"C:\Windows\System32\calc.exe";

        var resolver = new WindowsAumidResolver(
            windowsAppsDir: FakeWindowsApps,
            fileExists: p => string.Equals(p, System32Calc, StringComparison.OrdinalIgnoreCase),
            packageFamilyInstalled: _ => true);

        var target = resolver.Resolve(System32Calc);

        Assert.Equal(LaunchKind.Aumid, target.Kind);
        Assert.Equal("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", target.Value);
    }

    [Fact]
    public void Resolve_NoAliasStub_PackageNotInstalled_ReturnsPath()
    {
        // The app is in the known-alias table but the package is NOT installed.
        // Should fall through to normal Win32 launch.
        const string BareName = "calc.exe";

        var resolver = new WindowsAumidResolver(
            windowsAppsDir: FakeWindowsApps,
            fileExists: _ => false,
            packageFamilyInstalled: _ => false);

        var target = resolver.Resolve(BareName);

        Assert.Equal(LaunchKind.Path, target.Kind);
        Assert.Equal(BareName, target.Value);
    }

    // ─── Non-alias paths ─────────────────────────────────────────────────────

    [Fact]
    public void Resolve_UnknownExecutable_ReturnsPath()
    {
        const string App = @"C:\MyApp\myapp.exe";

        var resolver = new WindowsAumidResolver(
            windowsAppsDir: FakeWindowsApps,
            fileExists: _ => false,
            packageFamilyInstalled: _ => false);

        var target = resolver.Resolve(App);

        Assert.Equal(LaunchKind.Path, target.Kind);
        Assert.Equal(App, target.Value);
    }

    [Theory]
    [InlineData("regedit.exe")]
    [InlineData(@"C:\Windows\System32\mmc.exe")]
    [InlineData(@"C:\Program Files\MyApp\app.exe")]
    public void Resolve_StandardWin32Exe_ReturnsPath(string path)
    {
        var resolver = new WindowsAumidResolver(
            windowsAppsDir: FakeWindowsApps,
            fileExists: _ => false,
            packageFamilyInstalled: _ => false);

        var target = resolver.Resolve(path);

        Assert.Equal(LaunchKind.Path, target.Kind);
        Assert.Equal(path, target.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_NullOrWhitespace_ReturnsPath(string path)
    {
        var resolver = new WindowsAumidResolver(
            windowsAppsDir: FakeWindowsApps,
            fileExists: _ => false,
            packageFamilyInstalled: _ => false);

        var target = resolver.Resolve(path);

        Assert.Equal(LaunchKind.Path, target.Kind);
        Assert.Equal(path, target.Value);
    }

    // ─── Case-insensitivity ───────────────────────────────────────────────────

    [Theory]
    [InlineData("NOTEPAD.EXE")]
    [InlineData("Notepad.Exe")]
    [InlineData("CALC.EXE")]
    public void Resolve_KnownAlias_CaseInsensitive(string alias)
    {
        // Package is installed; alias stub does not matter here.
        var resolver = new WindowsAumidResolver(
            windowsAppsDir: FakeWindowsApps,
            fileExists: _ => false,
            packageFamilyInstalled: _ => true);

        var target = resolver.Resolve(alias);

        Assert.Equal(LaunchKind.Aumid, target.Kind);
        Assert.False(string.IsNullOrEmpty(target.Value));
    }

    // ─── PackageFamilyName extraction ─────────────────────────────────────────

    [Fact]
    public void Resolve_CalcExe_PackageFamilyNamePassedToInstalledCheck()
    {
        // Verify the correct PackageFamilyName is passed to the registry check.
        string? capturedPfn = null;

        var resolver = new WindowsAumidResolver(
            windowsAppsDir: FakeWindowsApps,
            fileExists: _ => false,
            packageFamilyInstalled: pfn =>
            {
                capturedPfn = pfn;
                return true;
            });

        resolver.Resolve("calc.exe");

        Assert.Equal("Microsoft.WindowsCalculator_8wekyb3d8bbwe", capturedPfn);
    }

    [Fact]
    public void Resolve_NotepadExe_PackageFamilyNamePassedToInstalledCheck()
    {
        string? capturedPfn = null;

        var resolver = new WindowsAumidResolver(
            windowsAppsDir: FakeWindowsApps,
            fileExists: _ => false,
            packageFamilyInstalled: pfn =>
            {
                capturedPfn = pfn;
                return true;
            });

        resolver.Resolve("notepad.exe");

        Assert.Equal("Microsoft.WindowsNotepad_8wekyb3d8bbwe", capturedPfn);
    }
}
