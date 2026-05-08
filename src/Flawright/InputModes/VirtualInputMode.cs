#pragma warning disable CA1725 // Parameter names intentionally use discard-style names for unused parameters in always-throwing methods

using Flawright.Backends;

namespace Flawright.InputModes;

/// <summary>
/// Input mode that drives the application via UIA patterns directly
/// (<c>InvokePattern</c>, <c>ValuePattern</c>, etc.). Does not touch the
/// user's mouse or keyboard — no focus-steal, no cursor movement.
///
/// <para>Recommended for CI runs and bulk test suites where tests should
/// run concurrently or unattended. Some actions (hover, drag, double-click,
/// key chords) have no UIA equivalent and throw
/// <see cref="NotSupportedException"/>.</para>
/// </summary>
public sealed class VirtualInputMode : IInputMode
{
    /// <inheritdoc/>
    /// <remarks>
    /// Attempts <c>InvokePattern.Invoke()</c>, falling back to
    /// <c>LegacyIAccessiblePattern.DoDefaultAction()</c>. Throws
    /// <see cref="NotSupportedException"/> if neither pattern is available.
    /// </remarks>
    public void Click(IElementBackend element, IInputBackend input)
    {
        if (!element.TryInvoke())
            throw new NotSupportedException(
                "ClickAsync target does not support InvokePattern or LegacyIAccessiblePattern. " +
                "To use this element in virtual input mode, ensure it implements one of those patterns, " +
                "or configure FlawrightOptions { InputMode = new RealInputMode() }.");
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">Always thrown — UIA has no generic double-click equivalent.</exception>
    public void DoubleClick(IElementBackend element, IInputBackend input)
        => throw new NotSupportedException(
            "DoubleClickAsync is not supported in virtual input mode. " +
            "UIA has no generic double-click equivalent. " +
            "To use this action, configure FlawrightOptions { InputMode = new RealInputMode() }.");

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">Always thrown — UIA has no equivalent for cursor movement.</exception>
    public void Hover(IElementBackend element, IInputBackend input)
        => throw new NotSupportedException(
            "HoverAsync is not supported in virtual input mode. " +
            "UIA has no equivalent for cursor movement. " +
            "To use this action, configure FlawrightOptions { InputMode = new RealInputMode() }.");

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">Always thrown — UIA has no drag-and-drop equivalent.</exception>
    public void DragTo(IElementBackend source, IElementBackend target, IInputBackend input)
        => throw new NotSupportedException(
            "DragToAsync is not supported in virtual input mode. " +
            "UIA has no equivalent for mouse drag operations. " +
            "To use this action, configure FlawrightOptions { InputMode = new RealInputMode() }.");

    /// <inheritdoc/>
    /// <remarks>
    /// Soft-degrades to <c>ValuePattern.SetValue</c>, setting the entire string
    /// at once. Per-keystroke event handlers will not fire; use
    /// <see cref="RealInputMode"/> if keystroke events are required.
    /// </remarks>
    public void Type(IElementBackend element, string text, IInputBackend input)
    {
        if (!element.TrySetValue(text))
            throw new NotSupportedException(
                "TypeAsync target does not support ValuePattern in virtual input mode. " +
                "Configure FlawrightOptions { InputMode = new RealInputMode() } to use real key input.");
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">Always thrown — key chords have no UIA equivalent.</exception>
    public void Press(IElementBackend element, string keyChord, IInputBackend input)
        => throw new NotSupportedException(
            "PressAsync is not supported in virtual input mode. " +
            "Key chords have no UIA equivalent. " +
            "To use this action, configure FlawrightOptions { InputMode = new RealInputMode() }.");
}
