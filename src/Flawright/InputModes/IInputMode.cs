using Flawright.Backends;

namespace Flawright.InputModes;

/// <summary>
/// Strategy for performing input actions (clicks, key presses, mouse movements)
/// against an application. Implement this to define how user actions are
/// translated into UIA calls or OS-level input. Built-in implementations live
/// in this namespace.
///
/// <para>Two built-ins ship out of the box:
/// <list type="bullet">
///   <item><see cref="RealInputMode"/> — the default; uses real mouse and keyboard
///   input via Win32 <c>SendInput</c>. Matches a user driving the application
///   manually. Steals focus and the cursor.</item>
///   <item><see cref="VirtualInputMode"/> — uses UIA patterns directly
///   (<c>InvokePattern</c>, <c>ValuePattern</c>, etc.). Does not touch the user's
///   peripherals. Allows concurrent tests against the same app and unattended
///   CI runs. Some actions (hover, drag, key chords, double-click) have no UIA
///   equivalent and throw <see cref="NotSupportedException"/>.</item>
/// </list>
/// </para>
/// </summary>
public interface IInputMode
{
    /// <summary>Performs a single click on the element.</summary>
    /// <param name="element">The element to click.</param>
    /// <param name="input">The input backend to use for the click.</param>
    void Click(IElementBackend element, IInputBackend input);

    /// <summary>Performs a double-click on the element.</summary>
    /// <param name="element">The element to double-click.</param>
    /// <param name="input">The input backend to use for the double-click.</param>
    void DoubleClick(IElementBackend element, IInputBackend input);

    /// <summary>Moves the mouse cursor over the element.</summary>
    /// <param name="element">The element to hover over.</param>
    /// <param name="input">The input backend to use for the hover.</param>
    void Hover(IElementBackend element, IInputBackend input);

    /// <summary>Drags from <paramref name="source"/> and drops onto <paramref name="target"/>.</summary>
    /// <param name="source">The element to drag from.</param>
    /// <param name="target">The element to drop onto.</param>
    /// <param name="input">The input backend to use for the drag.</param>
    void DragTo(IElementBackend source, IElementBackend target, IInputBackend input);

    /// <summary>
    /// Types the given text into the element. In real mode, sends per-keystroke
    /// input; in virtual mode, sets the entire value via UIA <c>ValuePattern</c>.
    /// </summary>
    /// <param name="element">The element to type into.</param>
    /// <param name="text">The text to type.</param>
    /// <param name="input">The input backend to use for typing.</param>
    void Type(IElementBackend element, string text, IInputBackend input);

    /// <summary>
    /// Presses the given key chord (e.g. "Ctrl+S", "Enter") on the element. In
    /// real mode, sends real key events. In virtual mode, throws
    /// <see cref="NotSupportedException"/>.
    /// </summary>
    /// <param name="element">The element to send the key press to.</param>
    /// <param name="keyChord">The key chord to press (e.g. "Ctrl+S", "Enter").</param>
    /// <param name="input">The input backend to use for the key press.</param>
    void Press(IElementBackend element, string keyChord, IInputBackend input);
}
