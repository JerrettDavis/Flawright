using System.Drawing;
using Flawright.Backends;

namespace Flawright.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="ICursorController"/> for unit tests.
///
/// Starts at a configurable initial position and records every
/// <see cref="SetPosition"/> call without touching the real OS cursor.
/// </summary>
internal sealed class FakeCursorController : ICursorController
{
    private Point _current;

    /// <summary>All positions written via <see cref="SetPosition"/>, in order.</summary>
    public IReadOnlyList<Point> Positions => _positions.AsReadOnly();
    private readonly List<Point> _positions = [];

    /// <summary>
    /// Creates a fake controller whose cursor starts at
    /// (<paramref name="startX"/>, <paramref name="startY"/>).
    /// </summary>
    public FakeCursorController(int startX = 0, int startY = 0)
    {
        _current = new Point(startX, startY);
    }

    /// <inheritdoc/>
    public Point GetPosition() => _current;

    /// <inheritdoc/>
    public void SetPosition(int x, int y)
    {
        _current = new Point(x, y);
        _positions.Add(_current);
    }
}
