using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Browser;

/// <summary>
/// Unit tests for the <see cref="LaunchOptions"/> record.
/// Verifies default values and that all init-only properties are correctly
/// settable via with-expressions and object initializers.
/// </summary>
public sealed class LaunchOptionsTests
{
    [Fact]
    public void DefaultConstruction_AllPropertiesAreNull()
    {
        var opts = new LaunchOptions();

        Assert.Null(opts.ApplicationPath);
        Assert.Null(opts.Aumid);
        Assert.Null(opts.Arguments);
        Assert.Null(opts.WorkingDirectory);
        Assert.Null(opts.StartupTimeout);
    }

    [Fact]
    public void ApplicationPath_CanBeSet()
    {
        var opts = new LaunchOptions { ApplicationPath = @"C:\Windows\System32\notepad.exe" };

        Assert.Equal(@"C:\Windows\System32\notepad.exe", opts.ApplicationPath);
    }

    [Fact]
    public void Aumid_CanBeSet()
    {
        var opts = new LaunchOptions { Aumid = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App" };

        Assert.Equal("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", opts.Aumid);
    }

    [Fact]
    public void Arguments_CanBeSet()
    {
        var opts = new LaunchOptions { Arguments = ["--flag", "value"] };

        Assert.Equal(["--flag", "value"], opts.Arguments);
    }

    [Fact]
    public void WorkingDirectory_CanBeSet()
    {
        var opts = new LaunchOptions { WorkingDirectory = @"C:\Temp" };

        Assert.Equal(@"C:\Temp", opts.WorkingDirectory);
    }

    [Fact]
    public void StartupTimeout_CanBeSet()
    {
        var t = TimeSpan.FromSeconds(60);
        var opts = new LaunchOptions { StartupTimeout = t };

        Assert.Equal(t, opts.StartupTimeout);
    }

    [Fact]
    public void RecordEquality_SameReferenceArguments_AreEqual()
    {
        // Records use reference equality for arrays, so the same instance must be shared.
        var args = new[] { "a" };
        var a = new LaunchOptions
        {
            ApplicationPath = "notepad.exe",
            Arguments = args,
            WorkingDirectory = @"C:\Temp",
            StartupTimeout = TimeSpan.FromSeconds(10)
        };
        var b = new LaunchOptions
        {
            ApplicationPath = "notepad.exe",
            Arguments = args,
            WorkingDirectory = @"C:\Temp",
            StartupTimeout = TimeSpan.FromSeconds(10)
        };

        Assert.Equal(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var a = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var b = new LaunchOptions { ApplicationPath = "calc.exe" };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void WithExpression_ProducesUpdatedCopy()
    {
        var original = new LaunchOptions { ApplicationPath = "notepad.exe" };
        var modified = original with { StartupTimeout = TimeSpan.FromSeconds(5) };

        Assert.Equal("notepad.exe", modified.ApplicationPath);
        Assert.Equal(TimeSpan.FromSeconds(5), modified.StartupTimeout);
        // Original is unchanged.
        Assert.Null(original.StartupTimeout);
    }

    [Fact]
    public void IsRecord_SupportsDeconstruction()
    {
        // Records are sealed value-equality types — verify ToString doesn't throw.
        var opts = new LaunchOptions { ApplicationPath = "a.exe", Aumid = null };
        Assert.Contains("LaunchOptions", opts.ToString());
    }
}
