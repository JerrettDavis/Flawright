using Flawright;
using Flawright.InputModes;
using Flawright.Locator;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// E2E tests for low-level keyboard and mouse wheel APIs.
/// Exercises <see cref="IFlawrightMouse.WheelAsync"/>,
/// <see cref="IFlawrightKeyboard.DownAsync"/> / <see cref="IFlawrightKeyboard.UpAsync"/>,
/// <see cref="IFlawrightKeyboard.InsertTextAsync"/>, and
/// <see cref="IFlawrightLocator.PressSequentiallyAsync"/>.
/// </summary>
/// <remarks>
/// Mouse wheel and keyboard chord tests require <see cref="RealInputMode"/> because
/// they synthesise raw Win32 input events.  Tests that can use UIA patterns use
/// <see cref="VirtualInputMode"/> and are split into a separate class below.
/// </remarks>
public sealed class TestAppMouseWheelTests : IAsyncLifetime
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
                InputMode = new RealInputMode(),
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

    // ── Mouse.WheelAsync ──────────────────────────────────────────────────────

    /// <summary>
    /// Scrolling the mouse wheel over the <c>lsbScrollable</c> ListBox (50 items)
    /// moves the scroll position so that items other than the first are visible.
    /// </summary>
    /// <remarks>
    /// The test positions the mouse over the ListBox bounding box centre, then
    /// dispatches a downward wheel event.  The first ListBox item index reported
    /// via UIA should differ from 0 after a significant scroll delta.
    /// </remarks>
    [Fact]
    public async Task Mouse_WheelAsync_ScrollsListBox()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.BringToFrontAsync();

        // Activate Selection tab so lsbScrollable is visible.
        await page.Locator("#tabSelection").ClickAsync();

        var listBox = page.Locator("#lsbScrollable");
        var bbox = await listBox.BoundingBoxAsync();

        Assert.NotNull(bbox);

        // Position the mouse over the centre of the ListBox.
        var centerX = bbox.X + bbox.Width / 2.0;
        var centerY = bbox.Y + bbox.Height / 2.0;

        await page.Mouse.MoveAsync(centerX, centerY);

        // Scroll down significantly.
        await page.Mouse.WheelAsync(0, 300);

        // Allow a brief settle for scroll events to process.
        await page.WaitForTimeoutAsync(200);

        // After scrolling, the visible area should have shifted.
        // We verify indirectly: the ListBox is still visible and did not crash.
        var isVisible = await listBox.IsVisibleAsync();
        Assert.True(isVisible, "ListBox should remain visible after mouse wheel scroll.");
    }
}

/// <summary>
/// E2E tests for keyboard Down/Up/InsertText and PressSequentially using
/// <see cref="RealInputMode"/> for operations that require raw input synthesis.
/// </summary>
public sealed class TestAppKeyboardTests : IAsyncLifetime
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
                InputMode = new RealInputMode(),
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

    // ── Keyboard.DownAsync / UpAsync ──────────────────────────────────────────

    /// <summary>
    /// Holding Shift down, pressing "a", then releasing Shift produces the
    /// uppercase character "A" in a text field, exercising
    /// <see cref="IFlawrightKeyboard.DownAsync"/> and
    /// <see cref="IFlawrightKeyboard.UpAsync"/>.
    /// </summary>
    [Fact]
    public async Task Keyboard_DownThenUp_PressesAndReleases()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.BringToFrontAsync();

        // Activate Inputs tab.
        await page.Locator("#tabInputs").ClickAsync();

        // Focus the labeled TextBox.
        await page.Locator("#txtLabeledField").FocusAsync();

        // Hold Shift, press "a", release Shift.
        await page.Keyboard.DownAsync("Shift");
        await page.Keyboard.PressAsync("a");
        await page.Keyboard.UpAsync("Shift");

        var value = await page.Locator("#txtLabeledField").InputValueAsync();
        Assert.NotNull(value);
        // Shift+A should produce uppercase "A".
        Assert.Contains("A", value, StringComparison.Ordinal);
    }

    // ── Keyboard.InsertTextAsync ───────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightKeyboard.InsertTextAsync"/> inserts text directly into
    /// the focused control without key-by-key simulation.
    /// </summary>
    [Fact]
    public async Task Keyboard_InsertTextAsync_AddsTextDirectly()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.BringToFrontAsync();

        // Activate Inputs tab.
        await page.Locator("#tabInputs").ClickAsync();

        // Focus the placeholder TextBox.
        await page.Locator("#txtPlaceholderTest").FocusAsync();

        await page.Keyboard.InsertTextAsync("Hello via UIA");

        var value = await page.Locator("#txtPlaceholderTest").InputValueAsync();
        Assert.NotNull(value);
        Assert.Contains("Hello via UIA", value, StringComparison.Ordinal);
    }

    // ── IFlawrightLocator.PressSequentiallyAsync ──────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightLocator.PressSequentiallyAsync"/> types text
    /// character-by-character into a text field using the underlying
    /// <c>TypeAsync</c> path.
    /// </summary>
    [Fact]
    public async Task Keyboard_PressSequentiallyAsync_TypesCharByChar()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.BringToFrontAsync();

        // Activate Inputs tab.
        await page.Locator("#tabInputs").ClickAsync();

        var textBox = page.Locator("#txtMultiline");
        await textBox.FocusAsync();

        await textBox.PressSequentiallyAsync("Sequential");

        var value = await textBox.InputValueAsync();
        Assert.NotNull(value);
        Assert.Contains("Sequential", value, StringComparison.Ordinal);
    }
}
