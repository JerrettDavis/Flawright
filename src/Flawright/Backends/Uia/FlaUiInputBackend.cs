using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUiMouseButton = FlaUI.Core.Input.MouseButton;

// CA5394: Random is used intentionally for non-security purposes (cursor jitter)
// — cryptographically secure randomness is not needed here.
#pragma warning disable CA5394

namespace Flawright.Backends.Uia;

/// <summary>
/// FlaUI-backed <see cref="IInputBackend"/> that delegates to
/// <see cref="FlaUI.Core.Input.Mouse"/> and
/// <see cref="FlaUI.Core.Input.Keyboard"/>.
///
/// <para>Mouse movement uses a quadratic Bézier curve with perpendicular
/// jitter for natural-looking, time-bounded motion.  The
/// <see cref="ICursorController"/> seam can be replaced in unit tests.</para>
/// </summary>
[ExcludeFromCodeCoverage(Justification = "FlaUI I/O; covered by E2E tests only.")]
internal sealed class FlaUiInputBackend : IInputBackend
{
    private readonly ICursorController _cursor;

    // Random.Shared is thread-safe in .NET 6+ and sufficient for cursor jitter.
    private static readonly Random _rng = Random.Shared;

    /// <summary>
    /// Initialises the backend with the default Win32 cursor controller.
    /// </summary>
    internal FlaUiInputBackend()
        : this(new Win32CursorController()) { }

    /// <summary>
    /// Initialises the backend with the supplied cursor controller.
    /// Intended for unit tests that inject a fake.
    /// </summary>
    internal FlaUiInputBackend(ICursorController cursor)
    {
        _cursor = cursor;
    }

    private static FlaUiMouseButton ToFlaUiButton(MouseButton button) => button switch
    {
        MouseButton.Right => FlaUiMouseButton.Right,
        MouseButton.Middle => FlaUiMouseButton.Middle,
        _ => FlaUiMouseButton.Left
    };

    // ── Mouse ─────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void MouseClick(int x, int y, MouseButton button, int clickCount)
    {
        // Move with realistic Bézier curve, then click at exact target.
        MouseMove(x, y, steps: 0);
        var flaButton = ToFlaUiButton(button);

        if (clickCount == 2)
        {
            Mouse.DoubleClick(flaButton);
        }
        else
        {
            for (var i = 0; i < clickCount; i++)
                Mouse.Click(flaButton);
        }
    }

    /// <summary>
    /// Moves the cursor from its current position to
    /// (<paramref name="x"/>, <paramref name="y"/>) using a quadratic Bézier
    /// curve with perpendicular jitter.
    /// </summary>
    /// <param name="x">Destination screen X coordinate.</param>
    /// <param name="y">Destination screen Y coordinate.</param>
    /// <param name="steps">
    /// Hint for intermediate positions.  Values ≤ 0 use an auto-computed
    /// step count derived from the travel distance.
    /// </param>
    public void MouseMove(int x, int y, int steps)
    {
        var start = _cursor.GetPosition();

        if (start.X == x && start.Y == y)
            return;

        var dx = x - start.X;
        var dy = y - start.Y;
        var distance = Math.Sqrt((double)dx * dx + (double)dy * dy);

        // Bézier control point: perpendicular to the start→end vector,
        // offset by 10–25 % of the total distance, randomised side.
        var perpX = -dy / distance;
        var perpY = dx / distance;
        var side = _rng.Next(2) == 0 ? -1.0 : 1.0;
        var bezierOffset = distance * (0.10 + _rng.NextDouble() * 0.15) * side;
        var ctrlX = (start.X + x) / 2.0 + perpX * bezierOffset;
        var ctrlY = (start.Y + y) / 2.0 + perpY * bezierOffset;

        // Step count: scale with distance, clamped to [12, 80].
        var effectiveSteps = steps > 0
            ? Math.Clamp(steps, 12, 80)
            : Math.Clamp((int)(distance / 4.0), 12, 80);

        // Total motion duration: ~60–220 ms scaled with distance.
        var totalMs = Math.Clamp(distance / 8.0, 60.0, 220.0);
        var perStepMs = totalMs / effectiveSteps;

        var sw = Stopwatch.StartNew();

        for (var i = 1; i <= effectiveSteps; i++)
        {
            var t = (double)i / effectiveSteps;
            var u = 1.0 - t;

            // Quadratic Bézier position.
            var px = u * u * start.X + 2.0 * u * t * ctrlX + t * t * x;
            var py = u * u * start.Y + 2.0 * u * t * ctrlY + t * t * y;

            // Small perpendicular jitter — natural micro-wobble.
            var jx = (_rng.NextDouble() - 0.5) * 2.5;
            var jy = (_rng.NextDouble() - 0.5) * 2.5;
            px += jx;
            py += jy;

            _cursor.SetPosition((int)Math.Round(px), (int)Math.Round(py));

            // Pace each step to maintain the target total duration.
            var elapsed = sw.Elapsed.TotalMilliseconds;
            var targetMs = i * perStepMs;
            var sleepMs = targetMs - elapsed;
            if (sleepMs > 1.0)
                Thread.Sleep((int)sleepMs);
        }

        // Final snap to exact target — no jitter on landing.
        _cursor.SetPosition(x, y);
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

    // ── Keyboard ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void KeyboardPress(VirtualKeyShort key) => Keyboard.Press(key);

    /// <inheritdoc/>
    public void KeyboardRelease(VirtualKeyShort key) => Keyboard.Release(key);

    /// <inheritdoc/>
    public void KeyboardType(string text) => Keyboard.Type(text);

    /// <inheritdoc/>
    public void KeyboardTap(VirtualKeyShort key) => Keyboard.Type(key);
}
