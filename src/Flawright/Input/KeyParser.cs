using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace Flawright.Input;

/// <summary>
/// Parses a Playwright-style key or chord string and dispatches it via the
/// FlaUI keyboard API.
/// </summary>
/// <remarks>
/// <para>Supported key syntax:</para>
/// <list type="bullet">
///   <item><description>Single key name: <c>"Enter"</c>, <c>"Escape"</c>, <c>"Tab"</c>, <c>"Space"</c>, etc.</description></item>
///   <item><description>Chord with modifiers: <c>"Ctrl+S"</c>, <c>"Ctrl+Shift+Z"</c>, <c>"Alt+F4"</c>.</description></item>
///   <item><description>Single character: <c>"a"</c>, <c>"A"</c>.</description></item>
/// </list>
/// </remarks>
internal static class KeyParser
{
    /// <summary>
    /// Parses and sends <paramref name="key"/> using FlaUI's
    /// <see cref="Keyboard"/> API.
    /// </summary>
    /// <param name="key">Key or chord string (e.g. "Enter", "Ctrl+S").</param>
    /// <exception cref="ArgumentException">
    /// Thrown when a modifier or key name cannot be resolved.
    /// </exception>
    internal static void Send(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        var parts = key.Split('+');
        var modifiers = parts[..^1];
        var mainKey = parts[^1].Trim();

        var modVks = modifiers
            .Select(m => ParseModifier(m.Trim()))
            .ToArray();

        var mainVk = ParseKey(mainKey);

        if (modVks.Length > 0)
        {
            // Hold all modifiers down, press the main key, release modifiers
            foreach (var mod in modVks)
                Keyboard.Press(mod);
            try
            {
                Keyboard.Type(mainVk);
            }
            finally
            {
                foreach (var mod in modVks.Reverse())
                    Keyboard.Release(mod);
            }
        }
        else
        {
            Keyboard.Type(mainVk);
        }
    }

    // ── Internal helpers (also used by tests) ────────────────────────────────

    /// <summary>
    /// Resolves a modifier name ("Ctrl", "Alt", "Shift", "Win") to its
    /// <see cref="VirtualKeyShort"/> value.  Exposed as <c>internal</c> so that
    /// unit tests can verify the parsing logic without dispatching actual input.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is not a recognised modifier.
    /// </exception>
    internal static VirtualKeyShort ParseModifier(string name)
    {
        return name.ToUpperInvariant() switch
        {
            "CTRL" or "CONTROL" => VirtualKeyShort.CONTROL,
            "ALT" => VirtualKeyShort.ALT,
            "SHIFT" => VirtualKeyShort.SHIFT,
            "WIN" or "META" => VirtualKeyShort.LWIN,
            _ => throw new ArgumentException(
                $"Unknown modifier key: '{name}'",
                nameof(name))
        };
    }

    /// <summary>
    /// Resolves a key name ("Enter", "F1", "A", …) to its
    /// <see cref="VirtualKeyShort"/> value.  Exposed as <c>internal</c> so that
    /// unit tests can verify the parsing logic without dispatching actual input.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is not a recognised key.
    /// </exception>
    internal static VirtualKeyShort ParseKey(string name)
    {
        return name.ToUpperInvariant() switch
        {
            "ENTER" or "RETURN" => VirtualKeyShort.ENTER,
            "ESCAPE" or "ESC" => VirtualKeyShort.ESCAPE,
            "TAB" => VirtualKeyShort.TAB,
            "SPACE" => VirtualKeyShort.SPACE,
            "BACKSPACE" or "BACK" => VirtualKeyShort.BACK,
            "DELETE" or "DEL" => VirtualKeyShort.DELETE,
            "INSERT" => VirtualKeyShort.INSERT,
            "HOME" => VirtualKeyShort.HOME,
            "END" => VirtualKeyShort.END,
            "PAGEUP" => VirtualKeyShort.PRIOR,
            "PAGEDOWN" => VirtualKeyShort.NEXT,
            "UP" => VirtualKeyShort.UP,
            "DOWN" => VirtualKeyShort.DOWN,
            "LEFT" => VirtualKeyShort.LEFT,
            "RIGHT" => VirtualKeyShort.RIGHT,
            "F1" => VirtualKeyShort.F1,
            "F2" => VirtualKeyShort.F2,
            "F3" => VirtualKeyShort.F3,
            "F4" => VirtualKeyShort.F4,
            "F5" => VirtualKeyShort.F5,
            "F6" => VirtualKeyShort.F6,
            "F7" => VirtualKeyShort.F7,
            "F8" => VirtualKeyShort.F8,
            "F9" => VirtualKeyShort.F9,
            "F10" => VirtualKeyShort.F10,
            "F11" => VirtualKeyShort.F11,
            "F12" => VirtualKeyShort.F12,
            "A" => VirtualKeyShort.KEY_A,
            "B" => VirtualKeyShort.KEY_B,
            "C" => VirtualKeyShort.KEY_C,
            "D" => VirtualKeyShort.KEY_D,
            "E" => VirtualKeyShort.KEY_E,
            "F" => VirtualKeyShort.KEY_F,
            "G" => VirtualKeyShort.KEY_G,
            "H" => VirtualKeyShort.KEY_H,
            "I" => VirtualKeyShort.KEY_I,
            "J" => VirtualKeyShort.KEY_J,
            "K" => VirtualKeyShort.KEY_K,
            "L" => VirtualKeyShort.KEY_L,
            "M" => VirtualKeyShort.KEY_M,
            "N" => VirtualKeyShort.KEY_N,
            "O" => VirtualKeyShort.KEY_O,
            "P" => VirtualKeyShort.KEY_P,
            "Q" => VirtualKeyShort.KEY_Q,
            "R" => VirtualKeyShort.KEY_R,
            "S" => VirtualKeyShort.KEY_S,
            "T" => VirtualKeyShort.KEY_T,
            "U" => VirtualKeyShort.KEY_U,
            "V" => VirtualKeyShort.KEY_V,
            "W" => VirtualKeyShort.KEY_W,
            "X" => VirtualKeyShort.KEY_X,
            "Y" => VirtualKeyShort.KEY_Y,
            "Z" => VirtualKeyShort.KEY_Z,
            "0" => VirtualKeyShort.KEY_0,
            "1" => VirtualKeyShort.KEY_1,
            "2" => VirtualKeyShort.KEY_2,
            "3" => VirtualKeyShort.KEY_3,
            "4" => VirtualKeyShort.KEY_4,
            "5" => VirtualKeyShort.KEY_5,
            "6" => VirtualKeyShort.KEY_6,
            "7" => VirtualKeyShort.KEY_7,
            "8" => VirtualKeyShort.KEY_8,
            "9" => VirtualKeyShort.KEY_9,
            _ => throw new ArgumentException(
                $"Unknown key name: '{name}'",
                nameof(name))
        };
    }
}
