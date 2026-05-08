using JerrettDavis.Flawright.InputModes;
using JerrettDavis.Flawright.UnitTests.Fakes;
using JerrettDavis.Flawright.UnitTests.Locator;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.InputModes;

/// <summary>
/// Integration-style tests that verify the configured <see cref="IInputMode"/>
/// is honoured end-to-end through <see cref="FlawrightLocator"/> and
/// <see cref="FlawrightElement"/>.
/// </summary>
public sealed class InputModeIntegrationTests
{
    // ── FlawrightLocator — VirtualInputMode wiring ────────────────────────────

    [Fact]
    public async Task ClickAsync_WithVirtualMode_CallsTryInvoke_NotElementClick()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        // Configure TryInvoke to succeed.
        var button = (FakeElementBackend)root.Children[0];
        button.TryInvokeResult = true;

        var locator = LocatorTestBase.CreateLocator(
            "controltype:Button",
            root,
            inputMode: new VirtualInputMode());

        await locator.ClickAsync();

        Assert.Equal(1, button.InvokeCount);   // TryInvoke was called
        Assert.Equal(0, button.ClickCount);    // real Click was NOT called
    }

    [Fact]
    public async Task ClickAsync_WithRealMode_CallsElementClick_NotTryInvoke()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();
        var button = (FakeElementBackend)root.Children[0];

        var locator = LocatorTestBase.CreateLocator(
            "controltype:Button",
            root,
            inputMode: new RealInputMode());

        await locator.ClickAsync();

        Assert.Equal(1, button.ClickCount);    // real Click WAS called
        Assert.Equal(0, button.InvokeCount);   // TryInvoke was NOT called
    }

    [Fact]
    public async Task TypeAsync_WithVirtualMode_CallsTrySetValue_NotKeyboardType()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Edit("Name").WithValue(""))
            .Build();
        var edit = (FakeElementBackend)root.Children[0];
        var input = new FakeInputBackend();

        var locator = LocatorTestBase.CreateLocator(
            "controltype:Edit",
            root,
            input: input,
            inputMode: new VirtualInputMode());

        await locator.TypeAsync("hello");

        Assert.Contains("hello", edit.Inputs);   // ValuePattern.SetValue was called
        Assert.Empty(input.TypedTexts);           // KeyboardType was NOT called
    }

    [Fact]
    public async Task TypeAsync_WithRealMode_CallsKeyboardType_NotSetValue()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Edit("Name").WithValue(""))
            .Build();
        var edit = (FakeElementBackend)root.Children[0];
        var input = new FakeInputBackend();

        var locator = LocatorTestBase.CreateLocator(
            "controltype:Edit",
            root,
            input: input,
            inputMode: new RealInputMode());

        await locator.TypeAsync("hello");

        Assert.Contains("hello", input.TypedTexts);  // KeyboardType WAS called
        Assert.DoesNotContain("hello", edit.Inputs); // SetValue was NOT called (focus() is recorded separately)
    }

    [Fact]
    public async Task HoverAsync_WithVirtualMode_ThrowsNotSupportedException()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();

        var locator = LocatorTestBase.CreateLocator(
            "controltype:Button",
            root,
            inputMode: new VirtualInputMode());

        await Assert.ThrowsAsync<NotSupportedException>(() => locator.HoverAsync());
    }

    [Fact]
    public async Task PressAsync_WithVirtualMode_ThrowsNotSupportedException()
    {
        var root = UiaTree.Window("App")
            .WithChild(UiaTree.Button("OK"))
            .Build();

        var locator = LocatorTestBase.CreateLocator(
            "controltype:Button",
            root,
            inputMode: new VirtualInputMode());

        await Assert.ThrowsAsync<NotSupportedException>(() => locator.PressAsync("Enter"));
    }

    // ── FlawrightElement — VirtualInputMode wiring ────────────────────────────

    [Fact]
    public async Task FlawrightElement_ClickAsync_WithVirtualMode_CallsTryInvoke()
    {
        var backend = new FakeElementBackend(name: "OK", controlTypeName: "Button");
        backend.TryInvokeResult = true;
        var input = new FakeInputBackend();
        var element = new FlawrightElement(backend, input, new VirtualInputMode());

        await element.ClickAsync();

        Assert.Equal(1, backend.InvokeCount);
        Assert.Equal(0, backend.ClickCount);
    }

    [Fact]
    public async Task FlawrightElement_DoubleClickAsync_WithVirtualMode_Throws()
    {
        var backend = new FakeElementBackend(name: "OK", controlTypeName: "Button");
        var element = new FlawrightElement(backend, new FakeInputBackend(), new VirtualInputMode());

        await Assert.ThrowsAsync<NotSupportedException>(() => element.DoubleClickAsync());
    }

    // ── FlawrightOptions default — RealInputMode ─────────────────────────────

    [Fact]
    public void FlawrightOptions_Default_HasRealInputMode()
    {
        var options = new FlawrightOptions();

        Assert.IsType<RealInputMode>(options.InputMode);
    }

    [Fact]
    public void FlawrightOptions_WithVirtualInputMode_CanBeConfigured()
    {
        var options = new FlawrightOptions { InputMode = new VirtualInputMode() };

        Assert.IsType<VirtualInputMode>(options.InputMode);
    }
}
