#pragma warning disable MA0009 // test regexes are safe

using Flawright.Backends;
using Flawright.Locator;
using Flawright.Selectors;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Page;

/// <summary>
/// Unit tests for <see cref="FlawrightPage"/>.
/// </summary>
public sealed class FlawrightPageTests
{
    private static readonly FlawrightOptions FastOpts = new()
    {
        DefaultTimeout = TimeSpan.FromMilliseconds(200),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(10),
    };

    private static FlawrightPage MakePage(
        FakeElementBackend? root = null,
        FakeInputBackend? input = null,
        FakeConditionTranslator? translator = null,
        FlawrightOptions? opts = null)
    {
        return new FlawrightPage(
            root ?? new FakeElementBackend(name: "TestWindow"),
            input ?? new FakeInputBackend(),
            opts ?? FastOpts,
            translator ?? new FakeConditionTranslator());
    }

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsOptions()
    {
        var opts = new FlawrightOptions { DefaultTimeout = TimeSpan.FromSeconds(99) };
        var page = MakePage(opts: opts);
        Assert.Same(opts, page.Options);
    }

    // ── TitleAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task TitleAsync_ReturnsWindowName()
    {
        var page = MakePage(root: new FakeElementBackend(name: "My Window"));
        var title = await page.TitleAsync();
        Assert.Equal("My Window", title);
    }

    [Fact]
    public async Task TitleAsync_ReturnsEmpty_WhenNameIsNull()
    {
        var page = MakePage(root: new FakeElementBackend(name: null));
        var title = await page.TitleAsync();
        Assert.Equal(string.Empty, title);
    }

    // ── BringToFrontAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task BringToFrontAsync_FocusesRootBackend()
    {
        var root = new FakeElementBackend(name: "Win");
        var page = MakePage(root: root);
        await page.BringToFrontAsync();
        Assert.Equal(1, root.FocusCount);
    }

    [Fact]
    public async Task BringToFrontAsync_DoesNotThrow()
    {
        var page = MakePage();
        await page.BringToFrontAsync(); // Should complete without throwing
    }

    // ── WaitForTimeoutAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task WaitForTimeoutAsync_WaitsApproximately()
    {
        var page = MakePage();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await page.WaitForTimeoutAsync(50);
        sw.Stop();
        // Should have waited at least 40ms (50ms nominal, some slack for scheduling)
        Assert.True(sw.ElapsedMilliseconds >= 40);
    }

