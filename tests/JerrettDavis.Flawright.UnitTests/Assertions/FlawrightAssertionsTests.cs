using System.Text.RegularExpressions;
using JerrettDavis.Flawright.Assertions;
using JerrettDavis.Flawright.Selectors;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Assertions;

/// <summary>
/// Comprehensive tests for <see cref="FlawrightAssertions"/>.
///
/// Uses NSubstitute to mock <see cref="IFlawrightLocator"/> — no real UIA tree required.
/// For each assertion:
///   - Passing: locator returns the "match" value → assertion completes.
///   - Failing: locator returns the "no match" value → AssertionException thrown.
///   - Timeout: per-call timeout option is respected.
///   - Default: null options use FlawrightOptions.DefaultTimeout.
/// </summary>
public sealed class FlawrightAssertionsTests
{
    private static readonly FlawrightOptions QuickOptions = new()
    {
        DefaultTimeout = TimeSpan.FromMilliseconds(50),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(10),
    };

    private static FlawrightAssertions Make(IFlawrightLocator locator, bool negated = false)
        => negated
            ? new FlawrightAssertions(locator, QuickOptions, negated: true)
            : new FlawrightAssertions(locator, QuickOptions);

    private static IFlawrightLocator Locator(string selector = "test")
    {
        var loc = Substitute.For<IFlawrightLocator>();
        loc.Selector.Returns(selector);
        return loc;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToBeVisibleAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToBeVisibleAsync_Passes_WhenVisible()
    {
        var loc = Locator();
        loc.IsVisibleAsync(Arg.Any<CancellationToken>()).Returns(true);

        await Make(loc).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ToBeVisibleAsync_Fails_WhenNotVisible()
    {
        var loc = Locator();
        loc.IsVisibleAsync(Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeVisibleAsync());
    }

    [Fact]
    public async Task ToBeVisibleAsync_RespectsPerCallTimeout()
    {
        var loc = Locator();
        loc.IsVisibleAsync(Arg.Any<CancellationToken>()).Returns(false);

        var opts = new AssertionsToBeVisibleOptions { Timeout = TimeSpan.FromMilliseconds(30) };
        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeVisibleAsync(opts));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToBeHiddenAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToBeHiddenAsync_Passes_WhenHidden()
    {
        var loc = Locator();
        loc.IsHiddenAsync(Arg.Any<CancellationToken>()).Returns(true);

        await Make(loc).ToBeHiddenAsync();
    }

    [Fact]
    public async Task ToBeHiddenAsync_Fails_WhenNotHidden()
    {
        var loc = Locator();
        loc.IsHiddenAsync(Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeHiddenAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToBeEnabledAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToBeEnabledAsync_Passes_WhenEnabled()
    {
        var loc = Locator();
        loc.IsEnabledAsync(Arg.Any<CancellationToken>()).Returns(true);

        await Make(loc).ToBeEnabledAsync();
    }

    [Fact]
    public async Task ToBeEnabledAsync_Fails_WhenDisabled()
    {
        var loc = Locator();
        loc.IsEnabledAsync(Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeEnabledAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToBeDisabledAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToBeDisabledAsync_Passes_WhenDisabled()
    {
        var loc = Locator();
        loc.IsDisabledAsync(Arg.Any<CancellationToken>()).Returns(true);

        await Make(loc).ToBeDisabledAsync();
    }

    [Fact]
    public async Task ToBeDisabledAsync_Fails_WhenEnabled()
    {
        var loc = Locator();
        loc.IsDisabledAsync(Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeDisabledAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToBeCheckedAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToBeCheckedAsync_Passes_WhenChecked()
    {
        var loc = Locator();
        loc.IsCheckedAsync(Arg.Any<CancellationToken>()).Returns(true);

        await Make(loc).ToBeCheckedAsync();
    }

    [Fact]
    public async Task ToBeCheckedAsync_Fails_WhenUnchecked()
    {
        var loc = Locator();
        loc.IsCheckedAsync(Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeCheckedAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToBeFocusedAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToBeFocusedAsync_Passes_WhenFocusedAttributeIsTrue()
    {
        var loc = Locator();
        loc.GetAttributeAsync("focused", Arg.Any<CancellationToken>()).Returns("true");

        await Make(loc).ToBeFocusedAsync();
    }

    [Fact]
    public async Task ToBeFocusedAsync_Fails_WhenFocusedAttributeIsFalse()
    {
        var loc = Locator();
        loc.GetAttributeAsync("focused", Arg.Any<CancellationToken>()).Returns("false");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeFocusedAsync());
    }

    [Fact]
    public async Task ToBeFocusedAsync_CaseInsensitive_AcceptsUppercase()
    {
        var loc = Locator();
        loc.GetAttributeAsync("focused", Arg.Any<CancellationToken>()).Returns("TRUE");

        await Make(loc).ToBeFocusedAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToBeEditableAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToBeEditableAsync_Passes_WhenEditable()
    {
        var loc = Locator();
        loc.IsEditableAsync(Arg.Any<CancellationToken>()).Returns(true);

        await Make(loc).ToBeEditableAsync();
    }

    [Fact]
    public async Task ToBeEditableAsync_Fails_WhenNotEditable()
    {
        var loc = Locator();
        loc.IsEditableAsync(Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeEditableAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToBeEmptyAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToBeEmptyAsync_Passes_WhenBothValueAndTextAreEmpty()
    {
        var loc = Locator();
        loc.InputValueAsync(Arg.Any<CancellationToken>()).Returns((string?)null);
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns(string.Empty);

        await Make(loc).ToBeEmptyAsync();
    }

    [Fact]
    public async Task ToBeEmptyAsync_Fails_WhenValueIsNonEmpty()
    {
        var loc = Locator();
        loc.InputValueAsync(Arg.Any<CancellationToken>()).Returns("hello");
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns(string.Empty);

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeEmptyAsync());
    }

    [Fact]
    public async Task ToBeEmptyAsync_Fails_WhenInnerTextIsNonEmpty()
    {
        var loc = Locator();
        loc.InputValueAsync(Arg.Any<CancellationToken>()).Returns((string?)null);
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("some text");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeEmptyAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToBeAttachedAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToBeAttachedAsync_Passes_WhenCountIsGreaterThanZero()
    {
        var loc = Locator();
        loc.CountAsync(Arg.Any<CancellationToken>()).Returns(2);

        await Make(loc).ToBeAttachedAsync();
    }

    [Fact]
    public async Task ToBeAttachedAsync_Fails_WhenCountIsZero()
    {
        var loc = Locator();
        loc.CountAsync(Arg.Any<CancellationToken>()).Returns(0);

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeAttachedAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToHaveTextAsync (string overload)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToHaveTextAsync_String_Passes_WhenTextMatches()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Hello World");

        await Make(loc).ToHaveTextAsync("Hello World");
    }

    [Fact]
    public async Task ToHaveTextAsync_String_Fails_WhenTextDoesNotMatch()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Goodbye");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveTextAsync("Hello World"));
    }

    [Fact]
    public async Task ToHaveTextAsync_IgnoreCase_Passes_WhenCaseDiffers()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("hello world");

        var opts = new AssertionsToHaveTextOptions { IgnoreCase = true };
        await Make(loc).ToHaveTextAsync("HELLO WORLD", opts);
    }

    [Fact]
    public async Task ToHaveTextAsync_Normalized_PassesWhenWhitespaceDiffers()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("  hello   world  ");

        var opts = new AssertionsToHaveTextOptions { Normalized = true };
        await Make(loc).ToHaveTextAsync("hello world", opts);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToHaveTextAsync (Regex overload)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToHaveTextAsync_Regex_Passes_WhenPatternMatches()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Hello 42");

        await Make(loc).ToHaveTextAsync(new Regex(@"Hello \d+", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task ToHaveTextAsync_Regex_Fails_WhenPatternDoesNotMatch()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Goodbye");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveTextAsync(new Regex(@"Hello \d+", RegexOptions.None, TimeSpan.FromSeconds(1))));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToContainTextAsync (string overload)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToContainTextAsync_String_Passes_WhenContains()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Hello World");

        await Make(loc).ToContainTextAsync("World");
    }

    [Fact]
    public async Task ToContainTextAsync_String_Fails_WhenNotContains()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Hello World");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToContainTextAsync("Goodbye"));
    }

    [Fact]
    public async Task ToContainTextAsync_String_IgnoreCase_Passes()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Hello World");

        var opts = new AssertionsToContainTextOptions { IgnoreCase = true };
        await Make(loc).ToContainTextAsync("world", opts);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToContainTextAsync (Regex overload)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToContainTextAsync_Regex_Passes_WhenPatternMatches()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Count: 42 items");

        await Make(loc).ToContainTextAsync(new Regex(@"\d+", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task ToContainTextAsync_Regex_Fails_WhenPatternDoesNotMatch()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("No numbers here");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToContainTextAsync(new Regex(@"^\d+$", RegexOptions.None, TimeSpan.FromSeconds(1))));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToHaveValueAsync (string overload)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToHaveValueAsync_String_Passes_WhenValueMatches()
    {
        var loc = Locator();
        loc.InputValueAsync(Arg.Any<CancellationToken>()).Returns("myvalue");

        await Make(loc).ToHaveValueAsync("myvalue");
    }

    [Fact]
    public async Task ToHaveValueAsync_String_Fails_WhenValueDiffers()
    {
        var loc = Locator();
        loc.InputValueAsync(Arg.Any<CancellationToken>()).Returns("other");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveValueAsync("myvalue"));
    }

    [Fact]
    public async Task ToHaveValueAsync_String_Fails_WhenValueIsNull()
    {
        var loc = Locator();
        loc.InputValueAsync(Arg.Any<CancellationToken>()).Returns((string?)null);

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveValueAsync("myvalue"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToHaveValueAsync (Regex overload)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToHaveValueAsync_Regex_Passes_WhenPatternMatches()
    {
        var loc = Locator();
        loc.InputValueAsync(Arg.Any<CancellationToken>()).Returns("abc123");

        await Make(loc).ToHaveValueAsync(new Regex(@"[a-z]+\d+", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task ToHaveValueAsync_Regex_Fails_WhenPatternDoesNotMatch()
    {
        var loc = Locator();
        loc.InputValueAsync(Arg.Any<CancellationToken>()).Returns("xyz");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveValueAsync(new Regex(@"^\d+$", RegexOptions.None, TimeSpan.FromSeconds(1))));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToHaveCountAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToHaveCountAsync_Passes_WhenCountMatches()
    {
        var loc = Locator();
        loc.CountAsync(Arg.Any<CancellationToken>()).Returns(3);

        await Make(loc).ToHaveCountAsync(3);
    }

    [Fact]
    public async Task ToHaveCountAsync_Fails_WhenCountDiffers()
    {
        var loc = Locator();
        loc.CountAsync(Arg.Any<CancellationToken>()).Returns(2);

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveCountAsync(3));
    }

    [Fact]
    public async Task ToHaveCountAsync_Passes_Zero_WhenEmpty()
    {
        var loc = Locator();
        loc.CountAsync(Arg.Any<CancellationToken>()).Returns(0);

        await Make(loc).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ToHaveCountAsync_RespectsPerCallTimeout()
    {
        var loc = Locator();
        loc.CountAsync(Arg.Any<CancellationToken>()).Returns(0);

        var opts = new AssertionsToHaveCountOptions { Timeout = TimeSpan.FromMilliseconds(30) };
        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveCountAsync(5, opts));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToHaveAttributeAsync (string overload)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToHaveAttributeAsync_String_Passes_WhenMatches()
    {
        var loc = Locator();
        loc.GetAttributeAsync("data-state", Arg.Any<CancellationToken>()).Returns("active");

        await Make(loc).ToHaveAttributeAsync("data-state", "active");
    }

    [Fact]
    public async Task ToHaveAttributeAsync_String_Fails_WhenDiffers()
    {
        var loc = Locator();
        loc.GetAttributeAsync("data-state", Arg.Any<CancellationToken>()).Returns("inactive");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveAttributeAsync("data-state", "active"));
    }

    [Fact]
    public async Task ToHaveAttributeAsync_String_IgnoreCase_Passes()
    {
        var loc = Locator();
        loc.GetAttributeAsync("aria-label", Arg.Any<CancellationToken>()).Returns("SUBMIT");

        var opts = new AssertionsToHaveAttributeOptions { IgnoreCase = true };
        await Make(loc).ToHaveAttributeAsync("aria-label", "submit", opts);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToHaveAttributeAsync (Regex overload)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToHaveAttributeAsync_Regex_Passes_WhenPatternMatches()
    {
        var loc = Locator();
        loc.GetAttributeAsync("class", Arg.Any<CancellationToken>()).Returns("btn btn-primary");

        await Make(loc).ToHaveAttributeAsync("class", new Regex(@"btn-\w+", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task ToHaveAttributeAsync_Regex_Fails_WhenPatternDoesNotMatch()
    {
        var loc = Locator();
        loc.GetAttributeAsync("class", Arg.Any<CancellationToken>()).Returns("nav");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveAttributeAsync("class", new Regex(@"btn-\w+", RegexOptions.None, TimeSpan.FromSeconds(1))));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToHaveIdAsync (string + Regex)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToHaveIdAsync_String_Passes_WhenIdMatches()
    {
        var loc = Locator();
        loc.GetAttributeAsync("id", Arg.Any<CancellationToken>()).Returns("btn_ok");

        await Make(loc).ToHaveIdAsync("btn_ok");
    }

    [Fact]
    public async Task ToHaveIdAsync_String_Fails_WhenIdDiffers()
    {
        var loc = Locator();
        loc.GetAttributeAsync("id", Arg.Any<CancellationToken>()).Returns("btn_cancel");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveIdAsync("btn_ok"));
    }

    [Fact]
    public async Task ToHaveIdAsync_Regex_Passes_WhenPatternMatches()
    {
        var loc = Locator();
        loc.GetAttributeAsync("id", Arg.Any<CancellationToken>()).Returns("btn_ok");

        await Make(loc).ToHaveIdAsync(new Regex(@"btn_\w+", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task ToHaveIdAsync_Regex_Fails_WhenPatternDoesNotMatch()
    {
        var loc = Locator();
        loc.GetAttributeAsync("id", Arg.Any<CancellationToken>()).Returns("nav_main");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveIdAsync(new Regex(@"btn_\w+", RegexOptions.None, TimeSpan.FromSeconds(1))));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToHaveClassAsync (string + Regex)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToHaveClassAsync_String_Passes_WhenClassMatches()
    {
        var loc = Locator();
        loc.GetAttributeAsync("class", Arg.Any<CancellationToken>()).Returns("MyClass");

        await Make(loc).ToHaveClassAsync("MyClass");
    }

    [Fact]
    public async Task ToHaveClassAsync_String_Fails_WhenClassDiffers()
    {
        var loc = Locator();
        loc.GetAttributeAsync("class", Arg.Any<CancellationToken>()).Returns("OtherClass");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveClassAsync("MyClass"));
    }

    [Fact]
    public async Task ToHaveClassAsync_IgnoreCase_Passes()
    {
        var loc = Locator();
        loc.GetAttributeAsync("class", Arg.Any<CancellationToken>()).Returns("myclass");

        var opts = new AssertionsToHaveClassOptions { IgnoreCase = true };
        await Make(loc).ToHaveClassAsync("MYCLASS", opts);
    }

    [Fact]
    public async Task ToHaveClassAsync_Regex_Passes_WhenPatternMatches()
    {
        var loc = Locator();
        loc.GetAttributeAsync("class", Arg.Any<CancellationToken>()).Returns("ToolBar_v2");

        await Make(loc).ToHaveClassAsync(new Regex(@"ToolBar_\w+", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task ToHaveClassAsync_Regex_Fails_WhenPatternDoesNotMatch()
    {
        var loc = Locator();
        loc.GetAttributeAsync("class", Arg.Any<CancellationToken>()).Returns("NavBar");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveClassAsync(new Regex(@"ToolBar_\w+", RegexOptions.None, TimeSpan.FromSeconds(1))));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToHaveRoleAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToHaveRoleAsync_Passes_WhenControlTypeNameMatches()
    {
        var loc = Locator();
        // AriaRole.Button → ControlType.Button → "Button"
        loc.GetAttributeAsync("controltype", Arg.Any<CancellationToken>()).Returns("Button");

        await Make(loc).ToHaveRoleAsync(AriaRole.Button);
    }

    [Fact]
    public async Task ToHaveRoleAsync_Fails_WhenControlTypeNameDiffers()
    {
        var loc = Locator();
        loc.GetAttributeAsync("controltype", Arg.Any<CancellationToken>()).Returns("Edit");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveRoleAsync(AriaRole.Button));
    }

    [Fact]
    public async Task ToHaveRoleAsync_ThrowsNotSupported_ForWebOnlyRole()
    {
        var loc = Locator();
        loc.GetAttributeAsync("controltype", Arg.Any<CancellationToken>()).Returns("Custom");

        // Article is a web-only role — should throw NotSupportedException
        await Assert.ThrowsAsync<NotSupportedException>(
            () => Make(loc).ToHaveRoleAsync(AriaRole.Article));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToHaveAccessibleNameAsync (string + Regex)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToHaveAccessibleNameAsync_String_Passes_WhenNameMatches()
    {
        var loc = Locator();
        loc.GetAttributeAsync("name", Arg.Any<CancellationToken>()).Returns("Submit");

        await Make(loc).ToHaveAccessibleNameAsync("Submit");
    }

    [Fact]
    public async Task ToHaveAccessibleNameAsync_String_Fails_WhenNameDiffers()
    {
        var loc = Locator();
        loc.GetAttributeAsync("name", Arg.Any<CancellationToken>()).Returns("Cancel");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveAccessibleNameAsync("Submit"));
    }

    [Fact]
    public async Task ToHaveAccessibleNameAsync_IgnoreCase_Passes()
    {
        var loc = Locator();
        loc.GetAttributeAsync("name", Arg.Any<CancellationToken>()).Returns("submit");

        var opts = new AssertionsToHaveAccessibleNameOptions { IgnoreCase = true };
        await Make(loc).ToHaveAccessibleNameAsync("SUBMIT", opts);
    }

    [Fact]
    public async Task ToHaveAccessibleNameAsync_Regex_Passes_WhenPatternMatches()
    {
        var loc = Locator();
        loc.GetAttributeAsync("name", Arg.Any<CancellationToken>()).Returns("Save Document");

        await Make(loc).ToHaveAccessibleNameAsync(new Regex(@"Save \w+", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task ToHaveAccessibleNameAsync_Regex_Fails_WhenPatternDoesNotMatch()
    {
        var loc = Locator();
        loc.GetAttributeAsync("name", Arg.Any<CancellationToken>()).Returns("Cancel");

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToHaveAccessibleNameAsync(new Regex(@"Save \w+", RegexOptions.None, TimeSpan.FromSeconds(1))));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Not property: returns IFlawrightNotAssertions
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Not_ReturnsNonNullObject()
    {
        var loc = Locator();
        var assertions = Make(loc);

        Assert.NotNull(assertions.Not);
    }

    [Fact]
    public void Not_ReturnsDifferentInstance()
    {
        var loc = Locator();
        var assertions = Make(loc);

        var not1 = assertions.Not;
        var not2 = assertions.Not;

        // Each call creates a fresh instance
        Assert.NotSame(not1, not2);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Exception messages
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AssertionException_ContainsSelectorInMessage()
    {
        var loc = Locator("#myButton");
        loc.IsVisibleAsync(Arg.Any<CancellationToken>()).Returns(false);

        var ex = await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeVisibleAsync());

        Assert.Contains("#myButton", ex.Message);
    }

    [Fact]
    public async Task AssertionException_ContainsDescriptionInMessage()
    {
        var loc = Locator("mysel");
        loc.IsVisibleAsync(Arg.Any<CancellationToken>()).Returns(false);

        var ex = await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeVisibleAsync());

        Assert.Contains("visible", ex.Message);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Transient exceptions are swallowed during polling
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TransientException_IsSwallowed_ThenSucceeds()
    {
        var loc = Locator();
        // First call throws, second returns true
        loc.IsVisibleAsync(Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("transient"),
                _ => Task.FromResult(true));

        // Should not throw — polling recovers after the transient error
        await Make(loc).ToBeVisibleAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CancellationToken is respected
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CancellationToken_CancelsPolling()
    {
        var loc = Locator();
        loc.IsVisibleAsync(Arg.Any<CancellationToken>()).Returns(false);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Make(loc).ToBeVisibleAsync(options: null, ct: cts.Token));
    }
}
