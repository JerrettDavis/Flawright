using System.Drawing;

namespace Flawright.Backends;

/// <summary>
/// Abstraction over Win32 cursor P/Invokes (<c>GetCursorPos</c> /
/// <c>SetCursorPos</c>).
///
/// <para>The production implementation (<see cref="Win32CursorController"/>)
/// calls the real Win32 API.  Tests inject a fake that records calls without
/// touching the actual cursor.</para>
/// </summary>
internal interface ICursorController
{
    /// <summary>Returns the current cursor position in screen coordinates.</summary>
    Point GetPosition();

    /// <summary>Moves the cursor to the specified screen coordinates.</summary>
    /// <param name="x">Screen X coordinate.</param>
    /// <param name="y">Screen Y coordinate.</param>
    void SetPosition(int x, int y);
}
