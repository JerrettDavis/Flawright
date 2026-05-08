using System.Diagnostics.CodeAnalysis;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUiMouseButton = FlaUI.Core.Input.MouseButton;

namespace Flawright.Backends.Uia;

/// <summary>
/// FlaUI-backed <see cref="IInputBackend"/> that delegates to
/// <see cref="FlaUI.Core.Input.Mouse"/> and
/// <see cref="FlaUI.Core.Input.Keyboard"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "FlaUI I/O; covered by E2E tests only.")]
internal sealed class FlaUiInputBackend : IInputBackend
{
    private static FlaUiMouseButton ToFlaUiButton(MouseButton button) => button switch
    {
        MouseButton.Right => FlaUiMouseButton.Right,
        MouseButton.Middle => FlaUiMouseButton.Middle,
        _ => FlaUiMouseButton.Left
    };

    /// <inheritdoc/>
    public void MouseClick(int x, int y, MouseButton button, int clickCount)
    {
        Mouse.MoveTo(x, y);
        var flaButton = ToFlaUiButton(button);

        for (var i = 0; i < clickCount; i++)
            Mouse.Click(flaButton);
    }

    /// <inheritdoc/>
    public void MouseMove(int x, int y, int steps)
    {
        if (steps <= 0)
        {
            Mouse.MoveTo(x, y);
            return;
        }

        var current = Mouse.Position;
        var dx = (x - current.X) / (double)steps;
        var dy = (y - current.Y) / (double)steps;

        for (var i = 1; i <= steps; i++)
        {
            Mouse.MoveTo(
                (int)(current.X + dx * i),
                (int)(current.Y + dy * i));
        }
    }

    /// <inheritdoc/>
    public void MouseWheel(int dx, int dy)
    {
        if (dy != 0)
            Mouse.Scroll(dy);

        if (dx != 0)
            Mouse.HorizontalScroll(dx);
    }

    /// <inheritdoc/>
    public void MouseDown(MouseButton button) => Mouse.Down(ToFlaUiButton(button));

    /// <inheritdoc/>
    public void MouseUp(MouseButton button) => Mouse.Up(ToFlaUiButton(button));

    /// <inheritdoc/>
    public void KeyboardPress(VirtualKeyShort key) => Keyboard.Press(key);

    /// <inheritdoc/>
    public void KeyboardRelease(VirtualKeyShort key) => Keyboard.Release(key);

    /// <inheritdoc/>
    public void KeyboardType(string text) => Keyboard.Type(text);

    /// <inheritdoc/>
    public void KeyboardTap(VirtualKeyShort key) => Keyboard.Type(key);
}
