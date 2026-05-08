using FlaUI.Core.WindowsAPI;

namespace JerrettDavis.Flawright.Backends;

/// <summary>
/// Low-level input backend seam for mouse and keyboard operations.
///
/// The sole production implementation is <c>FlaUiInputBackend</c> which
/// delegates to <c>FlaUI.Core.Input.Mouse</c> and
/// <c>FlaUI.Core.Input.Keyboard</c>.  Tests use <c>FakeInputBackend</c>.
///
/// This interface backs the <c>IFlawrightMouse</c> and
/// <c>IFlawrightKeyboard</c> public sub-APIs defined in §2.6 of the blueprint.
/// </summary>
public interface IInputBackend
{
    // ── Mouse ─────────────────────────────────────────────────────────────────

    /// <summary>Performs a mouse click at the specified screen coordinates.</summary>
    /// <param name="x">Screen X coordinate.</param>
    /// <param name="y">Screen Y coordinate.</param>
    /// <param name="button">Which mouse button to click.</param>
    /// <param name="clickCount">Number of clicks (1 = single, 2 = double).</param>
    void MouseClick(int x, int y, MouseButton button, int clickCount);

    /// <summary>Moves the mouse to the specified screen coordinates.</summary>
    /// <param name="x">Screen X coordinate.</param>
    /// <param name="y">Screen Y coordinate.</param>
    /// <param name="steps">Number of intermediate positions (0 = jump).</param>
    void MouseMove(int x, int y, int steps);

    /// <summary>Simulates the mouse scroll wheel.</summary>
    /// <param name="dx">Horizontal scroll delta.</param>
    /// <param name="dy">Vertical scroll delta.</param>
    void MouseWheel(int dx, int dy);

    /// <summary>Presses (holds down) a mouse button.</summary>
    /// <param name="button">The button to press.</param>
    void MouseDown(MouseButton button);

    /// <summary>Releases a previously pressed mouse button.</summary>
    /// <param name="button">The button to release.</param>
    void MouseUp(MouseButton button);

    // ── Keyboard ──────────────────────────────────────────────────────────────

    /// <summary>Presses and holds a key.</summary>
    /// <param name="key">The virtual key to press.</param>
    void KeyboardPress(VirtualKeyShort key);

    /// <summary>Releases a previously pressed key.</summary>
    /// <param name="key">The virtual key to release.</param>
    void KeyboardRelease(VirtualKeyShort key);

    /// <summary>Types a sequence of characters.</summary>
    /// <param name="text">The text to type.</param>
    void KeyboardType(string text);

    /// <summary>Presses and immediately releases a key.</summary>
    /// <param name="key">The virtual key to press.</param>
    void KeyboardTap(VirtualKeyShort key);
}

/// <summary>Mouse button enum matching Playwright's convention.</summary>
public enum MouseButton
{
    /// <summary>Primary (left) mouse button.</summary>
    Left = 0,

    /// <summary>Secondary (right) mouse button.</summary>
    Right = 1,

    /// <summary>Middle mouse button.</summary>
    Middle = 2
}
