using Flawright;
using Flawright.InputModes;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// E2E tests validating <see cref="RealInputMode"/> <c>TypeAsync</c>:
/// keystrokes are synthesised per-character via <c>SendInput</c>, exercising
/// any per-keystroke event handlers in the target application.
/// </summary>
/// <remarks>
/// <para>
/// The key behavioral difference between <c>VirtualInputMode + FillAsync</c>
/// (which uses <c>ValuePattern.SetValue</c> atomically) and
/// <c>RealInputMode + TypeAsync</c> is that the latter fires a
/// <c>KeyDown</c> / <c>KeyUp</c> event for every character, allowing the
/// target app to intercept or validate input as it arrives.
/// </para>
/// <para>
/// This class must NOT be merged with any <see cref="VirtualInputMode"/>
/// fixture — mixing modes in the same <see cref="IAsyncLifetime"/> class
/// is explicitly disallowed by the Flawright E2E conventions.
/// </para>
/// </remarks>
public sealed class TestAppRealInputTypeTests : IAsyncLifetime
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

    // ── RealInputMode TypeAsync ───────────────────────────────────────────────

    /// <summary>
    /// In <see cref="RealInputMode"/>, <see cref="IFlawrightLocator.TypeAsync"/>
    /// delivers one <c>SendInput</c> keystroke per character, which is confirmed
    /// by reading back the accumulated value via <c>InputValueAsync</c>.
    /// </summary>
    /// <remarks>
    /// This test specifically uses the tab-scoped <c>txtPlaceholderTest</c>
    /// TextBox (in the Inputs tab) to confirm that tab navigation to make the
    /// control visible does not interfere with subsequent keystroke delivery.
    /// </remarks>
    [Fact]
    public async Task RealInputMode_TypeAsync_KeystrokesPerCharacter()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Activate the Inputs tab so txtPlaceholderTest is in the UIA tree.
        await page.Locator("#tabInputs").ClickAsync();

        await page.BringToFrontAsync();

        // Focus the placeholder TextBox explicitly before typing.
        await page.Locator("#txtPlaceholderTest").FocusAsync();

        // TypeAsync in RealInputMode = SendInput per character.
        await page.Locator("#txtPlaceholderTest").TypeAsync("Hello");

        var value = await page.Locator("#txtPlaceholderTest").InputValueAsync();
        Assert.NotNull(value);
        Assert.Equal("Hello", value);
    }

    /// <summary>
    /// In <see cref="RealInputMode"/>, typing into the standard (non-tabbed)
    /// <c>txtFill</c> TextBox confirms that the basic real-input type path
    /// works against the always-visible main window controls.
    /// </summary>
    [Fact]
    public async Task RealInputMode_TypeAsync_MainWindowTextBox()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.BringToFrontAsync();

        await page.Locator("#txtType").FocusAsync();
        await page.Locator("#txtType").TypeAsync("RealType");

        var value = await page.Locator("#txtType").InputValueAsync();
        Assert.NotNull(value);
        Assert.Equal("RealType", value);
    }
}
