using System.Drawing;
using Flawright.Backends;
using Flawright.Locator;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.Locator;

/// <summary>
/// Comprehensive unit tests for <see cref="FlawrightElement"/>.
///
/// All tests use <see cref="FakeElementBackend"/> and <see cref="FakeInputBackend"/>
/// constructed via <see cref="UiaTree"/>. No FlaUI dependency.
/// </summary>
public sealed class FlawrightElementTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Construction and identity properties
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AutomationId_ReturnsBackendValue()
    {
        var backend = UiaTree.Button("OK").WithAutomationId("btn_ok").Build();
        var element = CreateElement(backend);

        Assert.Equal("btn_ok", element.AutomationId);
    }

    [Fact]
    public void AutomationId_ReturnsNull_WhenNotSet()
    {
        var backend = UiaTree.Button("OK").Build();
        var element = CreateElement(backend);

        Assert.Null(element.AutomationId);
    }

    [Fact]
    public void Name_ReturnsBackendValue()
    {
        var backend = UiaTree.Button("Save").Build();
        var element = CreateElement(backend);

        Assert.Equal("Save", element.Name);
    }

    [Fact]
    public void ClassName_ReturnsBackendValue()
    {
        var backend = UiaTree.Button("OK").WithClassName("MyClass").Build();
        var element = CreateElement(backend);

        Assert.Equal("MyClass", element.ClassName);
    }

    [Fact]
    public void ControlTypeName_ReturnsBackendValue()
    {
        var backend = UiaTree.Button("OK").Build();
        var element = CreateElement(backend);

        Assert.Equal("Button", element.ControlTypeName);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // InnerTextAsync / TextContentAsync — priority order
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task InnerTextAsync_ReturnsValuePatternValue_WhenAvailable()
    {
        var backend = UiaTree.Edit("MyEditor").WithValue("hello").Build();
        var element = CreateElement(backend);

        var result = await element.InnerTextAsync();

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task InnerTextAsync_FallsBackToName_WhenValuePatternNull()
    {
        var backend = UiaTree.Button("Click Me").Build();
        var element = CreateElement(backend);

        var result = await element.InnerTextAsync();

        Assert.Equal("Click Me", result);
    }

    [Fact]
    public async Task InnerTextAsync_ReturnsEmptyString_WhenAllSourcesNull()
    {
        var backend = new FakeElementBackend(name: null);
        var element = CreateElement(backend);

        var result = await element.InnerTextAsync();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task TextContentAsync_ReturnsValuePatternValue_WhenAvailable()
    {
        var backend = UiaTree.Edit("Editor").WithValue("world").Build();
        var element = CreateElement(backend);

        var result = await element.TextContentAsync();

        Assert.Equal("world", result);
    }

    [Fact]
    public async Task TextContentAsync_FallsBackToName()
    {
        var backend = UiaTree.Button("Submit").Build();
        var element = CreateElement(backend);

        var result = await element.TextContentAsync();

        Assert.Equal("Submit", result);
    }

    [Fact]
    public async Task TextContentAsync_ReturnsNull_WhenAllSourcesNull()
    {
        var backend = new FakeElementBackend(name: null);
        var element = CreateElement(backend);

        var result = await element.TextContentAsync();

        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // InputValueAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task InputValueAsync_ReturnsValuePatternValue_WhenAvailable()
    {
        var backend = UiaTree.Edit("Input").WithValue("typed text").Build();
        var element = CreateElement(backend);

        var result = await element.InputValueAsync();

        Assert.Equal("typed text", result);
    }

    [Fact]
    public async Task InputValueAsync_Throws_WhenNeitherPatternSupported()
    {
        // Use backend with no ValuePattern and no TextPattern support
        var backend = new NoValuePatternBackend();
        var element = new FlawrightElement(backend, new FakeInputBackend());

        await Assert.ThrowsAsync<InvalidOperationException>(() => element.InputValueAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetAttributeAsync — each known name + unknown name fallback
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAttributeAsync_Id_ReturnsAutomationId()
    {
        var backend = UiaTree.Button("B").WithAutomationId("my-id").Build();
        var element = CreateElement(backend);

        Assert.Equal("my-id", await element.GetAttributeAsync("id"));
    }

    [Fact]
    public async Task GetAttributeAsync_AutomationId_ReturnsAutomationId()
    {
        var backend = UiaTree.Button("B").WithAutomationId("aid").Build();
        var element = CreateElement(backend);

        Assert.Equal("aid", await element.GetAttributeAsync("automationid"));
    }

    [Fact]
    public async Task GetAttributeAsync_DataTestId_ReturnsAutomationId()
    {
        var backend = UiaTree.Button("B").WithAutomationId("testid-123").Build();
        var element = CreateElement(backend);

        Assert.Equal("testid-123", await element.GetAttributeAsync("data-testid"));
    }

    [Fact]
    public async Task GetAttributeAsync_Name_ReturnsName()
    {
        var backend = UiaTree.Button("MyButton").Build();
        var element = CreateElement(backend);

        Assert.Equal("MyButton", await element.GetAttributeAsync("name"));
    }

    [Fact]
    public async Task GetAttributeAsync_AriaLabel_ReturnsName()
    {
        var backend = UiaTree.Button("Accessible Name").Build();
        var element = CreateElement(backend);

        Assert.Equal("Accessible Name", await element.GetAttributeAsync("aria-label"));
    }

    [Fact]
    public async Task GetAttributeAsync_Class_ReturnsClassName()
    {
        var backend = UiaTree.Button("B").WithClassName("MyClass").Build();
        var element = CreateElement(backend);

        Assert.Equal("MyClass", await element.GetAttributeAsync("class"));
    }

    [Fact]
    public async Task GetAttributeAsync_ControlType_ReturnsControlTypeName()
    {
        var backend = UiaTree.Button("B").Build();
        var element = CreateElement(backend);

        Assert.Equal("Button", await element.GetAttributeAsync("controltype"));
    }

    [Fact]
    public async Task GetAttributeAsync_Value_ReturnsTryGetValue()
    {
        var backend = UiaTree.Edit("E").WithValue("myvalue").Build();
        var element = CreateElement(backend);

        Assert.Equal("myvalue", await element.GetAttributeAsync("value"));
    }

    [Fact]
    public async Task GetAttributeAsync_Enabled_ReturnsTrue_WhenEnabled()
    {
        var backend = UiaTree.Button("B").Build();
        var element = CreateElement(backend);

        Assert.Equal("true", await element.GetAttributeAsync("enabled"));
    }

    [Fact]
    public async Task GetAttributeAsync_Unknown_ReturnsNull()
    {
        var backend = UiaTree.Button("B").Build();
        var element = CreateElement(backend);

        Assert.Null(await element.GetAttributeAsync("no-such-attribute"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Boolean state methods — each method × 2 states
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task IsVisibleAsync_ReturnsTrue_WhenNotOffscreen()
    {
        var backend = UiaTree.Button("B").Build();  // isOffscreen defaults to false
        var element = CreateElement(backend);

        Assert.True(await element.IsVisibleAsync());
    }

    [Fact]
    public async Task IsVisibleAsync_ReturnsFalse_WhenOffscreen()
    {
        var backend = UiaTree.Button("B").AsOffscreen().Build();
        var element = CreateElement(backend);

        Assert.False(await element.IsVisibleAsync());
    }

    [Fact]
    public async Task IsHiddenAsync_ReturnsTrue_WhenOffscreen()
    {
        var backend = UiaTree.Button("B").AsOffscreen().Build();
        var element = CreateElement(backend);

        Assert.True(await element.IsHiddenAsync());
    }

    [Fact]
    public async Task IsHiddenAsync_ReturnsFalse_WhenNotOffscreen()
    {
        var backend = UiaTree.Button("B").Build();
        var element = CreateElement(backend);

        Assert.False(await element.IsHiddenAsync());
    }

    [Fact]
    public async Task IsEnabledAsync_ReturnsTrue_WhenEnabled()
    {
        var backend = UiaTree.Button("B").Build();
        var element = CreateElement(backend);

        Assert.True(await element.IsEnabledAsync());
    }

    [Fact]
    public async Task IsEnabledAsync_ReturnsFalse_WhenDisabled()
    {
        var backend = UiaTree.Button("B").AsDisabled().Build();
        var element = CreateElement(backend);

        Assert.False(await element.IsEnabledAsync());
    }

    [Fact]
    public async Task IsDisabledAsync_ReturnsTrue_WhenDisabled()
    {
        var backend = UiaTree.Button("B").AsDisabled().Build();
        var element = CreateElement(backend);

        Assert.True(await element.IsDisabledAsync());
    }

    [Fact]
    public async Task IsDisabledAsync_ReturnsFalse_WhenEnabled()
    {
        var backend = UiaTree.Button("B").Build();
        var element = CreateElement(backend);

        Assert.False(await element.IsDisabledAsync());
    }

    [Fact]
    public async Task IsCheckedAsync_ReturnsTrue_WhenToggleOn()
    {
        var backend = UiaTree.CheckBox("CB", initialState: true).Build();
        var element = CreateElement(backend);

        Assert.True(await element.IsCheckedAsync());
    }

    [Fact]
    public async Task IsCheckedAsync_ReturnsFalse_WhenToggleOff()
    {
        var backend = UiaTree.CheckBox("CB", initialState: false).Build();
        var element = CreateElement(backend);

        Assert.False(await element.IsCheckedAsync());
    }

    [Fact]
    public async Task IsCheckedAsync_ReturnsFalse_WhenTogglePatternUnsupported()
    {
        var backend = UiaTree.Button("B").Build();  // no toggle pattern
        var element = CreateElement(backend);

        Assert.False(await element.IsCheckedAsync());
    }

    // IsCheckedAsync — SelectionItemPattern fallback (RadioButton path)

    [Fact]
    public async Task IsCheckedAsync_ReturnsTrue_WhenSelectionItemSelected()
    {
        // A RadioButton that is currently selected should return true from IsCheckedAsync.
        var backend = UiaTree.RadioButton("Radio 1", initialState: true).Build();
        var element = CreateElement(backend);

        Assert.True(await element.IsCheckedAsync());
    }

    [Fact]
    public async Task IsCheckedAsync_ReturnsFalse_WhenSelectionItemNotSelected()
    {
        // A RadioButton that is NOT selected should return false.
        var backend = UiaTree.RadioButton("Radio 1", initialState: false).Build();
        var element = CreateElement(backend);

        Assert.False(await element.IsCheckedAsync());
    }

    [Fact]
    public async Task IsCheckedAsync_UsesToggleState_WhenBothPatternsPresent()
    {
        // When TogglePattern is present, it takes priority over SelectionItemPattern.
        var backend = new FakeElementBackend(
            name: "Hybrid",
            controlTypeName: "CheckBox",
            supportsToggle: true,
            initialToggleState: true,
            supportsSelection: true,
            initialSelectionState: false);  // toggle=true wins
        var element = CreateElement(backend);

        Assert.True(await element.IsCheckedAsync());
    }

    // SelectedTextAsync

    [Fact]
    public async Task SelectedTextAsync_ReturnsSelectedChildName_WhenChildIsSelected()
    {
        // A ComboBox / ListBox: the selected item's Name is the selected text.
        var backend = new FakeElementBackend(
            name: "MyCombo",
            controlTypeName: "ComboBox",
            children:
            [
                new FakeElementBackend(
                    name: "Option A",
                    controlTypeName: "ListItem",
                    supportsSelection: true,
                    initialSelectionState: false),
                new FakeElementBackend(
                    name: "Option B",
                    controlTypeName: "ListItem",
                    supportsSelection: true,
                    initialSelectionState: true),   // <-- selected
            ]);
        var element = CreateElement(backend);

        var text = await element.SelectedTextAsync();
        Assert.Equal("Option B", text);
    }

    [Fact]
    public async Task SelectedTextAsync_ReturnsNull_WhenNoChildSelected()
    {
        // No selected child and no ValuePattern — result is null.
        var backend = new FakeElementBackend(name: "MyCombo", controlTypeName: "ComboBox");
        var element = CreateElement(backend);

        var text = await element.SelectedTextAsync();
        Assert.Null(text);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TextPattern (TryGetDocumentText) fallback paths
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task InnerTextAsync_ReturnsDocumentText_WhenValuePatternNullAndTextPatternPresent()
    {
        // TryGetValue() returns null but TryGetDocumentText() returns text.
        var backend = new DocumentTextBackend("rich content");
        var element = new FlawrightElement(backend, new FakeInputBackend());

        var result = await element.InnerTextAsync();

        Assert.Equal("rich content", result);
    }

    [Fact]
    public async Task TextContentAsync_ReturnsDocumentText_WhenValuePatternNullAndTextPatternPresent()
    {
        var backend = new DocumentTextBackend("doc text");
        var element = new FlawrightElement(backend, new FakeInputBackend());

        var result = await element.TextContentAsync();

        Assert.Equal("doc text", result);
    }

    [Fact]
    public async Task InputValueAsync_ReturnsDocumentText_WhenValuePatternNullAndTextPatternPresent()
    {
        var backend = new DocumentTextBackend("document value");
        var element = new FlawrightElement(backend, new FakeInputBackend());

        var result = await element.InputValueAsync();

        Assert.Equal("document value", result);
    }

    [Fact]
    public async Task IsEditableAsync_ReturnsTrue_WhenValuePatternSupportedAndEnabled()
    {
        var backend = UiaTree.Edit("E").WithValue("x").Build();
        var element = CreateElement(backend);

        Assert.True(await element.IsEditableAsync());
    }

    [Fact]
    public async Task IsEditableAsync_ReturnsFalse_WhenValuePatternUnsupported()
    {
        var backend = UiaTree.Button("B").Build();  // no value set
        var element = CreateElement(backend);

        Assert.False(await element.IsEditableAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BoundingBoxAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BoundingBoxAsync_ReturnsPopulatedBox_WhenRectangleIsSet()
    {
        var backend = UiaTree.Button("B").WithBounds(10, 20, 100, 50).Build();
        var element = CreateElement(backend);

        var box = await element.BoundingBoxAsync();

        Assert.NotNull(box);
        Assert.Equal(10, box.X);
        Assert.Equal(20, box.Y);
        Assert.Equal(100, box.Width);
        Assert.Equal(50, box.Height);
    }

    [Fact]
    public async Task BoundingBoxAsync_ReturnsNull_WhenRectangleIsEmpty()
    {
        var backend = new FakeElementBackend(name: "B", boundingRectangle: Rectangle.Empty);
        var element = CreateElement(backend);

        var box = await element.BoundingBoxAsync();

        Assert.Null(box);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ClickAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ClickAsync_WithoutOptions_SendsMouseClick()
    {
        var backend = UiaTree.Button("B").Build();
        var (element, input) = CreateElementWithInput(backend);

        await element.ClickAsync();

        // RealInputMode routes clicks through input.MouseClick, not element.Click()
        Assert.Single(input.MouseClicks);
    }

    [Fact]
    public async Task ClickAsync_WithOptions_SendsMouseClick()
    {
        var backend = UiaTree.Button("B").Build();
        var (element, input) = CreateElementWithInput(backend);

        await element.ClickAsync(new LocatorClickOptions());

        // RealInputMode routes clicks through input.MouseClick, not element.Click()
        Assert.Single(input.MouseClicks);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DoubleClickAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DoubleClickAsync_WithoutOptions_SendsMouseClickWithCount2()
    {
        var backend = UiaTree.Button("B").Build();
        var (element, input) = CreateElementWithInput(backend);

        await element.DoubleClickAsync();

        // RealInputMode routes double-clicks through input.MouseClick with clickCount=2
        Assert.Single(input.MouseClicks);
        Assert.Equal(2, input.MouseClicks[0].ClickCount);
    }

    [Fact]
    public async Task DoubleClickAsync_WithOptions_SendsMouseClickWithCount2()
    {
        var backend = UiaTree.Button("B").Build();
        var (element, input) = CreateElementWithInput(backend);

        await element.DoubleClickAsync(new LocatorDoubleClickOptions());

        // RealInputMode routes double-clicks through input.MouseClick with clickCount=2
        Assert.Single(input.MouseClicks);
        Assert.Equal(2, input.MouseClicks[0].ClickCount);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FillAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FillAsync_CallsTrySetValue_WithProvidedText()
    {
        var backend = UiaTree.Edit("E").WithValue("old").Build();
        var element = CreateElement(backend);

        await element.FillAsync("new text");

        Assert.Equal("new text", backend.TryGetValue());
    }

    [Fact]
    public async Task FillAsync_Throws_WhenTrySetValueReturnsFalse()
    {
        // Use a backend that returns false from TrySetValue
        var backend = new NoValuePatternBackend();
        var element = new FlawrightElement(backend, new FakeInputBackend());

        await Assert.ThrowsAsync<InvalidOperationException>(() => element.FillAsync("text"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ClearAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ClearAsync_CallsTrySetValueWithEmptyString()
    {
        var backend = UiaTree.Edit("E").WithValue("some text").Build();
        var element = CreateElement(backend);

        await element.ClearAsync();

        Assert.Equal(string.Empty, backend.TryGetValue());
        Assert.Contains(string.Empty, backend.Inputs);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FocusAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FocusAsync_CallsBackendFocus()
    {
        var backend = UiaTree.Button("B").Build();
        var element = CreateElement(backend);

        await element.FocusAsync();

        Assert.Equal(1, backend.FocusCount);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HoverAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HoverAsync_MovesToElementCenter_WhenNoPositionOption()
    {
        var backend = UiaTree.Button("B").WithBounds(100, 200, 80, 40).Build();
        var (element, input) = CreateElementWithInput(backend);

        await element.HoverAsync();

        Assert.Single(input.MouseMoves);
        var move = input.MouseMoves[0];
        Assert.Equal(100 + 80 / 2, move.X);   // 140
        Assert.Equal(200 + 40 / 2, move.Y);   // 220
    }

    [Fact]
    public async Task HoverAsync_MovesToSpecifiedOffset_WhenPositionOptionSet()
    {
        var backend = UiaTree.Button("B").WithBounds(100, 200, 80, 40).Build();
        var (element, input) = CreateElementWithInput(backend);

        await element.HoverAsync(new LocatorHoverOptions { Position = new BoundingBox(10, 5, 0, 0) });

        Assert.Single(input.MouseMoves);
        var move = input.MouseMoves[0];
        Assert.Equal(100 + 10, move.X);  // rect.X + offset.X = 110
        Assert.Equal(200 + 5, move.Y);   // rect.Y + offset.Y = 205
    }

    [Fact]
    public async Task HoverAsync_WithNullOptions_DelegatesToCenter()
    {
        // Calls the options-based overload with null options — should hover to center
        var backend = UiaTree.Button("B").WithBounds(10, 20, 40, 20).Build();
        var (element, input) = CreateElementWithInput(backend);

        await element.HoverAsync(options: null);

        Assert.Single(input.MouseMoves);
        Assert.Equal(10 + 40 / 2, input.MouseMoves[0].X);
        Assert.Equal(20 + 20 / 2, input.MouseMoves[0].Y);
    }

    [Fact]
    public async Task HoverAsync_UsesStepsZero()
    {
        var backend = UiaTree.Button("B").WithBounds(0, 0, 100, 100).Build();
        var (element, input) = CreateElementWithInput(backend);

        await element.HoverAsync();

        Assert.Equal(0, input.MouseMoves[0].Steps);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ScrollIntoViewIfNeededAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ScrollIntoViewIfNeededAsync_CallsBackendTryScrollIntoView()
    {
        var backend = UiaTree.Button("B").Build();
        var element = CreateElement(backend);

        await element.ScrollIntoViewIfNeededAsync();

        Assert.True(backend.ScrolledIntoView);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CheckAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CheckAsync_NoOp_WhenAlreadyChecked()
    {
        var backend = UiaTree.CheckBox("CB", initialState: true).Build();
        var element = CreateElement(backend);

        // Should not throw and toggle count should not change
        await element.CheckAsync();

        Assert.True(backend.GetToggleState());  // still on
    }

    [Fact]
    public async Task CheckAsync_CallsTryToggleOn_WhenUnchecked()
    {
        var backend = UiaTree.CheckBox("CB", initialState: false).Build();
        var element = CreateElement(backend);

        await element.CheckAsync();

        Assert.True(backend.GetToggleState());
    }

    [Fact]
    public async Task CheckAsync_Throws_WhenTogglePatternUnsupported()
    {
        var backend = UiaTree.Button("B").Build();  // no toggle
        var element = CreateElement(backend);

        await Assert.ThrowsAsync<InvalidOperationException>(() => element.CheckAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UncheckAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UncheckAsync_NoOp_WhenAlreadyUnchecked()
    {
        var backend = UiaTree.CheckBox("CB", initialState: false).Build();
        var element = CreateElement(backend);

        await element.UncheckAsync();

        Assert.False(backend.GetToggleState());
    }

    [Fact]
    public async Task UncheckAsync_CallsTryToggleOff_WhenChecked()
    {
        var backend = UiaTree.CheckBox("CB", initialState: true).Build();
        var element = CreateElement(backend);

        await element.UncheckAsync();

        Assert.False(backend.GetToggleState());
    }

    [Fact]
    public async Task UncheckAsync_Throws_WhenTogglePatternUnsupported()
    {
        var backend = UiaTree.Button("B").Build();  // no toggle
        var element = CreateElement(backend);

        await Assert.ThrowsAsync<InvalidOperationException>(() => element.UncheckAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CheckAsync — RadioButton (SelectionItemPattern fallback)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CheckAsync_UsesSelectionItemPattern_WhenTogglePatternUnsupported()
    {
        // Simulate a RadioButton: no TogglePattern, but TrySelect returns true.
        var backend = new FakeElementBackend(name: "Radio 1", controlTypeName: "RadioButton");
        backend.TrySelectResult = true;
        var element = new FlawrightElement(backend, new FakeInputBackend());

        await element.CheckAsync();  // must not throw

        Assert.True(backend.WasSelected);
    }

    [Fact]
    public async Task CheckAsync_TogglePatternTakesPrecedenceOverSelectionItem()
    {
        // When TogglePattern is supported it must be used, not SelectionItemPattern.
        var backend = UiaTree.CheckBox("CB", initialState: false).Build();
        backend.TrySelectResult = true;  // also has selection pattern — toggle wins
        var element = CreateElement(backend);

        await element.CheckAsync();

        Assert.True(backend.GetToggleState());   // toggled on
        Assert.False(backend.WasSelected);       // selection path NOT taken
    }

    [Fact]
    public async Task CheckAsync_Throws_WhenNeitherToggleNorSelectionPatternSupported()
    {
        // Button has no toggle and no selection — should throw with both patterns mentioned.
        var backend = UiaTree.Button("B").Build();
        var element = CreateElement(backend);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => element.CheckAsync());
        Assert.Contains("TogglePattern", ex.Message);
        Assert.Contains("SelectionItemPattern", ex.Message);
    }

    [Fact]
    public async Task UncheckAsync_ThrowsSpecificMessage_WhenOnlySelectionPatternSupported()
    {
        // RadioButton: TryToggleOff returns false, and TrySelect is irrelevant for uncheck.
        var backend = new FakeElementBackend(name: "Radio 1", controlTypeName: "RadioButton");
        backend.TrySelectResult = true;  // selection supported, but no toggle
        var element = new FlawrightElement(backend, new FakeInputBackend());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => element.UncheckAsync());
        Assert.Contains("RadioButton", ex.Message);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SetCheckedAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SetCheckedAsync_True_Checks_WhenUnchecked()
    {
        var backend = UiaTree.CheckBox("CB", initialState: false).Build();
        var element = CreateElement(backend);

        await element.SetCheckedAsync(true);

        Assert.True(backend.GetToggleState());
    }

    [Fact]
    public async Task SetCheckedAsync_False_Unchecks_WhenChecked()
    {
        var backend = UiaTree.CheckBox("CB", initialState: true).Build();
        var element = CreateElement(backend);

        await element.SetCheckedAsync(false);

        Assert.False(backend.GetToggleState());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SelectOptionAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SelectOptionAsync_CallsTrySelectItem_WithProvidedValue()
    {
        var backend = UiaTree.List("Combo")
            .WithChild(UiaTree.ListItem("Option A"))
            .WithChild(UiaTree.ListItem("Option B"))
            .Build();
        var element = CreateElement(backend);

        await element.SelectOptionAsync("Option A");

        Assert.Equal("Option A", backend.LastSelectedItem);
    }

    [Fact]
    public async Task SelectOptionAsync_Throws_WhenItemNotFound()
    {
        // Backend with no children — TrySelectItem returns false
        var backend = UiaTree.List("Empty").Build();
        var element = CreateElement(backend);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => element.SelectOptionAsync("Missing Item"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Legacy surface delegates
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TextAsync_Legacy_DelegatesToInnerTextAsync()
    {
        var backend = UiaTree.Button("Hello").Build();
        // Access via interface since TextAsync is explicit interface implementation
        IFlawrightElement element = CreateElement(backend);

        var result = await element.TextAsync();

        Assert.Equal("Hello", result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static FlawrightElement CreateElement(FakeElementBackend backend)
        => new(backend, new FakeInputBackend());

    private static (FlawrightElement element, FakeInputBackend input) CreateElementWithInput(
        FakeElementBackend backend)
    {
        var input = new FakeInputBackend();
        return (new FlawrightElement(backend, input), input);
    }

    // ── Helpers — custom backend for failure cases ────────────────────────────

    /// <summary>
    /// A minimal <see cref="IElementBackend"/> stub where <c>TryGetValue()</c>
    /// returns <see langword="null"/> but <c>TryGetDocumentText()</c> returns the
    /// provided text.  Used to exercise the TextPattern fallback paths in
    /// <c>InnerTextAsync</c>, <c>TextContentAsync</c>, and <c>InputValueAsync</c>.
    /// </summary>
    private sealed class DocumentTextBackend : IElementBackend
    {
        private readonly string _documentText;
        public DocumentTextBackend(string documentText) => _documentText = documentText;
        public string? AutomationId => null;
        public string? Name => "DocElement";
        public string? ClassName => null;
        public string ControlTypeName => "Edit";
        public bool IsEnabled => true;
        public bool IsOffscreen => false;
        public System.Drawing.Rectangle BoundingRectangle => System.Drawing.Rectangle.Empty;
        public void Click() { }
        public void DoubleClick() { }
        public void Focus() { }
        public bool TrySetValue(string text) => true;
        public string? TryGetValue() => null;
        public string? TryGetDocumentText() => _documentText;
        public bool TrySelect() => false;
        public bool TryToggleOn() => false;
        public bool TryToggleOff() => false;
        public bool? GetToggleState() => null;
        public bool? GetSelectionState() => null;
        public string? GetSelectedText() => null;
        public bool TryScrollIntoView() => false;
        public bool TryExpand() => false;
        public bool TrySelectItem(string nameOrId) => false;
        public bool TryInvoke() => false;
        public bool? GetExpandCollapseState() => null;
        public string? FrameworkId => null;
        public bool HasKeyboardFocus => false;
        public nint NativeWindowHandle => IntPtr.Zero;
        public IReadOnlyList<IElementBackend> GetModalWindows() => Array.Empty<IElementBackend>();
        public System.Collections.Generic.IEnumerable<IElementBackend> FindAll(IElementCondition condition)
            => System.Linq.Enumerable.Empty<IElementBackend>();
        public IElementBackend? FindFirst(IElementCondition condition) => null;
        public byte[] CaptureScreenshot() => Array.Empty<byte>();
    }

    /// <summary>
    /// A minimal <see cref="IElementBackend"/> stub that always returns
    /// <see langword="false"/> from <see cref="TrySetValue"/> and
    /// <see langword="null"/> from value/document-text queries so that
    /// <c>FillAsync</c> and <c>InputValueAsync</c> throw tests can exercise
    /// the failure path.
    /// </summary>
    private sealed class NoValuePatternBackend : IElementBackend
    {
        public string? AutomationId => null;
        public string? Name => "NoValue";
        public string? ClassName => null;
        public string ControlTypeName => "Button";
        public bool IsEnabled => true;
        public bool IsOffscreen => false;
        public System.Drawing.Rectangle BoundingRectangle => System.Drawing.Rectangle.Empty;
        public void Click() { }
        public void DoubleClick() { }
        public void Focus() { }
        public bool TrySetValue(string text) => false;
        public string? TryGetValue() => null;
        public string? TryGetDocumentText() => null;
        public bool TrySelect() => false;
        public bool TryToggleOn() => false;
        public bool TryToggleOff() => false;
        public bool? GetToggleState() => null;
        public bool? GetSelectionState() => null;
        public string? GetSelectedText() => null;
        public bool TryScrollIntoView() => false;
        public bool TryExpand() => false;
        public bool TrySelectItem(string nameOrId) => false;
        public bool TryInvoke() => false;
        public bool? GetExpandCollapseState() => null;
        public string? FrameworkId => null;
        public bool HasKeyboardFocus => false;
        public nint NativeWindowHandle => IntPtr.Zero;
        public IReadOnlyList<IElementBackend> GetModalWindows() => Array.Empty<IElementBackend>();
        public System.Collections.Generic.IEnumerable<IElementBackend> FindAll(IElementCondition condition)
            => System.Linq.Enumerable.Empty<IElementBackend>();
        public IElementBackend? FindFirst(IElementCondition condition) => null;
        public byte[] CaptureScreenshot() => Array.Empty<byte>();
    }
}
