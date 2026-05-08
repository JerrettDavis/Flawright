using Flawright.AumidResolver;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.AumidResolver;

/// <summary>
/// Unit tests for <see cref="FakeAumidResolver"/> — the test double used in
/// browser and integration tests.
/// </summary>
public sealed class FakeAumidResolverTests
{
    [Fact]
    public void Resolve_UnregisteredPath_ReturnsPathTarget()
    {
        var fake = new FakeAumidResolver();
        var target = fake.Resolve("myapp.exe");

        Assert.Equal(LaunchKind.Path, target.Kind);
        Assert.Equal("myapp.exe", target.Value);
    }

    [Fact]
    public void Resolve_RegisteredAumid_ReturnsAumidTarget()
    {
        var fake = new FakeAumidResolver();
        fake.RegisterAumid("notepad.exe", "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App");

        var target = fake.Resolve("notepad.exe");

        Assert.Equal(LaunchKind.Aumid, target.Kind);
        Assert.Equal("Microsoft.WindowsNotepad_8wekyb3d8bbwe!App", target.Value);
    }

    [Fact]
    public void Resolve_RecordsAllCalls()
    {
        var fake = new FakeAumidResolver();

        fake.Resolve("app1.exe");
        fake.Resolve("app2.exe");
        fake.Resolve("app3.exe");

        Assert.Equal(["app1.exe", "app2.exe", "app3.exe"], fake.ResolveCalls);
    }

    [Fact]
    public void Register_OverwritesPreviousRegistration()
    {
        var fake = new FakeAumidResolver();
        fake.RegisterAumid("notepad.exe", "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App");
        fake.RegisterAumid("notepad.exe", "SomeOtherAumid");

        var target = fake.Resolve("notepad.exe");

        Assert.Equal("SomeOtherAumid", target.Value);
    }

    [Fact]
    public void Resolve_CaseInsensitive()
    {
        var fake = new FakeAumidResolver();
        fake.RegisterAumid("notepad.exe", "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App");

        var target = fake.Resolve("NOTEPAD.EXE");

        Assert.Equal(LaunchKind.Aumid, target.Kind);
    }
}
