using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Flawright.Backends;

/// <summary>
/// Production <see cref="ICursorController"/> that forwards to the Win32
/// <c>GetCursorPos</c> / <c>SetCursorPos</c> APIs.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Win32 P/Invoke; covered by E2E tests only.")]
internal sealed class Win32CursorController : ICursorController
{
    [DllImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int X, int Y);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    /// <inheritdoc/>
    public Point GetPosition()
    {
        if (GetCursorPos(out var pt))
            return new Point(pt.X, pt.Y);

        return Point.Empty;
    }

    /// <inheritdoc/>
    public void SetPosition(int x, int y) => SetCursorPos(x, y);
}
