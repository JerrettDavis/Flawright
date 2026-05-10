using Flawright.Reqnroll;
using Xunit;

namespace Flawright.UnitTests.Reqnroll;

/// <summary>
/// Unit tests for <see cref="TagParser"/>.
/// </summary>
public sealed class TagParserTests
{
    // ── Launch tags ───────────────────────────────────────────────────────────

    [Fact]
    public void Parse_LaunchTag_SetsApplicationPath()
    {
        var config = TagParser.Parse(["launch:notepad.exe"]);

        Assert.Equal("notepad.exe", config.ApplicationPath);
        Assert.Null(config.Aumid);
        Assert.Null(config.AttachProcessName);
        Assert.Null(config.AttachProcessId);
    }

    [Fact]
    public void Parse_AumidTag_SetsAumid()
    {
        var config = TagParser.Parse(["aumid:Microsoft.WindowsCalculator_8wekyb3d8bbwe!App"]);

        Assert.Equal("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", config.Aumid);
        Assert.Null(config.ApplicationPath);
    }

    // ── Attach-by-name tags ───────────────────────────────────────────────────

    [Fact]
    public void Parse_AttachTag_SetsAttachProcessName()
    {
        var config = TagParser.Parse(["attach:notepad"]);

        Assert.Equal("notepad", config.AttachProcessName);
        Assert.Null(config.AttachProcessId);
    }

    [Fact]
    public void Parse_AttachTag_CaseInsensitive()
    {
        var config = TagParser.Parse(["ATTACH:Notepad"]);

        Assert.Equal("Notepad", config.AttachProcessName);
    }

    // ── Attach-by-PID tags ────────────────────────────────────────────────────

    [Fact]
    public void Parse_AttachPidTag_SetsAttachProcessId()
    {
        var config = TagParser.Parse(["attachpid:12345"]);

        Assert.Equal(12345, config.AttachProcessId);
        Assert.Null(config.AttachProcessName);
    }

    [Fact]
    public void Parse_AttachPidTag_DoesNotPopulateAttachProcessName()
    {
        // Key guard: "attachpid:" must NOT match the "attach:" prefix path.
        // This test would fail before the redundant guard was removed if the
        // guard logic had been inverted or the else-if chain were wrong.
        var config = TagParser.Parse(["attachpid:999"]);

        Assert.Equal(999, config.AttachProcessId);
        Assert.Null(config.AttachProcessName);
    }

    [Fact]
    public void Parse_AttachPidTag_InvalidNumber_DoesNotSetId()
    {
        var config = TagParser.Parse(["attachpid:notanumber"]);

        Assert.Null(config.AttachProcessId);
    }

    // ── Priority and first-match semantics ────────────────────────────────────

    [Fact]
    public void Parse_MultipleAttachPidTags_UsesFirst()
    {
        var config = TagParser.Parse(["attachpid:1", "attachpid:2"]);

        Assert.Equal(1, config.AttachProcessId);
    }

    [Fact]
    public void Parse_MultipleAttachTags_UsesFirst()
    {
        var config = TagParser.Parse(["attach:first", "attach:second"]);

        Assert.Equal("first", config.AttachProcessName);
    }

    [Fact]
    public void Parse_MultipleLaunchTags_UsesFirst()
    {
        var config = TagParser.Parse(["launch:first.exe", "launch:second.exe"]);

        Assert.Equal("first.exe", config.ApplicationPath);
    }

    // ── Mixed tag sets ────────────────────────────────────────────────────────

    [Fact]
    public void Parse_BothAttachPidAndAttachName_BothSet()
    {
        // Both can co-exist in tags; the hook uses priority order at launch time.
        var config = TagParser.Parse(["attachpid:42", "attach:notepad"]);

        Assert.Equal(42, config.AttachProcessId);
        Assert.Equal("notepad", config.AttachProcessName);
    }

    [Fact]
    public void Parse_EmptyTags_ReturnsDefaultConfig()
    {
        var config = TagParser.Parse([]);

        Assert.Null(config.ApplicationPath);
        Assert.Null(config.Aumid);
        Assert.Null(config.AttachProcessName);
        Assert.Null(config.AttachProcessId);
    }

    [Fact]
    public void Parse_UnrecognisedTags_AreIgnored()
    {
        var config = TagParser.Parse(["smoke", "regression", "jira:ABC-123"]);

        Assert.Null(config.ApplicationPath);
        Assert.Null(config.Aumid);
        Assert.Null(config.AttachProcessName);
        Assert.Null(config.AttachProcessId);
    }
}
