using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Locator;

/// <summary>
/// Tests for <see cref="FlawrightElement.GetAttributeAsync"/> covering the
/// new switch arms added in the Hooks API expansion:
/// "selected", "isselected", "checked", "ischecked", "togglestate",
/// "frameworkid", "framework-id", "offscreen", "isoffscreen".
/// </summary>
public sealed class FlawrightElementAttributeTests
{
    private static FlawrightElement CreateElement(FakeElementBackend backend)
        => new(backend, new FakeInputBackend());

    // ── "selected" / "isselected" ─────────────────────────────────────────────

    [Fact]
    public async Task GetAttributeAsync_Selected_WhenSelected_ReturnsLowerCaseTrue()
    {
        var backend = UiaTree.RadioButton("R", initialState: true).Build();
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("selected");
        Assert.Equal("true", result);
    }

    [Fact]
    public async Task GetAttributeAsync_Selected_WhenNotSelected_ReturnsLowerCaseFalse()
    {
        var backend = UiaTree.RadioButton("R", initialState: false).Build();
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("selected");
        Assert.Equal("false", result);
    }

    [Fact]
    public async Task GetAttributeAsync_IsSelected_WhenSelected_ReturnsLowerCaseTrue()
    {
        var backend = UiaTree.RadioButton("R", initialState: true).Build();
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("isselected");
        Assert.Equal("true", result);
    }

    [Fact]
    public async Task GetAttributeAsync_Selected_WhenNoSelectionPattern_ReturnsNull()
    {
        // A plain Button has no SelectionItemPattern
        var backend = UiaTree.Button("OK").Build();
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("selected");
        Assert.Null(result);
    }

    // ── "checked" / "ischecked" / "togglestate" ───────────────────────────────

    [Fact]
    public async Task GetAttributeAsync_Checked_WhenChecked_ReturnsLowerCaseTrue()
    {
        var backend = UiaTree.CheckBox("CB", initialState: true).Build();
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("checked");
        Assert.Equal("true", result);
    }

    [Fact]
    public async Task GetAttributeAsync_Checked_WhenUnchecked_ReturnsLowerCaseFalse()
    {
        var backend = UiaTree.CheckBox("CB", initialState: false).Build();
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("checked");
        Assert.Equal("false", result);
    }

    [Fact]
    public async Task GetAttributeAsync_IsChecked_WhenChecked_ReturnsLowerCaseTrue()
    {
        var backend = UiaTree.CheckBox("CB", initialState: true).Build();
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("ischecked");
        Assert.Equal("true", result);
    }

    [Fact]
    public async Task GetAttributeAsync_ToggleState_WhenChecked_ReturnsLowerCaseTrue()
    {
        var backend = UiaTree.CheckBox("CB", initialState: true).Build();
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("togglestate");
        Assert.Equal("true", result);
    }

    [Fact]
    public async Task GetAttributeAsync_Checked_WhenNoTogglePattern_ReturnsNull()
    {
        // A plain Button has no TogglePattern
        var backend = UiaTree.Button("OK").Build();
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("checked");
        Assert.Null(result);
    }

    // ── "offscreen" / "isoffscreen" ───────────────────────────────────────────

    [Fact]
    public async Task GetAttributeAsync_Offscreen_WhenOffscreen_ReturnsTrue()
    {
        var backend = UiaTree.Button("Hidden").AsOffscreen().Build();
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("offscreen");
        Assert.Equal("true", result);
    }

    [Fact]
    public async Task GetAttributeAsync_Offscreen_WhenVisible_ReturnsFalse()
    {
        var backend = UiaTree.Button("Visible").Build();
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("offscreen");
        Assert.Equal("false", result);
    }

    [Fact]
    public async Task GetAttributeAsync_IsOffscreen_WhenOffscreen_ReturnsTrue()
    {
        var backend = UiaTree.Button("Hidden").AsOffscreen().Build();
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("isoffscreen");
        Assert.Equal("true", result);
    }

    // ── "frameworkid" / "framework-id" ───────────────────────────────────────

    [Fact]
    public async Task GetAttributeAsync_FrameworkId_ReturnsFrameworkId()
    {
        var backend = new FakeElementBackend(
            name: "MyBtn",
            controlTypeName: "Button");
        backend.FrameworkId = "WPF";
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("frameworkid");
        Assert.Equal("WPF", result);
    }

    [Fact]
    public async Task GetAttributeAsync_FrameworkIdWithDash_ReturnsFrameworkId()
    {
        var backend = new FakeElementBackend(
            name: "MyBtn",
            controlTypeName: "Button");
        backend.FrameworkId = "Win32";
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("framework-id");
        Assert.Equal("Win32", result);
    }

    [Fact]
    public async Task GetAttributeAsync_FrameworkId_WhenNull_ReturnsNull()
    {
        var backend = new FakeElementBackend(
            name: "MyBtn",
            controlTypeName: "Button");
        // FrameworkId defaults to null in FakeElementBackend
        var element = CreateElement(backend);

        var result = await element.GetAttributeAsync("frameworkid");
        Assert.Null(result);
    }
}
