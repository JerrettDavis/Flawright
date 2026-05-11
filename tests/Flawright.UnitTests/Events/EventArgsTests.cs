using Xunit;

namespace Flawright.UnitTests.Events;

/// <summary>
/// Tests that verify the EventArgs constructors for new event types added in
/// the Hooks API expansion. Covers property initialization for
/// AppExecutionAliasResolvedEventArgs, ProcessAttachRetriedEventArgs, and
/// ProcessReadyGuardWaitedEventArgs.
/// </summary>
public sealed class EventArgsTests
{
    // ── AppExecutionAliasResolvedEventArgs ────────────────────────────────────

    [Fact]
    public void AppExecutionAliasResolvedEventArgs_StoresAllProperties()
    {
        var args = new AppExecutionAliasResolvedEventArgs(
            originalPath: @"C:\Windows\System32\calc.exe",
            resolvedAumid: "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",
            packageFamilyName: "Microsoft.WindowsCalculator_8wekyb3d8bbwe");

        Assert.Equal(@"C:\Windows\System32\calc.exe", args.OriginalPath);
        Assert.Equal("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", args.ResolvedAumid);
        Assert.Equal("Microsoft.WindowsCalculator_8wekyb3d8bbwe", args.PackageFamilyName);
    }

    [Fact]
    public void AppExecutionAliasResolvedEventArgs_IsEventArgs()
    {
        var args = new AppExecutionAliasResolvedEventArgs("path", "aumid", "pfn");
        Assert.IsAssignableFrom<EventArgs>(args);
    }

    // ── ProcessAttachRetriedEventArgs ─────────────────────────────────────────

    [Fact]
    public void ProcessAttachRetriedEventArgs_StoresAllProperties()
    {
        var args = new ProcessAttachRetriedEventArgs(
            attemptNumber: 2,
            delayMs: 40,
            win32ErrorCode: 299);

        Assert.Equal(2, args.AttemptNumber);
        Assert.Equal(40, args.DelayMs);
        Assert.Equal(299, args.Win32ErrorCode);
    }

    [Fact]
    public void ProcessAttachRetriedEventArgs_IsEventArgs()
    {
        var args = new ProcessAttachRetriedEventArgs(1, 10, 299);
        Assert.IsAssignableFrom<EventArgs>(args);
    }

    // ── ProcessReadyGuardWaitedEventArgs ──────────────────────────────────────

    [Fact]
    public void ProcessReadyGuardWaitedEventArgs_StoresAllProperties()
    {
        var args = new ProcessReadyGuardWaitedEventArgs(
            processId: 12345,
            elapsedMs: 350,
            modulesProbeRetries: 5,
            mainModuleProbeRetries: 2);

        Assert.Equal(12345, args.ProcessId);
        Assert.Equal(350, args.ElapsedMs);
        Assert.Equal(5, args.ModulesProbeRetries);
        Assert.Equal(2, args.MainModuleProbeRetries);
    }

    [Fact]
    public void ProcessReadyGuardWaitedEventArgs_IsEventArgs()
    {
        var args = new ProcessReadyGuardWaitedEventArgs(1, 0, 0, 0);
        Assert.IsAssignableFrom<EventArgs>(args);
    }

    // ── WindowDetectedEventArgs ───────────────────────────────────────────────

    [Fact]
    public void WindowDetectedEventArgs_StoresAllProperties()
    {
        var args = new WindowDetectedEventArgs(
            windowHandle: 12345,
            title: "My Window",
            processId: 9999);

        Assert.Equal(12345, args.WindowHandle);
        Assert.Equal("My Window", args.Title);
        Assert.Equal(9999, args.ProcessId);
    }
}
