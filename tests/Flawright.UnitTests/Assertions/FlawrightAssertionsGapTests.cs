using System.Text.RegularExpressions;
using Flawright.Assertions;
using Flawright.UnitTests.Fakes;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Flawright.UnitTests.Assertions;

/// <summary>
/// Covers assertion paths not yet exercised by existing tests:
/// - ToBeFocusedAsync when AllAsync returns empty (no element) → returns false
/// - ToBeFocusedAsync when AllAsync throws → swallows exception and returns false
/// - FlawrightNotAssertions.ToContainTextAsync(Regex) wrapper
/// - FlawrightNotAssertions.ToHaveAttributeAsync(name, Regex) wrapper
/// </summary>
public sealed class FlawrightAssertionsGapTests
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

    private static IFlawrightNotAssertions MakeNot(IFlawrightLocator locator)
        => new FlawrightAssertions(locator, QuickOptions).Not;

    private static IFlawrightLocator Locator(string selector = "test")
    {
        var loc = Substitute.For<IFlawrightLocator>();
        loc.Selector.Returns(selector);
        return loc;
    }

    // ── ToBeFocusedAsync: empty AllAsync result → false (line 142) ────────────

    [Fact]
    public async Task ToBeFocusedAsync_WhenNoElements_Fails()
    {
        // AllAsync returns empty list → ToBeFocusedAsync should return false → assertion fails
        var loc = Locator();
        loc.AllAsync(null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IFlawrightElement>());

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeFocusedAsync());
    }

    [Fact]
    public async Task ToBeFocusedAsync_WhenNoElements_NegatedPasses()
    {
        var loc = Locator();
        loc.AllAsync(null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IFlawrightElement>());

        // Negated: not focused → passes
        await Make(loc, negated: true).ToBeFocusedAsync();
    }

    // ── ToBeFocusedAsync: AllAsync throws → swallow, return false (lines 148-151) ──

    [Fact]
    public async Task ToBeFocusedAsync_WhenAllAsyncThrows_Fails()
    {
        var loc = Locator();
        loc.AllAsync(null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("element not ready"));

        await Assert.ThrowsAsync<AssertionException>(
            () => Make(loc).ToBeFocusedAsync());
    }

    [Fact]
    public async Task ToBeFocusedAsync_WhenAllAsyncThrows_NegatedPasses()
    {
        var loc = Locator();
        loc.AllAsync(null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("element not ready"));

        // Exception swallowed → returns false → negated passes
        await Make(loc, negated: true).ToBeFocusedAsync();
    }

    // ── FlawrightNotAssertions.ToContainTextAsync(Regex) wrapper (line 501) ──

    [Fact]
    public async Task Not_ToContainTextAsync_Regex_Passes_WhenPatternDoesNotMatch()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Hello World");

        await MakeNot(loc).ToContainTextAsync(
            new Regex(@"Goodbye \w+", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task Not_ToContainTextAsync_Regex_Fails_WhenPatternMatches()
    {
        var loc = Locator();
        loc.InnerTextAsync(Arg.Any<CancellationToken>()).Returns("Hello World");

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToContainTextAsync(
                new Regex(@"Hello \w+", RegexOptions.None, TimeSpan.FromSeconds(1))));
    }

    // ── FlawrightNotAssertions.ToHaveAttributeAsync(name, Regex) wrapper (line 521) ──

    [Fact]
    public async Task Not_ToHaveAttributeAsync_Regex_Passes_WhenPatternDoesNotMatch()
    {
        var loc = Locator();
        loc.GetAttributeAsync("state", Arg.Any<CancellationToken>()).Returns("inactive");

        await MakeNot(loc).ToHaveAttributeAsync(
            "state",
            new Regex(@"^active$", RegexOptions.None, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task Not_ToHaveAttributeAsync_Regex_Fails_WhenPatternMatches()
    {
        var loc = Locator();
        loc.GetAttributeAsync("state", Arg.Any<CancellationToken>()).Returns("active");

        await Assert.ThrowsAsync<AssertionException>(
            () => MakeNot(loc).ToHaveAttributeAsync(
                "state",
                new Regex(@"^active$", RegexOptions.None, TimeSpan.FromSeconds(1))));
    }
}
