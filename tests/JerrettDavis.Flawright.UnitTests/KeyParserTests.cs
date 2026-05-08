using FlaUI.Core.WindowsAPI;
using JerrettDavis.Flawright.Input;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests;

/// <summary>
/// Tests for <see cref="KeyParser"/> chord-parsing logic.
///
/// Tests call <see cref="KeyParser.ParseKey"/> and
/// <see cref="KeyParser.ParseModifier"/> directly rather than
/// <see cref="KeyParser.Send"/>, because <c>Send</c> dispatches real keyboard
/// input via the FlaUI Keyboard API — which requires a desktop session and
/// would cause the test host to crash on headless CI runners.
/// </summary>
public class KeyParserTests
{
    // ── Guard conditions (via Send) ───────────────────────────────────────────
    // These guard tests use only the null/empty paths which throw ArgumentException
    // *before* any platform keyboard call is made.

    [Fact]
    public void Send_NullKey_ThrowsArgumentException()
    {
        // ThrowIfNullOrEmpty throws ArgumentNullException (a subclass of ArgumentException)
        // for null, so we use ThrowsAny to accept both.
        Assert.ThrowsAny<ArgumentException>(() => KeyParser.Send(null!));
    }

    [Theory]
    [InlineData("")]
    public void Send_EmptyKey_ThrowsArgumentException(string key)
    {
        // Empty string triggers ArgumentException from ThrowIfNullOrEmpty.
        var ex = Assert.ThrowsAny<ArgumentException>(() => KeyParser.Send(key));
        Assert.NotNull(ex);
    }

    // ── ParseKey — key name resolution (no keyboard dispatch) ────────────────

