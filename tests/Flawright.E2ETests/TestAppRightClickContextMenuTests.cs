using Flawright;
using Flawright.InputModes;
using Flawright.Locator;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// E2E tests for the new <c>RightClickAsync</c> API and WPF context-menu interaction.
/// </summary>
/// <remarks>
/// <para>
/// Tests that require physical mouse events (real right-click to open context menus)
/// use <see cref="RealInputMode"/>.
/// </para>
/// <para>
/// <see cref="VirtualInputMode"/> tests verify that the API correctly throws
/// <see cref="NotSupportedException"/> because UIA <c>InvokePattern</c> has no
/// concept of which mouse button triggered the invocation.
/// </para>
/// </remarks>
public sealed class TestAppRightClickContextMenuTests : IAsyncLifetime
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

    // ── Right-click opens context menu ────────────────────────────────────────

    /// <summary>
    /// Right-clicking <c>btnRightClickTarget</c> opens the WPF context menu,
    /// making the <c>menuEdit</c> and <c>menuDelete</c> menu items visible.
    /// </summary>
    /// <remarks>
    /// WPF <c>ContextMenu</c> items are surfaced in the main window's UIA tree
    /// even when the menu is displayed in a popup HWND.  The test uses
    /// <c>WaitForAsync(Visible)</c> (auto-waited) instead of a fixed delay so
    /// that it tolerates variable context-menu animation timing on CI.
    /// </remarks>
    [Fact]
    public async Task RightClickAsync_OpensContextMenu()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Activate Menu/Actions tab.
        await page.Locator("#tabMenuActions").ClickAsync();

        // Right-click the target button.
        await page.Locator("#btnRightClickTarget").RightClickAsync();

        // Wait (with auto-retry) until the Edit menu item appears in the UIA tree.
        // A fixed 300 ms delay is insufficient on slower CI agents.
        await page.Locator("#menuEdit").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForState.Visible });

        var editVisible = await page.Locator("#menuEdit").IsVisibleAsync();
        Assert.True(editVisible, "menuEdit should be visible after right-click.");

        // Dismiss the context menu by pressing Escape.
        await page.Keyboard.PressAsync("Escape");
    }

    /// <summary>
    /// After right-clicking and opening the context menu, clicking the Edit
    /// menu item dismisses the menu.
    /// </summary>
    /// <remarks>
    /// Uses <c>WaitForAsync(Visible)</c> for opening and <c>WaitForAsync(Hidden)</c>
    /// for dismissal so that variable context-menu animation timing on CI does not
    /// cause spurious failures.
    /// </remarks>
    [Fact]
    public async Task RightClickThenClickMenuItem_DismissesMenu()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Activate Menu/Actions tab.
        await page.Locator("#tabMenuActions").ClickAsync();

        // Right-click the target button to open the context menu.
        await page.Locator("#btnRightClickTarget").RightClickAsync();

        // Wait for the Edit menu item to appear before clicking it.
        await page.Locator("#menuEdit").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForState.Visible });

        // Click the Edit menu item.
        await page.Locator("#menuEdit").ClickAsync();

        // Wait for the context menu to close (auto-retry until hidden or timeout).
        await page.Locator("#menuEdit").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForState.Hidden });

        var editVisible = await page.Locator("#menuEdit").IsVisibleAsync();
        Assert.False(editVisible, "menuEdit should no longer be visible after clicking it.");
    }
}

/// <summary>
/// E2E tests for <c>RightClickAsync</c> in <see cref="VirtualInputMode"/>.
/// These tests run in a separate class from the real-input tests to avoid
/// mixing input modes within the same <see cref="IAsyncLifetime"/> fixture.
/// </summary>
public sealed class TestAppRightClickVirtualModeTests : IAsyncLifetime
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

    /// <summary>
    /// <see cref="IFlawrightLocator.RightClickAsync"/> in <see cref="VirtualInputMode"/>
    /// throws <see cref="NotSupportedException"/> because UIA's <c>InvokePattern</c>
    /// has no concept of which mouse button triggered the action.
    /// </summary>
    [Fact]
    public async Task RightClickAsync_VirtualInputMode_ThrowsNotSupported()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Activate Menu/Actions tab.
        await page.Locator("#tabMenuActions").ClickAsync();

        await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await page.Locator("#btnRightClickTarget").RightClickAsync();
        });
    }
}