    [Fact]
    public async Task WaitForTimeoutAsync_RespectsCancellation()
    {
        var page = MakePage();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => page.WaitForTimeoutAsync(10000, cts.Token));
    }

    // ── Locator factory ───────────────────────────────────────────────────────

    [Fact]
    public void Locator_ReturnsNonNull()
    {
        var page = MakePage();
        var loc = page.Locator("Button");
        Assert.NotNull(loc);
    }

    [Fact]
    public void Locator_HasCorrectSelector()
    {
        var page = MakePage();
        var loc = page.Locator("controltype:Button");
        Assert.Equal("controltype:Button", loc.Selector);
    }

    [Fact]
    public void Locator_ThrowsOnEmpty()
    {
        var page = MakePage();
        Assert.Throws<ArgumentException>(() => page.Locator(""));
    }

    [Fact]
    public void Locator_ThrowsOnNull()
    {
        var page = MakePage();
        Assert.Throws<ArgumentNullException>(() => page.Locator(null!));
    }

    // ── GetBy* methods ────────────────────────────────────────────────────────

    [Fact]
    public void GetByRole_ReturnsLocator()
    {
        var page = MakePage();
        var loc = page.GetByRole(AriaRole.Button);
        Assert.NotNull(loc);
    }

    [Fact]
    public void GetByLabel_ReturnsLocatorWithSelectorContainingLabel()
    {
        var page = MakePage();
        var loc = page.GetByLabel("Save");
        Assert.NotNull(loc);
        Assert.Contains("Save", loc.Selector);
    }

    [Fact]
    public void GetByText_ReturnsLocatorWithSelectorContainingText()
    {
        var page = MakePage();
        var loc = page.GetByText("Click me");
        Assert.NotNull(loc);
        Assert.Contains("Click me", loc.Selector);
    }

    [Fact]
    public void GetByTestId_ReturnsLocatorWithTestId()
    {
        var page = MakePage();
        var loc = page.GetByTestId("btn-save");
        Assert.NotNull(loc);
        Assert.Contains("btn-save", loc.Selector);
    }

    [Fact]
    public void GetByPlaceholder_ReturnsLocator()
    {
        var page = MakePage();
        var loc = page.GetByPlaceholder("Enter name");
        Assert.NotNull(loc);
    }

    [Fact]
    public void GetByTitle_ReturnsLocator()
    {
        var page = MakePage();
        var loc = page.GetByTitle("My tooltip");
        Assert.NotNull(loc);
    }

    // ── Mouse / Keyboard sub-APIs ─────────────────────────────────────────────

    [Fact]
    public void Mouse_ReturnsNonNull()
    {
        var page = MakePage();
        Assert.NotNull(page.Mouse);
    }

    [Fact]
    public void Mouse_ReturnsSameInstance()
    {
        var page = MakePage();
        var m1 = page.Mouse;
        var m2 = page.Mouse;
        Assert.Same(m1, m2);
    }

    [Fact]
    public void Keyboard_ReturnsNonNull()
    {
        var page = MakePage();
        Assert.NotNull(page.Keyboard);
    }

    [Fact]
    public void Keyboard_ReturnsSameInstance()
    {
        var page = MakePage();
        var k1 = page.Keyboard;
        var k2 = page.Keyboard;
        Assert.Same(k1, k2);
    }

    // ── Convenience action methods (delegation to Locator) ────────────────────

    [Fact]
    public async Task ClickAsync_DelegatesToLocator()
    {
        var btn = new FakeElementBackend(name: "OK", controlTypeName: "Button");
        var root = new FakeElementBackend(name: "Win", children: [btn]);
        var page = MakePage(root: root);

        await page.ClickAsync("OK");

        Assert.Equal(1, btn.ClickCount);
    }

    [Fact]
    public async Task DoubleClickAsync_DelegatesToLocator()
    {
        var btn = new FakeElementBackend(name: "OK", controlTypeName: "Button");
        var root = new FakeElementBackend(name: "Win", children: [btn]);
        var page = MakePage(root: root);

        await page.DoubleClickAsync("OK");

        Assert.Equal(1, btn.DoubleClickCount);
    }

    [Fact]
    public async Task FillAsync_DelegatesToLocator()
    {
        var edit = new FakeElementBackend(name: "Input", controlTypeName: "Edit", initialValue: "");
        var root = new FakeElementBackend(name: "Win", children: [edit]);
        var page = MakePage(root: root);

        await page.FillAsync("Input", "hello");

        Assert.Equal("hello", edit.Inputs[0]);
    }

    [Fact]
    public async Task TypeAsync_DelegatesToLocator()
    {
        var input = new FakeInputBackend();
        var edit = new FakeElementBackend(name: "Input", controlTypeName: "Edit");
        var root = new FakeElementBackend(name: "Win", children: [edit]);
        var page = MakePage(root: root, input: input);

        await page.TypeAsync("Input", "world");

        Assert.Contains("world", input.TypedTexts);
    }

    [Fact]
    public async Task PressAsync_DelegatesToLocator()
    {
        var input = new FakeInputBackend();
        var edit = new FakeElementBackend(name: "Input", controlTypeName: "Edit");
        var root = new FakeElementBackend(name: "Win", children: [edit]);
        var page = MakePage(root: root, input: input);

        await page.PressAsync("Input", "Enter");

        Assert.True(input.KeyTaps.Count > 0 || input.KeyPresses.Count > 0);
    }

    [Fact]
    public async Task CheckAsync_DelegatesToLocator()
    {
        var cb = new FakeElementBackend(name: "CB", controlTypeName: "CheckBox", supportsToggle: true, initialToggleState: false);
        var root = new FakeElementBackend(name: "Win", children: [cb]);
        var page = MakePage(root: root);

        await page.CheckAsync("CB");

        Assert.Equal(true, cb.GetToggleState());
    }

    [Fact]
    public async Task UncheckAsync_DelegatesToLocator()
    {
        var cb = new FakeElementBackend(name: "CB", controlTypeName: "CheckBox", supportsToggle: true, initialToggleState: true);
        var root = new FakeElementBackend(name: "Win", children: [cb]);
        var page = MakePage(root: root);

        await page.UncheckAsync("CB");

        Assert.Equal(false, cb.GetToggleState());
    }

    [Fact]
    public async Task SetCheckedAsync_DelegatesToLocator()
    {
        var cb = new FakeElementBackend(name: "CB", controlTypeName: "CheckBox", supportsToggle: true, initialToggleState: false);
        var root = new FakeElementBackend(name: "Win", children: [cb]);
        var page = MakePage(root: root);

        await page.SetCheckedAsync("CB", true);

        Assert.Equal(true, cb.GetToggleState());
    }

    [Fact]
    public async Task HoverAsync_DelegatesToLocator()
    {
        var btn = new FakeElementBackend(name: "Hover", controlTypeName: "Button");
        var root = new FakeElementBackend(name: "Win", children: [btn]);
        var input = new FakeInputBackend();
        var page = MakePage(root: root, input: input);

        await page.HoverAsync("Hover");

        Assert.True(input.MouseMoves.Count > 0);
    }

    [Fact]
    public async Task FocusAsync_DelegatesToLocator()
    {
        var btn = new FakeElementBackend(name: "Focusable", controlTypeName: "Button");
        var root = new FakeElementBackend(name: "Win", children: [btn]);
        var page = MakePage(root: root);

        await page.FocusAsync("Focusable");

        Assert.Equal(1, btn.FocusCount);
    }

    [Fact]
    public async Task SelectOptionAsync_DelegatesToLocator()
    {
        var item = new FakeElementBackend(name: "Apple", controlTypeName: "ListItem");
        var list = new FakeElementBackend(name: "Combo", controlTypeName: "ComboBox", children: [item]);
        var root = new FakeElementBackend(name: "Win", children: [list]);
        var page = MakePage(root: root);

        await page.SelectOptionAsync("Combo", "Apple");

        Assert.Equal("Apple", list.LastSelectedItem);
    }

    [Fact]
    public async Task WaitForSelectorAsync_ReturnsElement()
    {
        var btn = new FakeElementBackend(name: "Save", controlTypeName: "Button");
        var root = new FakeElementBackend(name: "Win", children: [btn]);
        var page = MakePage(root: root);

        var elem = await page.WaitForSelectorAsync("Save");

        Assert.NotNull(elem);
        Assert.Equal("Save", elem.Name);
    }

    [Fact]
    public async Task ScreenshotAsync_ReturnsBytesFromBackend()
    {
        // FakeElementBackend.CaptureScreenshot returns a minimal 1×1 PNG by default,
        // so the result must be non-null and non-empty.
        var page = MakePage();
        var bytes = await page.ScreenshotAsync();
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task ScreenshotAsync_ReturnsConfiguredBytes_WhenBackendOverridden()
    {
        var customBytes = new byte[] { 1, 2, 3, 4, 5 };
        var root = new FakeElementBackend(name: "TestWindow") { ScreenshotBytes = customBytes };
        var page = MakePage(root: root);

        var bytes = await page.ScreenshotAsync();

        Assert.Equal(customBytes, bytes);
    }

    [Fact]
    public async Task ScreenshotAsync_ReturnsEmptyArray_WhenBackendReturnsEmpty()
    {
        var root = new FakeElementBackend(name: "TestWindow") { ScreenshotBytes = Array.Empty<byte>() };
        var page = MakePage(root: root);

        var bytes = await page.ScreenshotAsync();

        Assert.Empty(bytes);
    }

    [Fact]
    public async Task ScreenshotAsync_WritesFileToDisk_WhenPathSet()
    {
        var customBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic bytes
        var root = new FakeElementBackend(name: "TestWindow") { ScreenshotBytes = customBytes };
        var page = MakePage(root: root);
        var path = System.IO.Path.GetTempFileName();
        try
        {
            await page.ScreenshotAsync(new LocatorScreenshotOptions { Path = path });
            Assert.True(System.IO.File.Exists(path));
            Assert.Equal(customBytes, await System.IO.File.ReadAllBytesAsync(path));
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    // ── DisposeAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DisposeAsync_CompletesCleanly()
    {
        var page = MakePage();
        await page.DisposeAsync();
        // Should not throw
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent()
    {
        var page = MakePage();
        await page.DisposeAsync();
        await page.DisposeAsync(); // Second dispose is safe
    }
}
