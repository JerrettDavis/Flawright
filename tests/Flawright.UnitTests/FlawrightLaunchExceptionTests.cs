using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests;

/// <summary>Tests for <see cref="FlawrightLaunchException"/>.</summary>
public class FlawrightLaunchExceptionTests
{
    // ── Constructor tests ─────────────────────────────────────────────────────

    [Fact]
    public void DefaultCtor_CreatesInstance()
    {
        var ex = new FlawrightLaunchException();

        Assert.NotNull(ex);
        Assert.Null(ex.OriginalPath);
        Assert.Null(ex.ResolvedPath);
        Assert.Null(ex.ElapsedMs);
    }

    [Fact]
    public void MessageCtor_PreservesMessage()
    {
        const string Msg = "Failed to launch application stub";

        var ex = new FlawrightLaunchException(Msg);

        Assert.Equal(Msg, ex.Message);
        Assert.Null(ex.OriginalPath);
        Assert.Null(ex.ResolvedPath);
        Assert.Null(ex.ElapsedMs);
    }

    [Fact]
    public void MessageInnerCtor_PreservesMessageAndInnerException()
    {
        var inner = new InvalidOperationException("underlying FlaUI error");
        const string Msg = "Wrapper message";

        var ex = new FlawrightLaunchException(Msg, inner);

        Assert.Equal(Msg, ex.Message);
        Assert.NotNull(ex.InnerException);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void PathElapsedCtor_InnerExceptionIsNotNull_WhenProvided()
    {
        var inner = new InvalidOperationException("process not running");

        var ex = new FlawrightLaunchException("calc.exe", "calc.exe", 250, inner);

        Assert.NotNull(ex.InnerException);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void PathElapsedCtor_InnerExceptionIsNull_WhenNotProvided()
    {
        var ex = new FlawrightLaunchException("calc.exe", "calc.exe", 250);

        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void PathElapsedCtor_ExposesOriginalPath()
    {
        var ex = new FlawrightLaunchException("calc.exe", @"C:\Windows\System32\calc.exe", 300);

        Assert.Equal("calc.exe", ex.OriginalPath);
    }

    [Fact]
    public void PathElapsedCtor_ExposesResolvedPath()
    {
        var ex = new FlawrightLaunchException("calc.exe", @"C:\Windows\System32\calc.exe", 300);

        Assert.Equal(@"C:\Windows\System32\calc.exe", ex.ResolvedPath);
    }

    [Fact]
    public void PathElapsedCtor_ExposesElapsedMs()
    {
        var ex = new FlawrightLaunchException("calc.exe", "calc.exe", 350);

        Assert.Equal(350, ex.ElapsedMs);
    }

    [Fact]
    public void PathElapsedCtor_MessageContainsOriginalPath()
    {
        const string OriginalPath = "calc.exe";

        var ex = new FlawrightLaunchException(OriginalPath, OriginalPath, 200);

        Assert.Contains(OriginalPath, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PathElapsedCtor_MessageContainsResolvedPath_WhenDifferentFromOriginal()
    {
        const string Original = "calc.exe";
        const string Resolved = @"C:\Windows\System32\calc.exe";

        var ex = new FlawrightLaunchException(Original, Resolved, 200);

        Assert.Contains(Resolved, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PathElapsedCtor_MessageContainsElapsedMs()
    {
        var ex = new FlawrightLaunchException("calc.exe", "calc.exe", 375);

        Assert.Contains("375", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PathElapsedCtor_MessageMentionsWinget()
    {
        var ex = new FlawrightLaunchException("calc.exe", "calc.exe", 200);

        Assert.Contains("winget", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsSubclassOfException()
    {
        var ex = new FlawrightLaunchException("msg");

        Assert.IsAssignableFrom<Exception>(ex);
    }

    [Fact]
    public void CanBeCaughtAsException()
    {
        FlawrightLaunchException? caught = null;

        try
        {
            throw new FlawrightLaunchException("calc.exe", "calc.exe", 200);
        }
        catch (Exception e)
        {
            caught = e as FlawrightLaunchException;
        }

        Assert.NotNull(caught);
        Assert.Equal("calc.exe", caught!.OriginalPath);
    }

    // ── FakeApplicationLauncher seam tests ────────────────────────────────────

    [Fact]
    public async Task FlawrightBrowser_PropagatesLaunchException_FromLauncher()
    {
        // Arrange: configure the fake launcher to throw FlawrightLaunchException
        // on Launch(). This simulates the broker-stub-exits path.
        var inner = new InvalidOperationException("Process with an Id of 9999 is not running.");
        var launchEx = new FlawrightLaunchException(
            "calc.exe", "calc.exe", 250, inner);

        var launcher = new FakeApplicationLauncher { ThrowOnLaunch = launchEx };
        var input = new FakeInputBackend();
        var translator = new FakeConditionTranslator();
        var opts = new LaunchOptions { ApplicationPath = "calc.exe" };
        var fwOpts = new FlawrightOptions
        {
            DefaultTimeout = TimeSpan.FromMilliseconds(200),
            DefaultRetryInterval = TimeSpan.FromMilliseconds(10),
        };
        var browser = new FlawrightBrowser(launcher, input, translator, opts, fwOpts);

        // Act + Assert: the FlawrightLaunchException should propagate out of
        // EnsureInitializedAsync unchanged.
        var thrown = await Assert.ThrowsAsync<FlawrightLaunchException>(
            () => browser.EnsureInitializedAsync());

        Assert.Same(launchEx, thrown);
        Assert.NotNull(thrown.InnerException);
        Assert.Equal("calc.exe", thrown.OriginalPath);
        Assert.Equal(250, thrown.ElapsedMs);
    }
}
