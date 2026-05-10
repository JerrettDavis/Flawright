using Flawright.Reqnroll;
using Xunit;

namespace Flawright.UnitTests.Reqnroll;

/// <summary>
/// Unit tests for <see cref="FlawrightLaunchConfig"/> — verifying the shape of
/// configs produced by <see cref="TagParser"/> and the properties used by
/// <see cref="FlawrightReqnrollHooks.TeardownAsync"/> to decide whether to skip
/// <c>CloseAsync</c>.
/// </summary>
public sealed class FlawrightLaunchConfigTests
{
    // ── "Was attached?" detection — mirrors TeardownAsync condition ───────────

    [Fact]
    public void AttachPidConfig_IsDetectedAsAttached()
    {
        var config = new FlawrightLaunchConfig { AttachProcessId = 1234 };

        var wasAttached = config.AttachProcessId.HasValue
            || config.AttachProcessName is { Length: > 0 };

        Assert.True(wasAttached);
    }

    [Fact]
    public void AttachNameConfig_IsDetectedAsAttached()
    {
        var config = new FlawrightLaunchConfig { AttachProcessName = "notepad" };

        var wasAttached = config.AttachProcessId.HasValue
            || config.AttachProcessName is { Length: > 0 };

        Assert.True(wasAttached);
    }

    [Fact]
    public void LaunchPathConfig_IsNotDetectedAsAttached()
    {
        var config = new FlawrightLaunchConfig { ApplicationPath = "notepad.exe" };

        var wasAttached = config.AttachProcessId.HasValue
            || config.AttachProcessName is { Length: > 0 };

        Assert.False(wasAttached);
    }

    [Fact]
    public void AumidConfig_IsNotDetectedAsAttached()
    {
        var config = new FlawrightLaunchConfig { Aumid = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App" };

        var wasAttached = config.AttachProcessId.HasValue
            || config.AttachProcessName is { Length: > 0 };

        Assert.False(wasAttached);
    }

    [Fact]
    public void DefaultConfig_IsNotDetectedAsAttached()
    {
        var config = new FlawrightLaunchConfig();

        var wasAttached = config.AttachProcessId.HasValue
            || config.AttachProcessName is { Length: > 0 };

        Assert.False(wasAttached);
    }

    // ── TagParser round-trip: attach tags produce attach config ───────────────

    [Fact]
    public void TagParser_AttachPidTag_ProducesAttachedConfig()
    {
        var config = TagParser.Parse(["attachpid:9999"]);

        var wasAttached = config.AttachProcessId.HasValue
            || config.AttachProcessName is { Length: > 0 };

        Assert.True(wasAttached);
    }

    [Fact]
    public void TagParser_AttachNameTag_ProducesAttachedConfig()
    {
        var config = TagParser.Parse(["attach:notepad"]);

        var wasAttached = config.AttachProcessId.HasValue
            || config.AttachProcessName is { Length: > 0 };

        Assert.True(wasAttached);
    }

    [Fact]
    public void TagParser_LaunchTag_ProducesNonAttachedConfig()
    {
        var config = TagParser.Parse(["launch:notepad.exe"]);

        var wasAttached = config.AttachProcessId.HasValue
            || config.AttachProcessName is { Length: > 0 };

        Assert.False(wasAttached);
    }
}
