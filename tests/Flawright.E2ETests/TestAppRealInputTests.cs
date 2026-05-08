using Flawright;
using Flawright.CloseBehaviors;
using Flawright.InputModes;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// E2E tests that require <see cref="RealInputMode"/> because the actions under
/// test have no UIA-pattern equivalent (double-click, hover, key chords, etc.).
///
/// These tests target the same deterministic WPF test application as
/// <see cref="TestAppTests"/> but configure <see cref="RealInputMode"/> so that
/// physical mouse and keyboard events are synthesised.
/// </summary>
/// <remarks>
/// <para>
/// These tests MUST NOT run on headless CI runners that have no interactive
/// desktop session — use the <c>RequiresAppFact</c> attribute (or an equivalent
/// skip condition) if your CI environment lacks a UI session.
/// </para>
/// <para>
/// The <see cref="TestAppTests"/> fixture covers the broad surface with
/// <see cref="VirtualInputMode"/> (safe for any runner).  Only move tests here
/// when they genuinely require a real input device.
/// </para>
/// </remarks>
public class TestAppRealInputTests : IAsyncLifetime
{
    /// <summary>
    /// Absolute path to the WPF test-app binary, resolved relative to the
    /// E2E test assembly's output directory.
    /// </summary>
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
                CloseBehavior = new DismissDialogCloseBehavior("Don't Save"),
                InputMode = new RealInputMode(),
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

    // ── Double-click ──────────────────────────────────────────────────────────

    /// <summary>
    /// Double-clicking <c>btnDoubleClick</c> sets <c>lblOutput</c> to
    /// <c>"DoubleClicked"</c>, verifying that <c>MouseDoubleClick</c> is fired
    /// correctly by Flawright when using real mouse input.
    ///
    /// This test requires <see cref="RealInputMode"/> because WPF
    /// <c>MouseDoubleClick</c> is only raised by genuine consecutive mouse clicks;
    /// UIA has no generic double-click pattern equivalent.
    /// </summary>
    [Fact]
    public async Task DoubleClick_TriggersHandler()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.DoubleClickAsync("#btnDoubleClick");

        await page.Locator("#lblOutput").Expect().ToHaveTextAsync("DoubleClicked");
    }

    // ── Enter key on button ───────────────────────────────────────────────────

    /// <summary>
    /// Pressing <c>Enter</c> on a focused button fires its <c>Click</c> handler,
    /// setting <c>lblOutput</c> to <c>"Clicked"</c>.
    ///
    /// This test demonstrates deterministic <see cref="PressAsync"/> behaviour
    /// under <see cref="RealInputMode"/>: a single key with no chord timing
    /// dependency and a UIA-observable outcome.
    /// </summary>
    [Fact]
    public async Task EnterKey_OnFocusedButton_TriggersClick()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("#btnClick").FocusAsync();
        await page.Locator("#btnClick").PressAsync("Enter");

        await page.Locator("#lblOutput").Expect().ToHaveTextAsync("Clicked");
    }
}
