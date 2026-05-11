using Flawright.Backends;
using Flawright.Input;
using Flawright.Locator;
using FlaUI.Core.WindowsAPI;

namespace Flawright.InputModes;

/// <summary>
/// Default input mode that uses real OS-level mouse and keyboard input via
/// Win32 <c>SendInput</c> (through FlaUI). Matches a user driving the
/// application manually — steals focus and the cursor.
///
/// <para>This is the default when no <see cref="IInputMode"/> is configured
/// on <see cref="FlawrightOptions"/>.</para>
/// </summary>
public sealed class RealInputMode : IInputMode
{
    /// <inheritdoc/>
    public void Click(IElementBackend element, IInputBackend input, MouseButton button = MouseButton.Left, BoundingBox? position = null, KeyModifiers modifiers = KeyModifiers.None, int clickCount = 1, TimeSpan? delay = null)
    {
        var rect = element.BoundingRectangle;
        var x = position is { X: var posX, Y: var posY }
            ? (int)(rect.X + posX)
            : rect.X + rect.Width / 2;
        var y = position is { X: var _, Y: var posY2 }
            ? (int)(rect.Y + posY2)
            : rect.Y + rect.Height / 2;

        PressModifiers(input, modifiers);
        try
        {
            if (delay.HasValue)
            {
                input.MouseDown(button);
                System.Threading.Thread.Sleep((int)delay.Value.TotalMilliseconds);
                input.MouseUp(button);
            }
            else
            {
                input.MouseClick(x, y, button, clickCount);
            }
        }
        finally
        {
            ReleaseModifiers(input, modifiers);
        }
    }

    /// <inheritdoc/>
    public void DoubleClick(IElementBackend element, IInputBackend input, MouseButton button = MouseButton.Left, BoundingBox? position = null, KeyModifiers modifiers = KeyModifiers.None, TimeSpan? delay = null)
    {
        var rect = element.BoundingRectangle;
        var x = position is { X: var posX, Y: var posY }
            ? (int)(rect.X + posX)
            : rect.X + rect.Width / 2;
        var y = position is { X: var _, Y: var posY2 }
            ? (int)(rect.Y + posY2)
            : rect.Y + rect.Height / 2;

        PressModifiers(input, modifiers);
        try
        {
            input.MouseClick(x, y, button, 2);
            if (delay.HasValue)
            {
                System.Threading.Thread.Sleep((int)delay.Value.TotalMilliseconds);
            }
        }
        finally
        {
            ReleaseModifiers(input, modifiers);
        }
    }

    private static void PressModifiers(IInputBackend input, KeyModifiers modifiers)
    {
        if ((modifiers & KeyModifiers.Control) != KeyModifiers.None)
            input.KeyboardPress(VirtualKeyShort.CONTROL);
        if ((modifiers & KeyModifiers.Shift) != KeyModifiers.None)
            input.KeyboardPress(VirtualKeyShort.SHIFT);
        if ((modifiers & KeyModifiers.Alt) != KeyModifiers.None)
            input.KeyboardPress(VirtualKeyShort.ALT);
        if ((modifiers & KeyModifiers.Meta) != KeyModifiers.None)
            input.KeyboardPress(VirtualKeyShort.LWIN);
    }

    private static void ReleaseModifiers(IInputBackend input, KeyModifiers modifiers)
    {
        if ((modifiers & KeyModifiers.Control) != KeyModifiers.None)
            input.KeyboardRelease(VirtualKeyShort.CONTROL);
        if ((modifiers & KeyModifiers.Shift) != KeyModifiers.None)
            input.KeyboardRelease(VirtualKeyShort.SHIFT);
        if ((modifiers & KeyModifiers.Alt) != KeyModifiers.None)
            input.KeyboardRelease(VirtualKeyShort.ALT);
        if ((modifiers & KeyModifiers.Meta) != KeyModifiers.None)
            input.KeyboardRelease(VirtualKeyShort.LWIN);
    }

    /// <inheritdoc/>
    public void Hover(IElementBackend element, IInputBackend input)
    {
        var rect = element.BoundingRectangle;
        var x = rect.X + rect.Width / 2;
        var y = rect.Y + rect.Height / 2;
        input.MouseMove(x, y, steps: 0);
    }

    /// <inheritdoc/>
    public void DragTo(IElementBackend source, IElementBackend target, IInputBackend input)
    {
        var sourceRect = source.BoundingRectangle;
        var targetRect = target.BoundingRectangle;

        var srcX = sourceRect.X + sourceRect.Width / 2;
        var srcY = sourceRect.Y + sourceRect.Height / 2;
        var tgtX = targetRect.X + targetRect.Width / 2;
        var tgtY = targetRect.Y + targetRect.Height / 2;

        input.MouseMove(srcX, srcY, steps: 0);
        input.MouseDown(MouseButton.Left);
        input.MouseMove(tgtX, tgtY, steps: 10);
        input.MouseUp(MouseButton.Left);
    }

    /// <inheritdoc/>
    public void Type(IElementBackend element, string text, IInputBackend input)
    {
        element.Focus();
        input.KeyboardType(text);
    }

    /// <inheritdoc/>
    public void Press(IElementBackend element, string keyChord, IInputBackend input)
    {
        element.Focus();
        var vk = KeyParser.ParseKey(keyChord.Split('+')[^1].Trim());
        input.KeyboardTap(vk);
    }
}
