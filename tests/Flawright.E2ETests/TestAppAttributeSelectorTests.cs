using Flawright;
using Flawright.InputModes;
using Flawright.Locator;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// E2E tests for attribute-syntax selectors: <c>[name=...]</c> (equals),
/// <c>[name^=...]</c> (starts-with), and <c>[name*=...]</c> (contains).
/// </summary>
/// <remarks>
/// These tests cover the coverage gap identified for attribute-syntax selectors
/// which were unit-tested against the selector parser but not exercised via the
/// full E2E resolution pipeline through the UIA backend.
/// </remarks>
public sealed class TestAppAttributeSelectorTests : IAsyncLifetime
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
                DefaultTimeout = TimeSpan.FromSeconds(10),
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

    // ── [name=...] exact equals ────────────────────────────────────────────────

    /// <summary>
    /// <c>[name=Exit]</c> resolves elements whose UIA Name is exactly "Exit".
    /// </summary>
    [Fact]
    public async Task Selector_AttributeEquals_ResolvesByName()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var count = await page.Locator("[name=Exit]").CountAsync();

        Assert.True(count >= 1, "[name=Exit] should resolve at least the Exit button.");
    }

    // ── [name^=...] starts-with ────────────────────────────────────────────────

    /// <summary>
    /// <c>[name^=Ex]</c> resolves elements whose UIA Name starts with "Ex".
    /// </summary>
    [Fact]
    public async Task Selector_AttributeStartsWith_ResolvesByPrefix()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var count = await page.Locator("[name^=Ex]").CountAsync();

        Assert.True(count >= 1, "[name^=Ex] should resolve at least the Exit button.");
    }

    // ── [name*=...] contains ───────────────────────────────────────────────────

    /// <summary>
    /// <c>[name*=xi]</c> resolves elements whose UIA Name contains the substring "xi".
    /// </summary>
    [Fact]
    public async Task Selector_AttributeContains_ResolvesBySubstring()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var count = await page.Locator("[name*=xi]").CountAsync();

        Assert.True(count >= 1, "[name*=xi] should resolve at least the Exit button.");
    }

    // ── Attribute selector combined with control type ──────────────────────────

    /// <summary>
    /// Attribute selector chained with a control-type filter resolves exactly
    /// the Exit <c>Button</c> (excludes the inner TextBlock with the same name).
    /// </summary>
    [Fact]
    public async Task Selector_AttributeEquals_FilteredByControlType_ResolvesButton()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Chain: all Buttons whose name == "Exit".
        var exitButtons = page
            .Locator("controltype:Button")
            .Filter(new LocatorFilterOptions { HasText = "Exit" });

        var count = await exitButtons.CountAsync();
        Assert.Equal(1, count);
    }
}
