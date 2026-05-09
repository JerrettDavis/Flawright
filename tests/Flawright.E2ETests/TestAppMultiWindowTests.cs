using Flawright;
using Flawright.CloseBehaviors;
using Flawright.InputModes;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// Multi-window E2E coverage for the Flawright browser API.
/// Validates that <see cref="IFlawrightBrowser.NewPageAsync"/>,
/// <see cref="IFlawrightBrowser.GetAllPagesAsync"/>, and
/// <see cref="IFlawrightBrowser.WaitForPageAsync"/> behave correctly when the
/// target application opens additional top-level windows.
/// </summary>
/// <remarks>
/// The deterministic WPF test app exposes a <c>btnSpawnWindow</c> button that
/// opens a second top-level window with the fixed title
/// <c>"Flawright Spawned Window"</c>. These tests:
/// <list type="bullet">
///   <item>Verify <see cref="IFlawrightBrowser.NewPageAsync"/> consistently
///   returns a page bound to the application's main window.</item>
///   <item>Verify <see cref="IFlawrightBrowser.GetAllPagesAsync"/> enumerates
///   both windows after the second is opened.</item>
///   <item>Verify <see cref="IFlawrightBrowser.WaitForPageAsync"/> resolves the
///   spawned window when matched by title.</item>
/// </list>
/// </remarks>
public class TestAppMultiWindowTests : IAsyncLifetime
{
    private static readonly string TestAppPath =
        Path.Combine(AppContext.BaseDirectory, "TestApp", "Flawright.E2ETests.TestApp.exe");

    private const string SpawnedWindowTitle = "Flawright Spawned Window";

    private IFlawright? _fw;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _fw = await global::Flawright.Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = TestAppPath },
            new FlawrightOptions
            {
                CloseBehavior = new DismissDialogCloseBehavior("Don't Save"),
                InputMode = new VirtualInputMode(),
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

    // ── NewPageAsync ──────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightBrowser.NewPageAsync"/> must return a page whose
    /// title matches the test app's main window. Repeated calls return pages
    /// bound to the same main window.
    /// </summary>
    [Fact]
    public async Task NewPageAsync_ReturnsMainWindowPage()
    {
        var page1 = await _fw!.Browser.NewPageAsync();
        var page2 = await _fw.Browser.NewPageAsync();

        Assert.Equal("Flawright Test App", await page1.TitleAsync());
        Assert.Equal("Flawright Test App", await page2.TitleAsync());
    }

    // ── GetAllPagesAsync ──────────────────────────────────────────────────────

    /// <summary>
    /// Before any extra window is spawned, <see cref="IFlawrightBrowser.GetAllPagesAsync"/>
    /// returns exactly the application's main window.
    /// </summary>
    [Fact]
    public async Task GetAllPagesAsync_ReturnsMainWindowOnly_BeforeSpawn()
    {
        var pages = await _fw!.Browser.GetAllPagesAsync();

        Assert.NotEmpty(pages);
        Assert.Contains(pages, p => string.Equals(
            p.TitleAsync().GetAwaiter().GetResult(),
            "Flawright Test App",
            StringComparison.Ordinal));
    }

    /// <summary>
    /// After clicking <c>btnSpawnWindow</c>, both the main window and the
    /// spawned window must be enumerated by <see cref="IFlawrightBrowser.GetAllPagesAsync"/>.
    /// </summary>
    [Fact]
    public async Task GetAllPagesAsync_EnumeratesSpawnedWindow()
    {
        var page = await _fw!.Browser.NewPageAsync();
        await page.Locator("#btnSpawnWindow").ClickAsync();

        // Poll briefly while WPF finishes showing the new window.
        IReadOnlyList<IFlawrightPage> pages = Array.Empty<IFlawrightPage>();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            pages = await _fw.Browser.GetAllPagesAsync();
            var titles = await Task.WhenAll(pages.Select(p => p.TitleAsync()));
            if (titles.Any(t => string.Equals(t, SpawnedWindowTitle, StringComparison.Ordinal)) &&
                titles.Any(t => string.Equals(t, "Flawright Test App", StringComparison.Ordinal)))
            {
                break;
            }
            await Task.Delay(100);
        }

        var seenTitles = await Task.WhenAll(pages.Select(p => p.TitleAsync()));
        Assert.Contains("Flawright Test App", seenTitles);
        Assert.Contains(SpawnedWindowTitle, seenTitles);
    }

    // ── WaitForPageAsync ──────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightBrowser.WaitForPageAsync"/> resolves once a window
    /// whose title contains the supplied substring appears.
    /// </summary>
    [Fact]
    public async Task WaitForPageAsync_ResolvesSpawnedWindow_ByTitle()
    {
        var page = await _fw!.Browser.NewPageAsync();
        await page.Locator("#btnSpawnWindow").ClickAsync();

        var spawned = await _fw.Browser.WaitForPageAsync(
            "Spawned Window", TimeSpan.FromSeconds(5));

        Assert.Equal(SpawnedWindowTitle, await spawned.TitleAsync());
    }

    /// <summary>
    /// <see cref="IFlawrightBrowser.WaitForPageAsync"/> must throw a
    /// <see cref="FlawrightTimeoutException"/> when no window with a matching
    /// title appears within the timeout.
    /// </summary>
    [Fact]
    public async Task WaitForPageAsync_TimesOut_WhenTitleNeverMatches()
    {
        // Warm up the browser handle first so the WaitForPageAsync call below
        // exercises the wait/timeout path rather than the launch path.
        _ = await _fw!.Browser.NewPageAsync();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAsync<FlawrightTimeoutException>(async () =>
            await _fw.Browser.WaitForPageAsync(
                "Window That Will Never Exist", TimeSpan.FromSeconds(1)));
        sw.Stop();

        // The wait must have honoured the timeout — give a generous upper bound
        // to absorb scheduler jitter on slow CI runners.
        Assert.True(sw.Elapsed >= TimeSpan.FromMilliseconds(900),
            $"Expected wait to honour ~1s timeout but completed in {sw.ElapsedMilliseconds} ms.");
    }
}
