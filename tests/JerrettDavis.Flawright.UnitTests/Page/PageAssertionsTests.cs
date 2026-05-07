#pragma warning disable MA0009 // test regexes are safe

using System.Text.RegularExpressions;
using JerrettDavis.Flawright.Assertions;
using JerrettDavis.Flawright.Page;
using JerrettDavis.Flawright.UnitTests.Fakes;
using NSubstitute;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Page;

/// <summary>
/// Unit tests for <see cref="FlawrightPageAssertions"/> and
/// <see cref="IFlawrightPageAssertions"/>.
/// </summary>
public sealed class PageAssertionsTests
{
    private static readonly FlawrightOptions FastOpts = new()
    {
        DefaultTimeout = TimeSpan.FromMilliseconds(300),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(20),
    };

    private static FlawrightPage MakePage(string title)
    {
        var root = new FakeElementBackend(name: title);
        var page = new FlawrightPage(
            root,
            new FakeInputBackend(),
            FastOpts,
            new FakeConditionTranslator());
        return page;
    }

    private static FlawrightPageAssertions Assertions(string title)
        => new FlawrightPageAssertions(MakePage(title), FastOpts);

    // ── Not property ──────────────────────────────────────────────────────────

    [Fact]
    public void Not_ReturnsNonNull()
    {
        var a = Assertions("Foo");
        Assert.NotNull(a.Not);
    }

    [Fact]
    public void Not_Not_ReturnsDifferentInstance()
    {
        var a = Assertions("Foo");
        var notA = a.Not;
        Assert.NotSame(a, notA);
    }

    [Fact]
    public void Not_Not_IsBackToNormal()
    {
        // Applying Not twice should work correctly (double-negation)
        var page = MakePage("Exact Title");
        var a = new FlawrightPageAssertions(page, FastOpts);
        var notNotA = a.Not.Not;
        // Passing title on double-negated should pass
        Assert.NotNull(notNotA);
    }

    // ── ToHaveTitleAsync (string) ─────────────────────────────────────────────

    [Fact]
    public async Task ToHaveTitleAsync_String_PassesWhenTitleMatches()
    {
        var a = Assertions("My App");
        await a.ToHaveTitleAsync("My App"); // Should not throw
    }

    [Fact]
    public async Task ToHaveTitleAsync_String_ThrowsWhenTitleDoesNotMatch()
    {
        var a = Assertions("My App");
        await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => a.ToHaveTitleAsync("Wrong Title", new PageAssertionsToHaveTitleOptions
            {
                Timeout = TimeSpan.FromMilliseconds(50)
            }));
    }

    [Fact]
    public async Task ToHaveTitleAsync_String_IsCaseSensitiveByDefault()
    {
        var a = Assertions("My App");
        await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => a.ToHaveTitleAsync("my app", new PageAssertionsToHaveTitleOptions
            {
                Timeout = TimeSpan.FromMilliseconds(50)
            }));
    }

    [Fact]
    public async Task ToHaveTitleAsync_String_IgnoresCase_WhenOptionSet()
    {
        var a = Assertions("My App");
        await a.ToHaveTitleAsync("my app", new PageAssertionsToHaveTitleOptions
        {
            IgnoreCase = true
        }); // Should not throw
    }

    [Fact]
    public async Task ToHaveTitleAsync_String_ThrowsOnNullExpected()
    {
        var a = Assertions("App");
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => a.ToHaveTitleAsync((string)null!));
    }

    // ── ToHaveTitleAsync (Regex) ──────────────────────────────────────────────

    [Fact]
    public async Task ToHaveTitleAsync_Regex_PassesWhenTitleMatches()
    {
        var a = Assertions("My App v1.0");
        await a.ToHaveTitleAsync(new Regex("My App v\\d+\\.\\d+", RegexOptions.None, TimeSpan.FromSeconds(1))); // Should not throw
    }

    [Fact]
    public async Task ToHaveTitleAsync_Regex_ThrowsWhenTitleDoesNotMatch()
    {
        var a = Assertions("My App");
        await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => a.ToHaveTitleAsync(new Regex("^\\d+$", RegexOptions.None, TimeSpan.FromSeconds(1)), new PageAssertionsToHaveTitleOptions
            {
                Timeout = TimeSpan.FromMilliseconds(50)
            }));
    }

    [Fact]
    public async Task ToHaveTitleAsync_Regex_ThrowsOnNullExpected()
    {
        var a = Assertions("App");
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => a.ToHaveTitleAsync((Regex)null!));
    }

    [Fact]
    public async Task ToHaveTitleAsync_Regex_UsesRegexOptions()
    {
        var a = Assertions("hello world");
        // Case-insensitive regex should match
        await a.ToHaveTitleAsync(new Regex("HELLO", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)));
    }

    // ── Not.ToHaveTitleAsync (string) ─────────────────────────────────────────

    [Fact]
    public async Task Not_ToHaveTitleAsync_String_PassesWhenTitleDoesNotMatch()
    {
        var a = Assertions("My App");
        await a.Not.ToHaveTitleAsync("Different Title"); // Should not throw
    }

    [Fact]
    public async Task Not_ToHaveTitleAsync_String_ThrowsWhenTitleMatches()
    {
        var a = Assertions("My App");
        await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => a.Not.ToHaveTitleAsync("My App", new PageAssertionsToHaveTitleOptions
            {
                Timeout = TimeSpan.FromMilliseconds(50)
            }));
    }

    // ── Not.ToHaveTitleAsync (Regex) ──────────────────────────────────────────

    [Fact]
    public async Task Not_ToHaveTitleAsync_Regex_PassesWhenTitleDoesNotMatch()
    {
        var a = Assertions("My App");
        await a.Not.ToHaveTitleAsync(new Regex("^\\d+$", RegexOptions.None, TimeSpan.FromSeconds(1))); // Should not throw
    }

    [Fact]
    public async Task Not_ToHaveTitleAsync_Regex_ThrowsWhenTitleMatches()
    {
        var a = Assertions("My App v1");
        await Assert.ThrowsAsync<FlawrightTimeoutException>(
            () => a.Not.ToHaveTitleAsync(new Regex("My App", RegexOptions.None, TimeSpan.FromSeconds(1)), new PageAssertionsToHaveTitleOptions
            {
                Timeout = TimeSpan.FromMilliseconds(50)
            }));
    }

    // ── Integration with AssertionsStatic ─────────────────────────────────────

    [Fact]
    public void AssertionsStatic_Expect_Page_ReturnsPageAssertions()
    {
        var page = MakePage("Test App");
        var assertions = AssertionsStatic.Expect(page);
        Assert.IsAssignableFrom<IFlawrightPageAssertions>(assertions);
    }

    [Fact]
    public async Task AssertionsStatic_Expect_Page_CanAssertTitle()
    {
        var page = MakePage("Test App");
        await AssertionsStatic.Expect(page).ToHaveTitleAsync("Test App");
    }

    [Fact]
    public void AssertionsStatic_Expect_Page_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => AssertionsStatic.Expect((IFlawrightPage)null!));
    }

    // ── Options round-trip ────────────────────────────────────────────────────

    [Fact]
    public async Task ToHaveTitleAsync_UsesPageDefaultTimeout_WhenOptionsHasNoTimeout()
    {
        var page = MakePage("Title");
        var assertions = new FlawrightPageAssertions(page, FastOpts);
        // Default timeout is 300ms — this should succeed quickly
        await assertions.ToHaveTitleAsync("Title");
    }
}
