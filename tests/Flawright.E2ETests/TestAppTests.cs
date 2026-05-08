using Flawright;
using Flawright.CloseBehaviors;
using Flawright.InputModes;
using Flawright.Locator;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// Deterministic E2E test suite that exercises Flawright's full action surface
/// against the repo-shipped WPF test application
/// (<c>Flawright.E2ETests.TestApp.exe</c>).
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="SystemNotepadTests"/> and <see cref="SystemCalculatorTests"/>,
/// these tests have NO prerequisites beyond a working .NET runtime.  The test
/// app is built automatically as part of the E2E test project build and copied
/// to <c>$(OutputPath)TestApp\</c> — it is always available.
/// </para>
/// <para>
/// This is the recommended pattern for Flawright E2E testing in CI:
/// ship your own controlled WPF/WinForms target so every test is
/// deterministic regardless of the runner's installed applications.
/// </para>
/// <para>
/// Uses <see cref="VirtualInputMode"/> throughout to avoid focus-steal and
/// cursor movement — safe for parallel and headless CI runners.
/// </para>
/// </remarks>
public class TestAppTests : IAsyncLifetime
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

    // ── Click ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Clicking <c>btnClick</c> sets <c>lblOutput</c> to <c>"Clicked"</c>.
    /// Exercises <see cref="IFlawrightLocator.ClickAsync()"/> and
    /// <see cref="IFlawrightAssertions.ToHaveTextAsync(string, Flawright.Assertions.AssertionsToHaveTextOptions?, CancellationToken)"/>.
    /// </summary>
    [Fact]
    public async Task Click_TriggersHandler()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("#btnClick").ClickAsync();

        await page.Locator("#lblOutput").Expect().ToHaveTextAsync("Clicked");
    }

    // ── Fill ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightPage.FillAsync"/> sets the TextBox value atomically
    /// via <c>ValuePattern</c>.
    /// </summary>
    [Fact]
    public async Task Fill_PopulatesTextBox()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.FillAsync("#txtFill", "Hello Fill");

        var value = await page.Locator("#txtFill").InputValueAsync();
        Assert.Equal("Hello Fill", value);
    }

    // ── Type ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightPage.TypeAsync"/> types text character-by-character
    /// into <c>txtType</c>.
    /// </summary>
    [Fact]
    public async Task Type_PopulatesTextBox()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.TypeAsync("#txtType", "Hello Type");

        var value = await page.Locator("#txtType").InputValueAsync();
        Assert.Equal("Hello Type", value);
    }

    // ── Check / Uncheck ───────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightLocator.CheckAsync()"/> sets the CheckBox to the
    /// <c>On</c> state; <see cref="IFlawrightLocator.UncheckAsync()"/> clears it.
    /// </summary>
    [Fact]
    public async Task Check_TogglesCheckBox()
    {
        var page = await _fw!.Browser.NewPageAsync();
        var checkbox = page.Locator("#chkToggle");

        await checkbox.CheckAsync();
        Assert.True(await checkbox.IsCheckedAsync());

        await checkbox.UncheckAsync();
        Assert.False(await checkbox.IsCheckedAsync());
    }

    // ── SelectOption ──────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightLocator.SelectOptionAsync(string, LocatorSelectOptionOptions?, CancellationToken)"/>
    /// sets <c>cmbSelect</c> to the specified item by name.
    /// </summary>
    [Fact]
    public async Task SelectOption_SetsComboBoxValue()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.SelectOptionAsync("#cmbSelect", "Option B");

        var value = await page.Locator("#cmbSelect").InputValueAsync();
        Assert.Equal("Option B", value);
    }

    // ── Filter ────────────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightLocator.Filter"/> with <c>HasText</c> narrows the
    /// list items to only those matching the substring (case-insensitive).
    /// The "Apple" filter should match both "Apple" and "Apple Pie" but not
    /// "Banana" or "Cherry".
    /// </summary>
    [Fact]
    public async Task Filter_NarrowsListItems()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var appleItems = page
            .Locator("#lstFilter")
            .Locator("controltype:ListItem")
            .Filter(new LocatorFilterOptions { HasText = "Apple" });

        var count = await appleItems.CountAsync();
        Assert.Equal(2, count);
    }

    // ── Locator by AutomationId ───────────────────────────────────────────────

    /// <summary>
    /// The <c>#id</c> selector resolves elements by <c>AutomationId</c>.
    /// </summary>
    [Fact]
    public async Task Locator_FindsByAutomationId()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var count = await page.Locator("#btnClick").CountAsync();
        Assert.Equal(1, count);
    }

    // ── Locator by Name ───────────────────────────────────────────────────────

    /// <summary>
    /// <c>name:</c>-prefixed selectors match elements whose UIA <c>Name</c>
    /// property equals the given string.
    /// </summary>
    [Fact]
    public async Task Locator_FindsByName()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var count = await page.Locator("name:Click Me").CountAsync();
        Assert.Equal(1, count);
    }

    // ── Screenshot ────────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightPage.ScreenshotAsync()"/> returns a non-empty byte
    /// array whose first 8 bytes match the PNG signature.
    /// </summary>
    [Fact]
    public async Task Screenshot_ReturnsNonEmptyPng()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var bytes = await page.ScreenshotAsync();

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0, "Screenshot must not be empty.");

        // PNG file signature: 89 50 4E 47 0D 0A 1A 0A
        ReadOnlySpan<byte> pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        Assert.True(bytes.Length >= 8, "Screenshot is shorter than the PNG header.");
        Assert.True(bytes.AsSpan()[..8].SequenceEqual(pngSignature), "Screenshot does not begin with the PNG signature.");
    }

    // ── DismissDialogCloseBehavior ────────────────────────────────────────────

    /// <summary>
    /// Clicking <c>btnShowDialog</c> opens a "Save changes?" modal.
    /// <see cref="DismissDialogCloseBehavior"/> should dismiss the "Don't Save"
    /// button automatically when <see cref="IFlawrightBrowser.CloseAsync"/> is
    /// called, allowing the application to exit cleanly.
    /// </summary>
    [Fact]
    public async Task DismissDialogCloseBehavior_HandlesModalDialog()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Open the "Save changes?" dialog — this leaves it open.
        await page.Locator("#btnShowDialog").ClickAsync();

        // CloseAsync triggers DismissDialogCloseBehavior: it finds "Don't Save"
        // and clicks it, which closes both the dialog and the owner window.
        // A successful, non-throwing return is the assertion.
        var exited = await _fw.Browser.CloseAsync(TimeSpan.FromSeconds(10));
        Assert.True(exited, "Application should have exited after dialog was dismissed.");

        // Null out _fw so DisposeAsync does not try to close it again.
        _fw = null;
    }

    // ── Title ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// The window title should exactly match the value set in XAML.
    /// </summary>
    [Fact]
    public async Task Title_ReturnsWindowTitle()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var title = await page.TitleAsync();
        Assert.Equal("Flawright Test App", title);
    }

    // ── RadioButton ───────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="IFlawrightLocator.CheckAsync()"/> on a RadioButton selects it
    /// (sets its toggle state to <c>On</c>).
    /// </summary>
    [Fact]
    public async Task RadioButton_Check_SelectsRadio()
    {
        var page = await _fw!.Browser.NewPageAsync();
        var radio2 = page.Locator("#radio2");

        await radio2.CheckAsync();

        Assert.True(await radio2.IsCheckedAsync());
    }

    // ── Double-click ──────────────────────────────────────────────────────────

    /// <summary>
    /// Double-clicking <c>btnDoubleClick</c> sets <c>lblOutput</c> to
    /// <c>"DoubleClicked"</c>, verifying that <c>MouseDoubleClick</c> is fired
    /// correctly by Flawright.
    /// </summary>
    [Fact]
    public async Task DoubleClick_TriggersHandler()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.DoubleClickAsync("#btnDoubleClick");

        await page.Locator("#lblOutput").Expect().ToHaveTextAsync("DoubleClicked");
    }

    // ── Exit button ───────────────────────────────────────────────────────────

    /// <summary>
    /// Clicking <c>btnExit</c> calls <c>Application.Current.Shutdown()</c>.
    /// <see cref="IFlawrightBrowser.CloseAsync"/> should return <see langword="true"/>
    /// because the process exits cleanly without needing dialog dismissal.
    /// </summary>
    [Fact]
    public async Task ExitButton_ShutsDownApplication()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("#btnExit").ClickAsync();

        var exited = await _fw.Browser.CloseAsync(TimeSpan.FromSeconds(10));
        Assert.True(exited, "Application should have exited after Exit button was clicked.");

        // Null out _fw so DisposeAsync does not try to close it again.
        _fw = null;
    }
}
