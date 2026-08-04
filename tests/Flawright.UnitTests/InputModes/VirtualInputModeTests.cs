using Flawright.Backends;
using Flawright.InputModes;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.InputModes;

/// <summary>
/// Unit tests for <see cref="VirtualInputMode"/>.
///
/// Verifies that UIA-backed actions succeed, unsupported actions throw
/// <see cref="NotSupportedException"/> with actionable messages, and that
/// no real input backend calls are made for virtualised actions.
/// </summary>
public sealed class VirtualInputModeTests
{
    private static FakeElementBackend MakeButton()
        => new(name: "OK", controlTypeName: "Button");

    private static FakeElementBackend MakeEdit()
        => new(name: "Name", controlTypeName: "Edit", initialValue: "");

    // ── Click — success ───────────────────────────────────────────────────────

    [Fact]
    public void Click_WhenTryInvokeSucceeds_DoesNotThrow()
    {
        var element = MakeButton();
        element.TryInvokeResult = true;
        var mode = new VirtualInputMode();

        mode.Click(element, new FakeInputBackend());

        Assert.Equal(1, element.InvokeCount);
    }

    [Fact]
    public void Click_WhenTryInvokeSucceeds_DoesNotCallInputBackend()
    {
        var element = MakeButton();
        element.TryInvokeResult = true;
        var input = new FakeInputBackend();
        var mode = new VirtualInputMode();

        mode.Click(element, input);

        Assert.Empty(input.MouseClicks);
    }

    // ── Click — failure ───────────────────────────────────────────────────────

    [Fact]
    public void Click_WhenTryInvokeFails_ThrowsNotSupportedException()
    {
        var element = MakeButton();
        element.TryInvokeResult = false;
        var mode = new VirtualInputMode();

        var ex = Assert.Throws<NotSupportedException>(
            () => mode.Click(element, new FakeInputBackend()));

        Assert.Contains("RealInputMode", ex.Message);
    }

    [Fact]
    public void Click_WhenTryInvokeFails_MessageMentionsInvokePattern()
    {
        var element = MakeButton();
        element.TryInvokeResult = false;
        var mode = new VirtualInputMode();

        var ex = Assert.Throws<NotSupportedException>(
            () => mode.Click(element, new FakeInputBackend()));

        Assert.Contains("InvokePattern", ex.Message);
    }

    // ── DoubleClick — always throws ────────────────────────────────────────────

    [Fact]
    public void DoubleClick_AlwaysThrowsNotSupportedException()
    {
        var mode = new VirtualInputMode();

        Assert.Throws<NotSupportedException>(
            () => mode.DoubleClick(MakeButton(), new FakeInputBackend()));
    }

    [Fact]
    public void DoubleClick_ThrowMessage_MentionsRealInputMode()
    {
        var mode = new VirtualInputMode();

        var ex = Assert.Throws<NotSupportedException>(
            () => mode.DoubleClick(MakeButton(), new FakeInputBackend()));

        Assert.Contains("RealInputMode", ex.Message);
    }

    // ── Hover — always throws ─────────────────────────────────────────────────

    [Fact]
    public void Hover_AlwaysThrowsNotSupportedException()
    {
        var mode = new VirtualInputMode();

        Assert.Throws<NotSupportedException>(
            () => mode.Hover(MakeButton(), new FakeInputBackend()));
    }

    [Fact]
    public void Hover_ThrowMessage_MentionsCursorMovement()
    {
        var mode = new VirtualInputMode();

        var ex = Assert.Throws<NotSupportedException>(
            () => mode.Hover(MakeButton(), new FakeInputBackend()));

        Assert.Contains("cursor movement", ex.Message);
    }

    // ── DragTo — always throws ────────────────────────────────────────────────

    [Fact]
    public void DragTo_AlwaysThrowsNotSupportedException()
    {
        var mode = new VirtualInputMode();

        Assert.Throws<NotSupportedException>(
            () => mode.DragTo(MakeButton(), MakeButton(), new FakeInputBackend()));
    }

    [Fact]
    public void DragTo_ThrowMessage_MentionsRealInputMode()
    {
        var mode = new VirtualInputMode();

        var ex = Assert.Throws<NotSupportedException>(
            () => mode.DragTo(MakeButton(), MakeButton(), new FakeInputBackend()));

        Assert.Contains("RealInputMode", ex.Message);
    }

    // ── Type — success (ValuePattern soft-degrade) ─────────────────────────────

    [Fact]
    public void Type_WhenTrySetValueSucceeds_SetValueOnElement()
    {
        var element = MakeEdit();  // FakeElementBackend.TrySetValue always returns true
        var mode = new VirtualInputMode();

        mode.Type(element, "hello world", new FakeInputBackend());

        Assert.Contains("hello world", element.Inputs, StringComparer.Ordinal);
    }

    [Fact]
    public void Type_WhenTrySetValueSucceeds_DoesNotCallKeyboardType()
    {
        var element = MakeEdit();
        var input = new FakeInputBackend();
        var mode = new VirtualInputMode();

        mode.Type(element, "hello", input);

        Assert.Empty(input.TypedTexts);
    }

    // ── Type — failure ────────────────────────────────────────────────────────

    [Fact]
    public void Type_WhenTrySetValueFails_ThrowsNotSupportedException()
    {
        var element = new NoValueFakeElementBackend();
        var mode = new VirtualInputMode();

        var ex = Assert.Throws<NotSupportedException>(
            () => mode.Type(element, "text", new FakeInputBackend()));

        Assert.Contains("RealInputMode", ex.Message);
    }

    // ── Press — always throws ─────────────────────────────────────────────────

    [Fact]
    public void Press_AlwaysThrowsNotSupportedException()
    {
        var mode = new VirtualInputMode();

        Assert.Throws<NotSupportedException>(
            () => mode.Press(MakeButton(), "Enter", new FakeInputBackend()));
    }

    [Fact]
    public void Press_ThrowMessage_MentionsKeyChordsAndRealInputMode()
    {
        var mode = new VirtualInputMode();

        var ex = Assert.Throws<NotSupportedException>(
            () => mode.Press(MakeButton(), "Ctrl+S", new FakeInputBackend()));

        Assert.Contains("RealInputMode", ex.Message);
        Assert.Contains("key chords", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Minimal <see cref="IElementBackend"/> stub where <c>TrySetValue</c> always
    /// returns <see langword="false"/> so <see cref="VirtualInputMode.Type"/>
    /// exercises the failure path.
    /// </summary>
    private sealed class NoValueFakeElementBackend : IElementBackend
    {
        public string? AutomationId => null;
        public string? Name => "NoValue";
        public string? ClassName => null;
        public string ControlTypeName => "Pane";
        public bool IsEnabled => true;
        public bool IsOffscreen => false;
        public System.Drawing.Rectangle BoundingRectangle => System.Drawing.Rectangle.Empty;
        public void Click() { }
        public void DoubleClick() { }
        public void Focus() { }
        public bool TryInvoke() => false;
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
        public bool? GetExpandCollapseState() => null;
        public bool TrySetRangeValue(double value) => false;
        public double? TryGetRangeValue() => null;
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
