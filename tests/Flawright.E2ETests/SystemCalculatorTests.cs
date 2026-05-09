using Flawright;
using Flawright.InputModes;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// Opt-in E2E tests for the Windows Calculator application.
/// Each test skips automatically with a descriptive reason when the
/// Calculator AppX package is not installed on the current machine.
/// </summary>
/// <remarks>
/// <para>
/// These tests are intentionally opt-in: <c>windows-latest</c> CI runners
/// (Windows Server 2025) do not ship the UWP Calculator, so every test
/// skips with a human-readable message rather than failing.  They run
/// automatically on developer machines and Windows 11 Pro CI runners
/// where Calculator is installed.
/// </para>
/// <para>
/// For deterministic, always-on E2E coverage use
/// <see cref="TestAppTests"/> which targets the repo-shipped WPF test app.
/// </para>
/// <para>
/// Uses <see cref="VirtualInputMode"/> — all actions use UIA patterns
/// (no focus-steal, no cursor movement), safe for CI runners.
/// </para>
/// </remarks>
public class SystemCalculatorTests : IAsyncLifetime
{
    private const string CalculatorAumid = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App";

    private IFlawright? _fw;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // VirtualInputMode: drives Calculator via UIA patterns — no focus-steal,
        // no cursor movement, safe for CI runners.
        _fw = await global::Flawright.Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = "calc.exe" },
            new FlawrightOptions
            {
                InputMode = new VirtualInputMode()
            });
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_fw != null)
            await _fw.DisposeAsync();
    }

    /// <summary>Verifies that Calculator launches and a window can be retrieved.</summary>
    [RequiresAppFact(Aumid = CalculatorAumid)]
    public async Task Calculator_LaunchAndFindWindow()
    {
        var page = await _fw!.Browser.NewPageAsync();
        Assert.NotNull(page);
    }

    /// <summary>Counts button controls and confirms at least one exists.</summary>
    [RequiresAppFact(Aumid = CalculatorAumid)]
    public async Task Calculator_FindNumberButtons()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var buttonCount = await page.Locator("controltype:Button").CountAsync();
        Assert.True(buttonCount > 0, "Calculator should have buttons");
    }

    /// <summary>Clicks the "3" number button via its automation ID.</summary>
    /// <remarks>
    /// WinUI 3 Calculator (Windows 11) uses <c>num3Button</c> as the UIA AutomationId
    /// for the digit-3 button.  The old Win32-era Calculator used a bare <c>3</c> as
    /// its AutomationId; that name no longer exists in the WinUI 3 version.
    /// </remarks>
    [RequiresAppFact(Aumid = CalculatorAumid)]
    public async Task Calculator_ClickButton()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // WinUI 3 Calculator uses "num3Button" as the AutomationId for the digit-3 button.
        await page.Locator("#num3Button").First.ClickAsync();
        // Clicking without error means the API works
    }

    /// <summary>Takes a screenshot of the Calculator window.</summary>
    [RequiresAppFact(Aumid = CalculatorAumid)]
    public async Task Calculator_Screenshot()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var screenshot = await page.ScreenshotAsync();
        Assert.NotNull(screenshot);
        Assert.True(screenshot.Length > 0);
    }

    /// <summary>Asserts that at least one Calculator button is present in the UIA tree.</summary>
    /// <remarks>
    /// Uses <c>ToBeAttachedAsync</c> rather than <c>ToBeVisibleAsync</c> because WinUI 3
    /// Calculator buttons can report <c>IsOffscreen = true</c> through FlaUI even when
    /// they are fully rendered and interactive.  <c>IsAttached</c> (element exists in the
    /// UIA tree) is the reliable signal here, consistent with the same workaround applied
    /// in <see cref="SystemNotepadTests.Notepad_ExpectToBeVisible"/>.
    /// </remarks>
    [RequiresAppFact(Aumid = CalculatorAumid)]
    public async Task Calculator_ExpectButtonsToBeVisible()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("controltype:Button").Expect().ToBeAttachedAsync();
    }
}
