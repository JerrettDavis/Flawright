using FlaUI.Core.WindowsAPI;
using Flawright.Backends;

namespace Flawright.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="IInputBackend"/> for unit tests.
///
/// Records every input action so tests can assert what was sent without
/// dispatching real OS-level input.
/// </summary>
internal sealed class FakeInputBackend : IInputBackend
{
    // ── Recorded actions ──────────────────────────────────────────────────────

    /// <summary>All mouse clicks recorded, in order.</summary>
    public IReadOnlyList<MouseClickRecord> MouseClicks => _mouseClicks.AsReadOnly();
    private readonly List<MouseClickRecord> _mouseClicks = [];

    /// <summary>All mouse move operations recorded, in order.</summary>
    public IReadOnlyList<MouseMoveRecord> MouseMoves => _mouseMoves.AsReadOnly();
    private readonly List<MouseMoveRecord> _mouseMoves = [];

    /// <summary>All mouse wheel operations recorded, in order.</summary>
    public IReadOnlyList<(int Dx, int Dy)> MouseWheels => _mouseWheels.AsReadOnly();
    private readonly List<(int Dx, int Dy)> _mouseWheels = [];

    /// <summary>All mouse down operations recorded, in order.</summary>
    public IReadOnlyList<MouseButton> MouseDowns => _mouseDowns.AsReadOnly();
    private readonly List<MouseButton> _mouseDowns = [];

    /// <summary>All mouse up operations recorded, in order.</summary>
    public IReadOnlyList<MouseButton> MouseUps => _mouseUps.AsReadOnly();
    private readonly List<MouseButton> _mouseUps = [];

    /// <summary>All keyboard press operations recorded, in order.</summary>
    public IReadOnlyList<VirtualKeyShort> KeyPresses => _keyPresses.AsReadOnly();
    private readonly List<VirtualKeyShort> _keyPresses = [];

    /// <summary>All keyboard release operations recorded, in order.</summary>
    public IReadOnlyList<VirtualKeyShort> KeyReleases => _keyReleases.AsReadOnly();
    private readonly List<VirtualKeyShort> _keyReleases = [];

    /// <summary>All keyboard type strings recorded, in order.</summary>
    public IReadOnlyList<string> TypedTexts => _typedTexts.AsReadOnly();
    private readonly List<string> _typedTexts = [];

    /// <summary>All keyboard tap (press+release) operations recorded, in order.</summary>
    public IReadOnlyList<VirtualKeyShort> KeyTaps => _keyTaps.AsReadOnly();
    private readonly List<VirtualKeyShort> _keyTaps = [];

    // ── IInputBackend ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void MouseClick(int x, int y, MouseButton button, int clickCount)
        => _mouseClicks.Add(new MouseClickRecord(x, y, button, clickCount));

    /// <inheritdoc/>
    public void MouseMove(int x, int y, int steps)
        => _mouseMoves.Add(new MouseMoveRecord(x, y, steps));

    /// <inheritdoc/>
    public void MouseWheel(int dx, int dy)
        => _mouseWheels.Add((dx, dy));

    /// <inheritdoc/>
    public void MouseDown(MouseButton button)
        => _mouseDowns.Add(button);

    /// <inheritdoc/>
    public void MouseUp(MouseButton button)
        => _mouseUps.Add(button);

    /// <inheritdoc/>
    public void KeyboardPress(VirtualKeyShort key)
        => _keyPresses.Add(key);

    /// <inheritdoc/>
    public void KeyboardRelease(VirtualKeyShort key)
        => _keyReleases.Add(key);

    /// <inheritdoc/>
    public void KeyboardType(string text)
        => _typedTexts.Add(text);

    /// <inheritdoc/>
    public void KeyboardTap(VirtualKeyShort key)
        => _keyTaps.Add(key);

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Clears all recorded interactions.</summary>
    public void Reset()
    {
        _mouseClicks.Clear();
        _mouseMoves.Clear();
        _mouseWheels.Clear();
        _mouseDowns.Clear();
        _mouseUps.Clear();
        _keyPresses.Clear();
        _keyReleases.Clear();
        _typedTexts.Clear();
        _keyTaps.Clear();
    }

    // ── Record types ──────────────────────────────────────────────────────────

    /// <summary>A recorded mouse click operation.</summary>
    /// <param name="X">Screen X coordinate.</param>
    /// <param name="Y">Screen Y coordinate.</param>
    /// <param name="Button">Which button was clicked.</param>
    /// <param name="ClickCount">Number of clicks.</param>
    internal sealed record MouseClickRecord(int X, int Y, MouseButton Button, int ClickCount);

    /// <summary>A recorded mouse move operation.</summary>
    /// <param name="X">Destination X coordinate.</param>
    /// <param name="Y">Destination Y coordinate.</param>
    /// <param name="Steps">Number of intermediate steps.</param>
    internal sealed record MouseMoveRecord(int X, int Y, int Steps);
}
