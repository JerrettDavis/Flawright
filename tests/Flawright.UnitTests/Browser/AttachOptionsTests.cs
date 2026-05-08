using Xunit;

namespace Flawright.UnitTests.Browser;

/// <summary>
/// Unit tests for the <see cref="AttachOptions"/> record.
/// Verifies default values and that all init-only properties are correctly
/// settable via with-expressions and object initializers.
/// </summary>
public sealed class AttachOptionsTests
{
    [Fact]
    public void DefaultConstruction_ProcessIdIsNull()
    {
        var opts = new AttachOptions();

        Assert.Null(opts.ProcessId);
    }

    [Fact]
    public void DefaultConstruction_ProcessNameIsNull()
    {
        var opts = new AttachOptions();

        Assert.Null(opts.ProcessName);
    }

    [Fact]
    public void DefaultConstruction_IndexIsZero()
    {
        var opts = new AttachOptions();

        Assert.Equal(0, opts.Index);
    }

    [Fact]
    public void ProcessId_CanBeSet()
    {
        var opts = new AttachOptions { ProcessId = 12345 };

        Assert.Equal(12345, opts.ProcessId);
    }

    [Fact]
    public void ProcessName_CanBeSet()
    {
        var opts = new AttachOptions { ProcessName = "notepad" };

        Assert.Equal("notepad", opts.ProcessName);
    }

    [Fact]
    public void ProcessName_AcceptsExeSuffix()
    {
        var opts = new AttachOptions { ProcessName = "notepad.exe" };

        Assert.Equal("notepad.exe", opts.ProcessName);
    }

    [Fact]
    public void Index_CanBeSet()
    {
        var opts = new AttachOptions { ProcessName = "notepad", Index = 3 };

        Assert.Equal(3, opts.Index);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var a = new AttachOptions { ProcessId = 1, ProcessName = null, Index = 0 };
        var b = new AttachOptions { ProcessId = 1, ProcessName = null, Index = 0 };

        Assert.Equal(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentPid_AreNotEqual()
    {
        var a = new AttachOptions { ProcessId = 1 };
        var b = new AttachOptions { ProcessId = 2 };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void WithExpression_ProducesUpdatedCopy()
    {
        var original = new AttachOptions { ProcessName = "notepad" };
        var modified = original with { Index = 5 };

        Assert.Equal("notepad", modified.ProcessName);
        Assert.Equal(5, modified.Index);
        // Original is unchanged.
        Assert.Equal(0, original.Index);
    }

    [Fact]
    public void IsRecord_SupportsToString()
    {
        var opts = new AttachOptions { ProcessId = 42 };
        Assert.Contains("AttachOptions", opts.ToString());
    }
}
