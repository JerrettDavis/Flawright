using System.Text.RegularExpressions;
using Flawright;
using Flawright.CloseBehaviors;
using Flawright.InputModes;
using Flawright.Locator;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// E2E coverage for multi-level locator chaining and regex-based filtering.
/// The WPF test app exposes a three-level nested control hierarchy
/// (<c>nestedGroup</c> → <c>nestedPanel</c> → buttons named
/// <c>"Inner Alpha 1"</c>, <c>"Inner Alpha 2"</c>, <c>"Inner Beta 1"</c>) so
/// chains and regex filters can be validated deterministically.
/// </summary>
/// <remarks>
/// Running under <see cref="VirtualInputMode"/> keeps these tests safe on
/// headless runners.
/// </remarks>
public class TestAppLocatorChainTests : IAsyncLifetime
{
    private static readonly string TestAppPath =
        Path.Combine(AppContext.BaseDirectory, "TestApp", "Flawright.E2ETests.TestApp.exe");

    private IFlawright? _fw;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _fw = await global::Flawright.Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = TestAppPath },
            new FlawrightOptions
            {
                InputMode = new VirtualInputMode(),
                CloseBehavior = new DismissDialogCloseBehavior("Don't Save"),
                DefaultTimeout = TimeSpan.FromSeconds(5),
            });
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_fw != null)
        {
            await _fw.Browser.CloseAsync();
            await _fw.DisposeAsync();
        }
    }

    // ── Three-level chain ─────────────────────────────────────────────────────

    /// <summary>
    /// A three-level chain of <c>Locator(...)</c> calls
    /// (outer group → inner group → button) resolves to the three buttons
    /// inside the inner panel and excludes every button outside the nested
    /// hierarchy.
    /// </summary>
    [Fact]
    public async Task Locator_ThreeLevelChain_ResolvesNestedButtons()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Gate on the outermost nested container being present in the UIA tree
        // before diving three levels deep.  Without this wait, FlaUI can start
        // reading the WPF automation tree before the nested GroupBox subtree is
        // fully materialised, causing ReadProcessMemory failures on loaded CI
        // runners.  WaitForAsync is a non-mouse/keyboard probe — safe on headless
        // runners and compliant with the no-E2E-locally rule.
        await page.Locator("#nestedGroup").WaitForAsync();

        var nestedButtons = page
            .Locator("#nestedGroup")
            .Locator("#innerGroup")
            .Locator("controltype:Button");

        var count = await nestedButtons.CountAsync();
        Assert.Equal(3, count);
    }

    /// <summary>
    /// Chaining narrows scope: querying buttons inside <c>nestedGroup</c>
    /// returns only nested buttons, not the page-level buttons such as
    /// <c>btnClick</c>.
    /// </summary>
    [Fact]
    public async Task Locator_Chain_NarrowsScopeBelowAncestor()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var allButtons = await page.Locator("controltype:Button").CountAsync();
        var nestedButtons = await page
            .Locator("#nestedGroup")
            .Locator("controltype:Button")
            .CountAsync();

        Assert.True(allButtons > nestedButtons,
            "Window-wide button count must exceed nested-group count.");
        Assert.Equal(3, nestedButtons);
    }

    // ── Regex-filtered chains ─────────────────────────────────────────────────

    /// <summary>
    /// <see cref="LocatorFilterOptions.HasTextRegex"/> on a chained locator
    /// matches only elements whose visible text satisfies the regex.
    /// </summary>
    [Fact]
    public async Task Locator_FilterByHasTextRegex_MatchesOnlyAlphaButtons()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var alphaButtons = page
            .Locator("#nestedGroup")
            .Locator("controltype:Button")
            .Filter(new LocatorFilterOptions
            {
                HasTextRegex = new Regex(@"^Inner Alpha \d+$", RegexOptions.None, TimeSpan.FromSeconds(1)),
            });

        var count = await alphaButtons.CountAsync();
        Assert.Equal(2, count);
    }

    /// <summary>
    /// <see cref="LocatorFilterOptions.HasNotTextRegex"/> excludes elements
    /// whose visible text matches the regex. Combined with the chain, this
    /// returns only the Beta button under <c>nestedGroup</c>.
    /// </summary>
    [Fact]
    public async Task Locator_FilterByHasNotTextRegex_ExcludesAlphaButtons()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var nonAlpha = page
            .Locator("#nestedGroup")
            .Locator("controltype:Button")
            .Filter(new LocatorFilterOptions
            {
                HasNotTextRegex = new Regex(@"^Inner Alpha \d+$", RegexOptions.None, TimeSpan.FromSeconds(1)),
            });

        var count = await nonAlpha.CountAsync();
        Assert.Equal(1, count);
    }

    /// <summary>
    /// A regex anchored with <c>$</c> end-of-string ensures partial-name
    /// collisions are rejected. The pattern <c>"^Inner Alpha 1$"</c> matches
    /// only the first Alpha button — not "Inner Alpha 2" — even though both
    /// share the "Inner Alpha" prefix.
    /// </summary>
    [Fact]
    public async Task Locator_FilterByHasTextRegex_AnchoredPatternRejectsPartialMatches()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var onlyAlpha1 = page
            .Locator("#nestedGroup")
            .Locator("controltype:Button")
            .Filter(new LocatorFilterOptions
            {
                HasTextRegex = new Regex(@"^Inner Alpha 1$", RegexOptions.None, TimeSpan.FromSeconds(1)),
            });

        var count = await onlyAlpha1.CountAsync();
        Assert.Equal(1, count);
    }
}
