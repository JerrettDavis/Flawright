using System.Diagnostics.CodeAnalysis;
using JerrettDavis.Flawright.Backends;

namespace JerrettDavis.Flawright.Page;

// ── Mouse ─────────────────────────────────────────────────────────────────────

/// <summary>Options for <c>IFlawrightMouse.ClickAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record MouseClickOptions
{
    /// <summary>Which mouse button to click. Default is <see cref="MouseButton.Left"/>.</summary>
    public MouseButton Button { get; init; } = MouseButton.Left;

    /// <summary>Number of clicks. Default is 1.</summary>
    public int ClickCount { get; init; } = 1;

    /// <summary>Delay between mouse down and mouse up. <see langword="null"/> = no delay.</summary>
    public TimeSpan? Delay { get; init; }
}

/// <summary>Options for <c>IFlawrightMouse.DoubleClickAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record MouseDoubleClickOptions
{
    /// <summary>Which mouse button to double-click. Default is <see cref="MouseButton.Left"/>.</summary>
    public MouseButton Button { get; init; } = MouseButton.Left;

    /// <summary>Delay between the two clicks. <see langword="null"/> = no delay.</summary>
    public TimeSpan? Delay { get; init; }
}

/// <summary>Options for <c>IFlawrightMouse.DownAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record MouseDownOptions
{
    /// <summary>Which mouse button to press. Default is <see cref="MouseButton.Left"/>.</summary>
    public MouseButton Button { get; init; } = MouseButton.Left;

    /// <summary>Click count (used for multi-click press). Default is 1.</summary>
    public int ClickCount { get; init; } = 1;
}

/// <summary>Options for <c>IFlawrightMouse.UpAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record MouseUpOptions
{
    /// <summary>Which mouse button to release. Default is <see cref="MouseButton.Left"/>.</summary>
    public MouseButton Button { get; init; } = MouseButton.Left;

    /// <summary>Click count (used for multi-click release). Default is 1.</summary>
    public int ClickCount { get; init; } = 1;
}

/// <summary>Options for <c>IFlawrightMouse.MoveAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record MouseMoveOptions
{
    /// <summary>Number of intermediate positions to generate during the move. Default is 1.</summary>
    public int Steps { get; init; } = 1;
}

// ── Keyboard ──────────────────────────────────────────────────────────────────

/// <summary>Options for <c>IFlawrightKeyboard.PressAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record KeyboardPressOptions
{
    /// <summary>Delay between key down and key up. <see langword="null"/> = no delay.</summary>
    public TimeSpan? Delay { get; init; }
}

/// <summary>Options for <c>IFlawrightKeyboard.TypeAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record KeyboardTypeOptions
{
    /// <summary>Delay between each character key-press. <see langword="null"/> = no delay.</summary>
    public TimeSpan? Delay { get; init; }
}
