using Flawright.Backends;
using Flawright.Input;

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
    public void Click(IElementBackend element, IInputBackend input)
    {
        element.Click();
    }

    /// <inheritdoc/>
    public void DoubleClick(IElementBackend element, IInputBackend input)
    {
        element.DoubleClick();
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
