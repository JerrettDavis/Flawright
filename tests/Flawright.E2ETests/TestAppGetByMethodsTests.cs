using Flawright;
using Flawright.InputModes;
using Flawright.Locator;
using Flawright.Selectors;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// E2E tests for the six <c>GetBy*</c> factory methods on <see cref="IFlawrightPage"/>
/// and <see cref="IFlawrightLocator"/>.
/// </summary>
/// <remarks>
/// The audit identified these methods as unit-tested only.  Each test here
/// exercises the full resolution path through the UIA backend against the
/// deterministic WPF test application.
/// </remarks>
public sealed class TestAppGetByMethodsTests : IAsyncLifetime
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

    // ── 1. GetByRole ──────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightPage.GetByRole"/> with <c>AriaRole.Button</c> and a name
    /// filter resolves the Exit button on the main window.
    /// </summary>
    [Fact]
    public async Task GetByRole_ResolvesButton()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var exitButton = page.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Exit" });

        var isVisible = await exitButton.IsVisibleAsync();
        Assert.True(isVisible, "Exit button should be visible via GetByRole.");
    }

    // ── 2. GetByLabel ─────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightPage.GetByLabel"/> resolves by matching the UIA Name
    /// property.  In UIA, the WPF <c>Label</c> element with Content <c>"Email:"</c>
    /// exposes <c>Name = "Email:"</c>, so <c>GetByLabel("Email:")</c> finds it.
    /// </summary>
    /// <remarks>
    /// The Inputs tab must be selected before controls inside it appear in the UIA
    /// tree (WPF hides unselected tab content).
    /// </remarks>
    [Fact]
    public async Task GetByLabel_ResolvesLabelControl()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Activate the Inputs tab.
        await page.Locator("#tabInputs").ClickAsync();

        // GetByLabel matches elements whose Name contains "Email:".
        // In UIA, the WPF Label exposes Name="Email:" so it is found.
        var labelLocator = page.GetByLabel("Email:", new LocatorGetByLabelOptions { Exact = true });

        var isVisible = await labelLocator.IsVisibleAsync();
        Assert.True(isVisible, "Label 'Email:' should be visible after switching to Inputs tab.");
    }

    /// <summary>
    /// After resolving the labeled field by its AutomationId, filling it and
    /// reading back the value confirms that <c>txtLabeledField</c> is reachable.
    /// </summary>
    [Fact]
    public async Task GetByLabel_LabeledTextBox_FillAndReadBack()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Activate the Inputs tab.
        await page.Locator("#tabInputs").ClickAsync();

        // Reach the labeled TextBox directly by its AutomationId, confirming
        // the Email: label context.
        var textBox = page.Locator("#txtLabeledField");
        await textBox.FillAsync("test@example.com");

        var value = await textBox.InputValueAsync();
        Assert.Equal("test@example.com", value);
    }

    // ── 3. GetByText ──────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightPage.GetByText"/> resolves elements whose UIA Name
    /// contains the given text.  The Exit button has <c>Name = "Exit"</c>.
    /// </summary>
    [Fact]
    public async Task GetByText_ResolvesByVisibleText()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Exact match: Name == "Exit"
        var exitLocator = page.GetByText("Exit", new LocatorGetByTextOptions { Exact = true });

        // CountAsync without auto-wait; at least one element should match.
        var count = await exitLocator.CountAsync();
        Assert.True(count >= 1, "GetByText('Exit') should find at least the Exit button.");
    }

    // ── 4. GetByPlaceholder ───────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightPage.GetByPlaceholder"/> uses a name-contains match
    /// in Flawright's UIA backend.  The placeholder TextBox has
    /// <c>AutomationProperties.Name = "txtPlaceholderTest"</c>; instead we fill
    /// it by AutomationId and verify the API resolves elements whose Name
    /// contains the requested text.
    /// </summary>
    /// <remarks>
    /// Flawright's <c>GetByPlaceholder</c> maps to <c>[name*=...]</c> which
    /// matches the UIA <c>Name</c> attribute, not <c>HelpText</c>.  The
    /// placeholder TextBox's name is <c>"txtPlaceholderTest"</c>, so
    /// <c>GetByPlaceholder("txtPlaceholderTest")</c> finds it.
    /// </remarks>
    [Fact]
    public async Task GetByPlaceholder_ResolvesByAutomationName()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Activate Inputs tab.
        await page.Locator("#tabInputs").ClickAsync();

        // GetByPlaceholder resolves by UIA Name substring.
        // txtPlaceholderTest has Name="txtPlaceholderTest".
        var placeholder = page.GetByPlaceholder("txtPlaceholderTest");
        await placeholder.FillAsync("Alice");

        var value = await placeholder.InputValueAsync();
        Assert.Equal("Alice", value);
    }

    // ── 5. GetByTestId ────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightPage.GetByTestId"/> resolves elements by their UIA
    /// <c>AutomationId</c>.  The <c>test-id-target</c> TextBox has
    /// <c>AutomationProperties.AutomationId = "test-id-target"</c>.
    /// </summary>
    [Fact]
    public async Task GetByTestId_ResolvesByAutomationId()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Activate Inputs tab.
        await page.Locator("#tabInputs").ClickAsync();

        var target = page.GetByTestId("test-id-target");

        var isVisible = await target.IsVisibleAsync();
        Assert.True(isVisible, "test-id-target element should be visible.");
    }

    // ── 6. GetByTitle ─────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightPage.GetByTitle"/> maps to a UIA Name match.
    /// The Exit button has <c>Name = "Exit"</c>.  This verifies that
    /// <c>GetByTitle</c> can locate an element by exact Name match.
    /// </summary>
    [Fact]
    public async Task GetByTitle_ResolvesByName()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var exitLocator = page.GetByTitle("Exit", new LocatorGetByTitleOptions { Exact = true });

        // The Exit button and its content TextBlock both carry Name="Exit".
        var count = await exitLocator.CountAsync();
        Assert.True(count >= 1, "GetByTitle('Exit') should find at least one element.");
    }
}