    [Theory]
    [InlineData("Enter", VirtualKeyShort.ENTER)]
    [InlineData("Return", VirtualKeyShort.ENTER)]
    [InlineData("Escape", VirtualKeyShort.ESCAPE)]
    [InlineData("Esc", VirtualKeyShort.ESCAPE)]
    [InlineData("Tab", VirtualKeyShort.TAB)]
    [InlineData("Space", VirtualKeyShort.SPACE)]
    [InlineData("Backspace", VirtualKeyShort.BACK)]
    [InlineData("Back", VirtualKeyShort.BACK)]
    [InlineData("Delete", VirtualKeyShort.DELETE)]
    [InlineData("Del", VirtualKeyShort.DELETE)]
    [InlineData("Insert", VirtualKeyShort.INSERT)]
    [InlineData("Home", VirtualKeyShort.HOME)]
    [InlineData("End", VirtualKeyShort.END)]
    [InlineData("PageUp", VirtualKeyShort.PRIOR)]
    [InlineData("PageDown", VirtualKeyShort.NEXT)]
    [InlineData("Up", VirtualKeyShort.UP)]
    [InlineData("Down", VirtualKeyShort.DOWN)]
    [InlineData("Left", VirtualKeyShort.LEFT)]
    [InlineData("Right", VirtualKeyShort.RIGHT)]
    [InlineData("F1", VirtualKeyShort.F1)]
    [InlineData("F12", VirtualKeyShort.F12)]
    [InlineData("A", VirtualKeyShort.KEY_A)]
    [InlineData("B", VirtualKeyShort.KEY_B)]
    [InlineData("C", VirtualKeyShort.KEY_C)]
    [InlineData("D", VirtualKeyShort.KEY_D)]
    [InlineData("E", VirtualKeyShort.KEY_E)]
    [InlineData("F", VirtualKeyShort.KEY_F)]
    [InlineData("G", VirtualKeyShort.KEY_G)]
    [InlineData("H", VirtualKeyShort.KEY_H)]
    [InlineData("I", VirtualKeyShort.KEY_I)]
    [InlineData("J", VirtualKeyShort.KEY_J)]
    [InlineData("K", VirtualKeyShort.KEY_K)]
    [InlineData("L", VirtualKeyShort.KEY_L)]
    [InlineData("M", VirtualKeyShort.KEY_M)]
    [InlineData("N", VirtualKeyShort.KEY_N)]
    [InlineData("O", VirtualKeyShort.KEY_O)]
    [InlineData("P", VirtualKeyShort.KEY_P)]
    [InlineData("Q", VirtualKeyShort.KEY_Q)]
    [InlineData("R", VirtualKeyShort.KEY_R)]
    [InlineData("S", VirtualKeyShort.KEY_S)]
    [InlineData("T", VirtualKeyShort.KEY_T)]
    [InlineData("U", VirtualKeyShort.KEY_U)]
    [InlineData("V", VirtualKeyShort.KEY_V)]
    [InlineData("W", VirtualKeyShort.KEY_W)]
    [InlineData("X", VirtualKeyShort.KEY_X)]
    [InlineData("Y", VirtualKeyShort.KEY_Y)]
    [InlineData("Z", VirtualKeyShort.KEY_Z)]
    [InlineData("0", VirtualKeyShort.KEY_0)]
    [InlineData("1", VirtualKeyShort.KEY_1)]
    [InlineData("2", VirtualKeyShort.KEY_2)]
    [InlineData("3", VirtualKeyShort.KEY_3)]
    [InlineData("4", VirtualKeyShort.KEY_4)]
    [InlineData("5", VirtualKeyShort.KEY_5)]
    [InlineData("6", VirtualKeyShort.KEY_6)]
    [InlineData("7", VirtualKeyShort.KEY_7)]
    [InlineData("8", VirtualKeyShort.KEY_8)]
    [InlineData("9", VirtualKeyShort.KEY_9)]
    public void ParseKey_ValidSingleKey_ReturnsExpectedVirtualKey(string key, VirtualKeyShort expected)
    {
        var result = KeyParser.ParseKey(key);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("F1", VirtualKeyShort.F1)]
    [InlineData("F2", VirtualKeyShort.F2)]
    [InlineData("F3", VirtualKeyShort.F3)]
    [InlineData("F4", VirtualKeyShort.F4)]
    [InlineData("F5", VirtualKeyShort.F5)]
    [InlineData("F6", VirtualKeyShort.F6)]
    [InlineData("F7", VirtualKeyShort.F7)]
    [InlineData("F8", VirtualKeyShort.F8)]
    [InlineData("F9", VirtualKeyShort.F9)]
    [InlineData("F10", VirtualKeyShort.F10)]
    [InlineData("F11", VirtualKeyShort.F11)]
    [InlineData("F12", VirtualKeyShort.F12)]
    public void ParseKey_FunctionKeys_AreAllRecognised(string key, VirtualKeyShort expected)
    {
        var result = KeyParser.ParseKey(key);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("NotAKey")]
    [InlineData("Save")]
    [InlineData("ZZ")]
    public void ParseKey_UnknownKey_ThrowsArgumentException(string key)
    {
        var ex = Assert.Throws<ArgumentException>(() => KeyParser.ParseKey(key));
        Assert.Contains("Unknown key name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── ParseModifier — modifier name resolution ──────────────────────────────

    [Theory]
    [InlineData("Ctrl", VirtualKeyShort.CONTROL)]
    [InlineData("Control", VirtualKeyShort.CONTROL)]
    [InlineData("Alt", VirtualKeyShort.ALT)]
    [InlineData("Shift", VirtualKeyShort.SHIFT)]
    [InlineData("Win", VirtualKeyShort.LWIN)]
    [InlineData("Meta", VirtualKeyShort.LWIN)]
    [InlineData("CTRL", VirtualKeyShort.CONTROL)]
    [InlineData("ALT", VirtualKeyShort.ALT)]
    public void ParseModifier_ValidModifier_ReturnsExpectedVirtualKey(string modifier, VirtualKeyShort expected)
    {
        var result = KeyParser.ParseModifier(modifier);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("UnknownModifier")]
    [InlineData("Super")]
    [InlineData("Command")]
    public void ParseModifier_UnknownModifier_ThrowsArgumentException(string modifier)
    {
        var ex = Assert.Throws<ArgumentException>(() => KeyParser.ParseModifier(modifier));
        Assert.Contains("Unknown modifier", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Send — unknown key/modifier routing (throws before any platform call) ─

    [Theory]
    [InlineData("Control+Save")]    // 'Save' is not a known key name
    [InlineData("Ctrl+ZZ")]         // 'ZZ' is not a single key
    public void Send_UnknownMainKey_ThrowsArgumentException(string key)
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() => KeyParser.Send(key));
        Assert.NotNull(ex);
    }

    [Theory]
    [InlineData("UnknownModifier+S")]
    public void Send_UnknownModifier_ThrowsArgumentException(string key)
    {
        var ex = Assert.Throws<ArgumentException>(() => KeyParser.Send(key));
        Assert.Contains("Unknown modifier", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
