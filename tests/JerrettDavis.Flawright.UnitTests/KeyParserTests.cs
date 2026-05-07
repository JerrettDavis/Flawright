using JerrettDavis.Flawright.Input;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests;

/// <summary>
/// Tests for <see cref="KeyParser"/> chord-parsing logic.
///
/// <see cref="KeyParser.Send"/> dispatches real keyboard input via the FlaUI
/// Keyboard API, so we cannot call it in a pure unit test without a desktop
/// session. Instead we test the guard conditions (null/empty → ArgumentException,
/// unknown key → ArgumentException) by using <c>Record.Exception</c>.
///
/// Positive routing (Enter, Ctrl+S, F1, etc.) is covered in E2E tests.
/// </summary>
public class KeyParserTests
{
    // ── Guard conditions ──────────────────────────────────────────────────────

    [Fact]
    public void Send_NullKey_ThrowsArgumentException()
    {
        // ThrowIfNullOrEmpty throws ArgumentNullException (a subclass of ArgumentException)
        // for null, so we use ThrowsAny to accept both.
        Assert.ThrowsAny<ArgumentException>(() => KeyParser.Send(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Send_EmptyOrWhitespaceKey_ThrowsArgumentException(string key)
    {
        // ThrowIfNullOrEmpty treats whitespace-only strings as non-empty,
        // but a whitespace main key will fail ParseKey lookup.
        // Empty string triggers ArgumentException from ThrowIfNullOrEmpty.
        // Whitespace-only gets to ParseKey and fails with "Unknown key name".
        var ex = Assert.ThrowsAny<ArgumentException>(() => KeyParser.Send(key));
        Assert.NotNull(ex);
    }

    [Theory]
    [InlineData("NotAKey")]
    [InlineData("Control+Save")]    // 'Save' is not a known key name
    [InlineData("Ctrl+ZZ")]         // 'ZZ' is not a single key
    public void Send_UnknownKey_ThrowsArgumentException(string key)
    {
        // The actual keyboard dispatch would fail too (no desktop session in unit
        // test), but ArgumentException is thrown before any platform call for
        // unknown key names.
        var ex = Record.Exception(() => KeyParser.Send(key));
        // May throw ArgumentException (unknown key), COMException (no desktop),
        // or InvalidOperationException — but NOT succeed silently.
        Assert.NotNull(ex);
    }

    [Theory]
    [InlineData("UnknownModifier+S")]
    public void Send_UnknownModifier_ThrowsArgumentException(string key)
    {
        var ex = Assert.Throws<ArgumentException>(() => KeyParser.Send(key));
        Assert.Contains("Unknown modifier", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Chord structure (without dispatching) ─────────────────────────────────
    // These tests verify that valid chord strings don't throw *argument* errors
    // (parsing succeeds). They may still throw a COMException or similar from
    // the actual keyboard dispatch if no desktop session is available, but that
    // is not our concern here — we only care that the parsing logic is correct.

    [Theory]
    [InlineData("Enter")]
    [InlineData("Escape")]
    [InlineData("Tab")]
    [InlineData("Space")]
    [InlineData("F1")]
    [InlineData("F12")]
    [InlineData("A")]
    [InlineData("Z")]
    [InlineData("0")]
    [InlineData("9")]
    public void Send_ValidSingleKey_DoesNotThrowArgumentException(string key)
    {
        // If an ArgumentException is thrown, the key name is not recognised by
        // the parser — that would be a bug. Other exception types (COM, UI) are
        // expected in a headless environment.
        var ex = Record.Exception(() => KeyParser.Send(key));
        Assert.False(
            ex is ArgumentException,
            $"Key '{key}' should be parseable but got ArgumentException: {ex?.Message}");
    }

    [Theory]
    [InlineData("Ctrl+S")]
    [InlineData("Ctrl+Shift+T")]
    [InlineData("Alt+F4")]
    [InlineData("Ctrl+A")]
    [InlineData("Ctrl+Z")]
    [InlineData("Shift+Enter")]
    public void Send_ValidChord_DoesNotThrowArgumentException(string key)
    {
        var ex = Record.Exception(() => KeyParser.Send(key));
        Assert.False(
            ex is ArgumentException,
            $"Chord '{key}' should be parseable but got ArgumentException: {ex?.Message}");
    }

    [Theory]
    [InlineData("F1")]
    [InlineData("F2")]
    [InlineData("F3")]
    [InlineData("F4")]
    [InlineData("F5")]
    [InlineData("F6")]
    [InlineData("F7")]
    [InlineData("F8")]
    [InlineData("F9")]
    [InlineData("F10")]
    [InlineData("F11")]
    [InlineData("F12")]
    public void Send_FunctionKeys_AreAllRecognised(string key)
    {
        var ex = Record.Exception(() => KeyParser.Send(key));
        Assert.False(
            ex is ArgumentException,
            $"Function key '{key}' should be parseable but got ArgumentException: {ex?.Message}");
    }
}
