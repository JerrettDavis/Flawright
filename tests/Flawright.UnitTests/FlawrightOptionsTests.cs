using Xunit;

namespace Flawright.UnitTests;

/// <summary>Tests for <see cref="FlawrightOptions"/> defaults and construction.</summary>
public class FlawrightOptionsTests
{
    [Fact]
    public void DefaultTimeout_IsFiveSeconds()
    {
        var opts = new FlawrightOptions();
        Assert.Equal(TimeSpan.FromSeconds(5), opts.DefaultTimeout);
    }

    [Fact]
    public void DefaultRetryInterval_Is100Milliseconds()
    {
        var opts = new FlawrightOptions();
        Assert.Equal(TimeSpan.FromMilliseconds(100), opts.DefaultRetryInterval);
    }

    [Fact]
    public void ScreenshotDirectory_DefaultsToNull()
    {
        var opts = new FlawrightOptions();
        Assert.Null(opts.ScreenshotDirectory);
    }

    [Fact]
    public void CustomTimeout_IsPreserved()
    {
        var opts = new FlawrightOptions
        {
            DefaultTimeout = TimeSpan.FromSeconds(30)
        };
        Assert.Equal(TimeSpan.FromSeconds(30), opts.DefaultTimeout);
    }

    [Fact]
    public void CustomRetryInterval_IsPreserved()
    {
        var opts = new FlawrightOptions
        {
            DefaultRetryInterval = TimeSpan.FromMilliseconds(50)
        };
        Assert.Equal(TimeSpan.FromMilliseconds(50), opts.DefaultRetryInterval);
    }

    [Fact]
    public void CustomScreenshotDirectory_IsPreserved()
    {
        const string Dir = @"C:\TestOutput";
        var opts = new FlawrightOptions { ScreenshotDirectory = Dir };
        Assert.Equal(Dir, opts.ScreenshotDirectory);
    }
}

/// <summary>Tests for <see cref="LaunchOptions"/> and <see cref="AttachOptions"/>.</summary>
public class LaunchAttachOptionsTests
{
    [Fact]
    public void LaunchOptions_ApplicationPath_IsPreserved()
    {
        var opts = new LaunchOptions { ApplicationPath = "notepad.exe" };
        Assert.Equal("notepad.exe", opts.ApplicationPath);
    }

    [Fact]
    public void LaunchOptions_Arguments_IsPreserved()
    {
        var opts = new LaunchOptions
        {
            ApplicationPath = "app.exe",
            Arguments = ["--headless", "--no-sandbox"]
        };
        Assert.Equal(["--headless", "--no-sandbox"], opts.Arguments);
    }

    [Fact]
    public void LaunchOptions_WorkingDirectory_IsPreserved()
    {
        const string Dir = @"C:\MyApp";
        var opts = new LaunchOptions
        {
            ApplicationPath = "app.exe",
            WorkingDirectory = Dir
        };
        Assert.Equal(Dir, opts.WorkingDirectory);
    }

    [Fact]
    public void LaunchOptions_Arguments_DefaultsToNull()
    {
        var opts = new LaunchOptions { ApplicationPath = "app.exe" };
        Assert.Null(opts.Arguments);
    }

    [Fact]
    public void LaunchOptions_WorkingDirectory_DefaultsToNull()
    {
        var opts = new LaunchOptions { ApplicationPath = "app.exe" };
        Assert.Null(opts.WorkingDirectory);
    }

    [Fact]
    public void AttachOptions_ProcessId_IsPreserved()
    {
        var opts = new AttachOptions { ProcessId = 1234 };
        Assert.Equal(1234, opts.ProcessId);
    }

    [Fact]
    public void LaunchOptions_IsRecord_SupportsEquality()
    {
        var a = new LaunchOptions { ApplicationPath = "app.exe" };
        var b = new LaunchOptions { ApplicationPath = "app.exe" };
        Assert.Equal(a, b);
    }

    [Fact]
    public void AttachOptions_IsRecord_SupportsEquality()
    {
        var a = new AttachOptions { ProcessId = 42 };
        var b = new AttachOptions { ProcessId = 42 };
        Assert.Equal(a, b);
    }
}
