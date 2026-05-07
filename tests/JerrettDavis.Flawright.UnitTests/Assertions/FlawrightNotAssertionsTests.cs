using System.Text.RegularExpressions;
using JerrettDavis.Flawright.Assertions;
using JerrettDavis.Flawright.Selectors;
using NSubstitute;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Assertions;

/// <summary>
/// Tests for <see cref="FlawrightNotAssertions"/>, obtained via
/// <see cref="IFlawrightAssertions.Not"/>.
///
/// Each assertion's pass/fail condition is the inverse of the positive case.
/// </summary>
public sealed class FlawrightNotAssertionsTests
{
    private static readonly FlawrightOptions QuickOptions = new()
    {
        DefaultTimeout = TimeSpan.FromMilliseconds(50),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(10),
    };

    private static IFlawrightNotAssertions MakeNot(IFlawrightLocator locator)
        => new FlawrightAssertions(locator, QuickOptions).Not;

    private static IFlawrightLocator Locator(string selector = "test")
    {
        var loc = Substitute.For<IFlawrightLocator>();
        loc.Selector.Returns(selector);
        return loc;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToBeVisibleAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToBeVisibleAsync_Passes_WhenNotVisible()
    {
        var loc = Locator();
        loc.IsVisibleAsync(Arg.Any<CancellationToken>()).Returns(false);

        await MakeNot(loc).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Not_ToBeVisibleAsync_Fails_WhenVisible()
    {
        var loc = Locator();
        loc.IsVisibleAsync(Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToBeVisibleAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToBeHiddenAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToBeHiddenAsync_Passes_WhenNotHidden()
    {
        var loc = Locator();
        loc.IsHiddenAsync(Arg.Any<CancellationToken>()).Returns(false);

        await MakeNot(loc).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Not_ToBeHiddenAsync_Fails_WhenHidden()
    {
        var loc = Locator();
        loc.IsHiddenAsync(Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToBeHiddenAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToBeEnabledAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToBeEnabledAsync_Passes_WhenNotEnabled()
    {
        var loc = Locator();
        loc.IsEnabledAsync(Arg.Any<CancellationToken>()).Returns(false);

        await MakeNot(loc).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Not_ToBeEnabledAsync_Fails_WhenEnabled()
    {
        var loc = Locator();
        loc.IsEnabledAsync(Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToBeEnabledAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToBeDisabledAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToBeDisabledAsync_Passes_WhenNotDisabled()
    {
        var loc = Locator();
        loc.IsDisabledAsync(Arg.Any<CancellationToken>()).Returns(false);

        await MakeNot(loc).ToBeDisabledAsync();
    }

    [Fact]
    public async Task Not_ToBeDisabledAsync_Fails_WhenDisabled()
    {
        var loc = Locator();
        loc.IsDisabledAsync(Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToBeDisabledAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToBeCheckedAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToBeCheckedAsync_Passes_WhenNotChecked()
    {
        var loc = Locator();
        loc.IsCheckedAsync(Arg.Any<CancellationToken>()).Returns(false);

        await MakeNot(loc).ToBeCheckedAsync();
    }

    [Fact]
    public async Task Not_ToBeCheckedAsync_Fails_WhenChecked()
    {
        var loc = Locator();
        loc.IsCheckedAsync(Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToBeCheckedAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToBeFocusedAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToBeFocusedAsync_Passes_WhenNotFocused()
    {
        var loc = Locator();
        loc.GetAttributeAsync("focused", Arg.Any<CancellationToken>()).Returns("false");

        await MakeNot(loc).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Not_ToBeFocusedAsync_Fails_WhenFocused()
    {
        var loc = Locator();
        loc.GetAttributeAsync("focused", Arg.Any<CancellationToken>()).Returns("true");

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToBeFocusedAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToBeEditableAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToBeEditableAsync_Passes_WhenNotEditable()
    {
        var loc = Locator();
        loc.IsEditableAsync(Arg.Any<CancellationToken>()).Returns(false);

        await MakeNot(loc).ToBeEditableAsync();
    }

    [Fact]
    public async Task Not_ToBeEditableAsync_Fails_WhenEditable()
    {
        var loc = Locator();
        loc.IsEditableAsync(Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToBeEditableAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToBeEmptyAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToBeEmptyAsync_Passes_WhenHasValue()
    {
        var loc = Locator();
        loc.InputValueAsync(Arg.Any<CancellationToken>()).Returns("some value");
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("some value");

        await MakeNot(loc).ToBeEmptyAsync();
    }

    [Fact]
    public async Task Not_ToBeEmptyAsync_Fails_WhenEmpty()
    {
        var loc = Locator();
        loc.InputValueAsync(Arg.Any<CancellationToken>()).Returns((string?)null);
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns(string.Empty);

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToBeEmptyAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToBeAttachedAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToBeAttachedAsync_Passes_WhenNotAttached()
    {
        var loc = Locator();
        loc.CountAsync(Arg.Any<CancellationToken>()).Returns(0);

        await MakeNot(loc).ToBeAttachedAsync();
    }

    [Fact]
    public async Task Not_ToBeAttachedAsync_Fails_WhenAttached()
    {
        var loc = Locator();
        loc.CountAsync(Arg.Any<CancellationToken>()).Returns(1);

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToBeAttachedAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToHaveTextAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToHaveTextAsync_String_Passes_WhenTextDiffers()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Goodbye");

        await MakeNot(loc).ToHaveTextAsync("Hello World");
    }

    [Fact]
    public async Task Not_ToHaveTextAsync_String_Fails_WhenTextMatches()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Hello World");

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToHaveTextAsync("Hello World"));
    }

    [Fact]
    public async Task Not_ToHaveTextAsync_Regex_Passes_WhenPatternDoesNotMatch()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Goodbye");

        await MakeNot(loc).ToHaveTextAsync(new Regex(@"Hello \d+", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task Not_ToHaveTextAsync_Regex_Fails_WhenPatternMatches()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Hello 42");

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToHaveTextAsync(new Regex(@"Hello \d+", RegexOptions.None, TimeSpan.FromSeconds(1))));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToContainTextAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToContainTextAsync_String_Passes_WhenNotContains()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Hello World");

        await MakeNot(loc).ToContainTextAsync("Goodbye");
    }

    [Fact]
    public async Task Not_ToContainTextAsync_String_Fails_WhenContains()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Hello World");

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToContainTextAsync("World"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToHaveValueAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToHaveValueAsync_String_Passes_WhenValueDiffers()
    {
        var loc = Locator();
        loc.InputValueAsync(Arg.Any<CancellationToken>()).Returns("other");

        await MakeNot(loc).ToHaveValueAsync("myvalue");
    }

    [Fact]
    public async Task Not_ToHaveValueAsync_String_Fails_WhenValueMatches()
    {
        var loc = Locator();
        loc.InputValueAsync(Arg.Any<CancellationToken>()).Returns("myvalue");

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToHaveValueAsync("myvalue"));
    }

    [Fact]
    public async Task Not_ToHaveValueAsync_Regex_Passes_WhenPatternDoesNotMatch()
    {
        var loc = Locator();
        loc.InputValueAsync(Arg.Any<CancellationToken>()).Returns("xyz");

        await MakeNot(loc).ToHaveValueAsync(new Regex(@"^\d+$", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToHaveCountAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToHaveCountAsync_Passes_WhenCountDiffers()
    {
        var loc = Locator();
        loc.CountAsync(Arg.Any<CancellationToken>()).Returns(2);

        await MakeNot(loc).ToHaveCountAsync(3);
    }

    [Fact]
    public async Task Not_ToHaveCountAsync_Fails_WhenCountMatches()
    {
        var loc = Locator();
        loc.CountAsync(Arg.Any<CancellationToken>()).Returns(3);

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToHaveCountAsync(3));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToHaveAttributeAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToHaveAttributeAsync_String_Passes_WhenAttributeDiffers()
    {
        var loc = Locator();
        loc.GetAttributeAsync("state", Arg.Any<CancellationToken>()).Returns("inactive");

        await MakeNot(loc).ToHaveAttributeAsync("state", "active");
    }

    [Fact]
    public async Task Not_ToHaveAttributeAsync_String_Fails_WhenAttributeMatches()
    {
        var loc = Locator();
        loc.GetAttributeAsync("state", Arg.Any<CancellationToken>()).Returns("active");

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToHaveAttributeAsync("state", "active"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToHaveIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToHaveIdAsync_String_Passes_WhenIdDiffers()
    {
        var loc = Locator();
        loc.GetAttributeAsync("id", Arg.Any<CancellationToken>()).Returns("btn_cancel");

        await MakeNot(loc).ToHaveIdAsync("btn_ok");
    }

    [Fact]
    public async Task Not_ToHaveIdAsync_Regex_Passes_WhenPatternDoesNotMatch()
    {
        var loc = Locator();
        loc.GetAttributeAsync("id", Arg.Any<CancellationToken>()).Returns("nav_main");

        await MakeNot(loc).ToHaveIdAsync(new Regex(@"btn_\w+", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToHaveClassAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToHaveClassAsync_String_Passes_WhenClassDiffers()
    {
        var loc = Locator();
        loc.GetAttributeAsync("class", Arg.Any<CancellationToken>()).Returns("OtherClass");

        await MakeNot(loc).ToHaveClassAsync("MyClass");
    }

    [Fact]
    public async Task Not_ToHaveClassAsync_Regex_Passes_WhenPatternDoesNotMatch()
    {
        var loc = Locator();
        loc.GetAttributeAsync("class", Arg.Any<CancellationToken>()).Returns("NavBar");

        await MakeNot(loc).ToHaveClassAsync(new Regex(@"ToolBar_\w+", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToHaveRoleAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToHaveRoleAsync_Passes_WhenRoleDiffers()
    {
        var loc = Locator();
        loc.GetAttributeAsync("controltype", Arg.Any<CancellationToken>()).Returns("Edit");

        await MakeNot(loc).ToHaveRoleAsync(AriaRole.Button);
    }

    [Fact]
    public async Task Not_ToHaveRoleAsync_Fails_WhenRoleMatches()
    {
        var loc = Locator();
        loc.GetAttributeAsync("controltype", Arg.Any<CancellationToken>()).Returns("Button");

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToHaveRoleAsync(AriaRole.Button));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not.ToHaveAccessibleNameAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_ToHaveAccessibleNameAsync_String_Passes_WhenNameDiffers()
    {
        var loc = Locator();
        loc.GetAttributeAsync("name", Arg.Any<CancellationToken>()).Returns("Cancel");

        await MakeNot(loc).ToHaveAccessibleNameAsync("Submit");
    }

    [Fact]
    public async Task Not_ToHaveAccessibleNameAsync_Regex_Passes_WhenPatternDoesNotMatch()
    {
        var loc = Locator();
        loc.GetAttributeAsync("name", Arg.Any<CancellationToken>()).Returns("Cancel");

        await MakeNot(loc).ToHaveAccessibleNameAsync(new Regex(@"Save \w+", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not exception messages
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Not_AssertionException_ContainsNOT_InMessage()
    {
        var loc = Locator("#myBtn");
        loc.IsVisibleAsync(Arg.Any<CancellationToken>()).Returns(true);

        var ex = await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToBeVisibleAsync());

        Assert.Contains("NOT", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#myBtn", ex.Message);
    }
}
