using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using JerrettDavis.Flawright.Backends;

namespace JerrettDavis.Flawright.Locator;

// ── Click / Double-click ───────────────────────────────────────────────────────

/// <summary>Options for <c>IFlawrightLocator.ClickAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorClickOptions
{
    /// <summary>Which mouse button to click. Default is <see cref="MouseButton.Left"/>.</summary>
    public MouseButton Button { get; init; } = MouseButton.Left;

    /// <summary>Number of clicks. Default is 1.</summary>
    public int ClickCount { get; init; } = 1;

    /// <summary>Delay between mouse down and mouse up. <see langword="null"/> = no delay.</summary>
    public TimeSpan? Delay { get; init; }

    /// <summary>Keyboard modifiers to hold during the click.</summary>
    public KeyModifiers Modifiers { get; init; } = KeyModifiers.None;

    /// <summary>Click position relative to the element's bounding box. <see langword="null"/> = centre.</summary>
    public BoundingBox? Position { get; init; }

    /// <summary>Skip actionability checks and click regardless of element state.</summary>
    public bool Force { get; init; }

    /// <summary>Whether to skip waiting for network/UI to settle after the action.</summary>
    public bool NoWaitAfter { get; init; }

    /// <summary>Per-call timeout override. <see langword="null"/> uses the default timeout.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Perform the click without actually executing it (trial run).</summary>
    public bool Trial { get; init; }
}

/// <summary>Options for <c>IFlawrightLocator.DoubleClickAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorDoubleClickOptions
{
    /// <summary>Which mouse button to double-click. Default is <see cref="MouseButton.Left"/>.</summary>
    public MouseButton Button { get; init; } = MouseButton.Left;

    /// <summary>Delay between the two clicks. <see langword="null"/> = no delay.</summary>
    public TimeSpan? Delay { get; init; }

    /// <summary>Keyboard modifiers to hold during the double-click.</summary>
    public KeyModifiers Modifiers { get; init; } = KeyModifiers.None;

    /// <summary>Click position relative to the element's bounding box. <see langword="null"/> = centre.</summary>
    public BoundingBox? Position { get; init; }

    /// <summary>Skip actionability checks.</summary>
    public bool Force { get; init; }

    /// <summary>Whether to skip waiting for network/UI to settle after the action.</summary>
    public bool NoWaitAfter { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Perform without actually executing (trial run).</summary>
    public bool Trial { get; init; }
}

// ── Fill / Clear ───────────────────────────────────────────────────────────────

/// <summary>Options for <c>IFlawrightLocator.FillAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorFillOptions
{
    /// <summary>Skip actionability checks.</summary>
    public bool Force { get; init; }

    /// <summary>Whether to skip waiting for network/UI to settle after the action.</summary>
    public bool NoWaitAfter { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>IFlawrightLocator.ClearAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorClearOptions
{
    /// <summary>Skip actionability checks.</summary>
    public bool Force { get; init; }

    /// <summary>Whether to skip waiting for network/UI to settle after the action.</summary>
    public bool NoWaitAfter { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

// ── Type / PressSequentially / Press ──────────────────────────────────────────

/// <summary>Options for <c>IFlawrightLocator.TypeAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorTypeOptions
{
    /// <summary>Delay between each character key-press. <see langword="null"/> = no delay.</summary>
    public TimeSpan? Delay { get; init; }

    /// <summary>Whether to skip waiting for network/UI to settle after the action.</summary>
    public bool NoWaitAfter { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>IFlawrightLocator.PressSequentiallyAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorPressSequentiallyOptions
{
    /// <summary>Delay between each character key-press. <see langword="null"/> = no delay.</summary>
    public TimeSpan? Delay { get; init; }

    /// <summary>Whether to skip waiting for network/UI to settle after the action.</summary>
    public bool NoWaitAfter { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>IFlawrightLocator.PressAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorPressOptions
{
    /// <summary>Delay between key down and key up. <see langword="null"/> = no delay.</summary>
    public TimeSpan? Delay { get; init; }

    /// <summary>Whether to skip waiting for network/UI to settle after the action.</summary>
    public bool NoWaitAfter { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

// ── Check / Uncheck / SetChecked ──────────────────────────────────────────────

/// <summary>Options for <c>IFlawrightLocator.CheckAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorCheckOptions
{
    /// <summary>Skip actionability checks.</summary>
    public bool Force { get; init; }

    /// <summary>Whether to skip waiting for network/UI to settle after the action.</summary>
    public bool NoWaitAfter { get; init; }

    /// <summary>Click position relative to the element's bounding box. <see langword="null"/> = centre.</summary>
    public BoundingBox? Position { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Perform without actually executing (trial run).</summary>
    public bool Trial { get; init; }
}

/// <summary>Options for <c>IFlawrightLocator.UncheckAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorUncheckOptions
{
    /// <summary>Skip actionability checks.</summary>
    public bool Force { get; init; }

    /// <summary>Whether to skip waiting for network/UI to settle after the action.</summary>
    public bool NoWaitAfter { get; init; }

    /// <summary>Click position relative to the element's bounding box. <see langword="null"/> = centre.</summary>
    public BoundingBox? Position { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Perform without actually executing (trial run).</summary>
    public bool Trial { get; init; }
}

/// <summary>Options for <c>IFlawrightLocator.SetCheckedAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorSetCheckedOptions
{
    /// <summary>Skip actionability checks.</summary>
    public bool Force { get; init; }

    /// <summary>Whether to skip waiting for network/UI to settle after the action.</summary>
    public bool NoWaitAfter { get; init; }

    /// <summary>Click position relative to the element's bounding box. <see langword="null"/> = centre.</summary>
    public BoundingBox? Position { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Perform without actually executing (trial run).</summary>
    public bool Trial { get; init; }
}

// ── SelectOption ──────────────────────────────────────────────────────────────

/// <summary>Options for <c>IFlawrightLocator.SelectOptionAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorSelectOptionOptions
{
    /// <summary>Skip actionability checks.</summary>
    public bool Force { get; init; }

    /// <summary>Whether to skip waiting for network/UI to settle after the action.</summary>
    public bool NoWaitAfter { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

// ── Hover ─────────────────────────────────────────────────────────────────────

/// <summary>Options for <c>IFlawrightLocator.HoverAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorHoverOptions
{
    /// <summary>Skip actionability checks.</summary>
    public bool Force { get; init; }

    /// <summary>Keyboard modifiers to hold during hover.</summary>
    public KeyModifiers Modifiers { get; init; } = KeyModifiers.None;

    /// <summary>Hover position relative to the element's bounding box. <see langword="null"/> = centre.</summary>
    public BoundingBox? Position { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Perform without actually executing (trial run).</summary>
    public bool Trial { get; init; }
}

// ── DragTo ────────────────────────────────────────────────────────────────────

/// <summary>Options for <c>IFlawrightLocator.DragToAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorDragToOptions
{
    /// <summary>Skip actionability checks.</summary>
    public bool Force { get; init; }

    /// <summary>Whether to skip waiting for network/UI to settle after the action.</summary>
    public bool NoWaitAfter { get; init; }

    /// <summary>Source position offset relative to the source element's bounding box.</summary>
    public BoundingBox? SourcePosition { get; init; }

    /// <summary>Target position offset relative to the target element's bounding box.</summary>
    public BoundingBox? TargetPosition { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Perform without actually executing (trial run).</summary>
    public bool Trial { get; init; }
}

// ── Screenshot ────────────────────────────────────────────────────────────────

/// <summary>Screenshot image format.</summary>
public enum ScreenshotType
{
    /// <summary>PNG format (lossless).</summary>
    Png,

    /// <summary>JPEG format (lossy, smaller).</summary>
    Jpeg
}

/// <summary>Options for <c>IFlawrightLocator.ScreenshotAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorScreenshotOptions
{
    /// <summary>Optional file path to save the screenshot.</summary>
    public string? Path { get; init; }

    /// <summary>Image format. Default is <see cref="ScreenshotType.Png"/>.</summary>
    public ScreenshotType Type { get; init; } = ScreenshotType.Png;

    /// <summary>Quality (0-100) for JPEG screenshots. Ignored for PNG.</summary>
    public int? Quality { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Whether to render CSS animations at their end state.</summary>
    public bool Animations { get; init; }

    /// <summary>Whether to include the text cursor in the screenshot.</summary>
    public bool Caret { get; init; }

    /// <summary>Locators to mask (rendered as coloured overlays).</summary>
    public IFlawrightLocator[]? Mask { get; init; }

    /// <summary>Colour used for masking (e.g. "#FF00FF"). Default is pink.</summary>
    public string? MaskColor { get; init; }

    /// <summary>Whether to render the page with a transparent background.</summary>
    public bool OmitBackground { get; init; }

    /// <summary>Device scale factor for the screenshot.</summary>
    public double? Scale { get; init; }
}

// ── WaitFor ───────────────────────────────────────────────────────────────────

/// <summary>Options for <c>IFlawrightLocator.WaitForAsync</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorWaitForOptions
{
    /// <summary>State to wait for. Default is <see cref="WaitForState.Visible"/>.</summary>
    public WaitForState State { get; init; } = WaitForState.Visible;

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

// ── Filter ────────────────────────────────────────────────────────────────────

/// <summary>Options for <c>IFlawrightLocator.Filter</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorFilterOptions
{
    /// <summary>Narrows results to elements that also match this locator.</summary>
    public IFlawrightLocator? Has { get; init; }

    /// <summary>Narrows results to elements that do <em>not</em> match this locator.</summary>
    public IFlawrightLocator? HasNot { get; init; }

    /// <summary>Narrows results to elements whose visible text contains <see cref="HasText"/>.</summary>
    public string? HasText { get; init; }

    /// <summary>Narrows results to elements whose visible text matches this regex.</summary>
    public Regex? HasTextRegex { get; init; }

    /// <summary>Narrows results to elements whose visible text does <em>not</em> contain <see cref="HasNotText"/>.</summary>
    public string? HasNotText { get; init; }

    /// <summary>Narrows results to elements whose visible text does <em>not</em> match this regex.</summary>
    public Regex? HasNotTextRegex { get; init; }

    /// <summary>When set, narrows to visible (<see langword="true"/>) or hidden (<see langword="false"/>) elements only.</summary>
    public bool? Visible { get; init; }
}

// ── GetBy* options ────────────────────────────────────────────────────────────

/// <summary>Options for <c>IFlawrightLocator.GetByRole</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorGetByRoleOptions
{
    /// <summary>Accessible name to match (exact string).</summary>
    public string? Name { get; init; }

    /// <summary>Accessible name regex to match.</summary>
    public Regex? NameRegex { get; init; }

    /// <summary>Whether the name match is exact (vs contains). Default is <see langword="false"/>.</summary>
    public bool Exact { get; init; }

    /// <summary>Filter to checked elements only.</summary>
    public bool? Checked { get; init; }

    /// <summary>Filter to disabled elements only.</summary>
    public bool? Disabled { get; init; }

    /// <summary>Filter to expanded elements only.</summary>
    public bool? Expanded { get; init; }

    /// <summary>Whether to include hidden elements. Default is <see langword="false"/>.</summary>
    public bool IncludeHidden { get; init; }

    /// <summary>Heading level (for role=heading). Only values 1-6 are valid.</summary>
    public int? Level { get; init; }

    /// <summary>Filter to pressed elements only (for toggle buttons).</summary>
    public bool? Pressed { get; init; }

    /// <summary>Filter to selected elements only.</summary>
    public bool? Selected { get; init; }
}

/// <summary>Options for <c>IFlawrightLocator.GetByText</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorGetByTextOptions
{
    /// <summary>
    /// When <see langword="true"/>, the text must match exactly (case-sensitive);
    /// otherwise a substring (contains) match is used. Default is <see langword="false"/>.
    /// </summary>
    public bool Exact { get; init; }
}

/// <summary>Options for <c>IFlawrightLocator.GetByLabel</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorGetByLabelOptions
{
    /// <summary>Whether to require an exact name match vs. contains match. Default is <see langword="false"/>.</summary>
    public bool Exact { get; init; }
}

/// <summary>Options for <c>IFlawrightLocator.GetByPlaceholder</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorGetByPlaceholderOptions
{
    /// <summary>Whether to require an exact placeholder match vs. contains match. Default is <see langword="false"/>.</summary>
    public bool Exact { get; init; }
}

/// <summary>Options for <c>IFlawrightLocator.GetByTitle</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record LocatorGetByTitleOptions
{
    /// <summary>Whether to require an exact title match vs. contains match. Default is <see langword="false"/>.</summary>
    public bool Exact { get; init; }
}

// ── KeyModifiers (flags) ──────────────────────────────────────────────────────

/// <summary>
/// Keyboard modifier flags used by click, hover, and drag options.
/// </summary>
[Flags]
public enum KeyModifiers
{
    /// <summary>No modifier keys.</summary>
    None = 0,

    /// <summary>Shift key.</summary>
    Shift = 1,

    /// <summary>Control key.</summary>
    Control = 2,

    /// <summary>Alt key.</summary>
    Alt = 4,

    /// <summary>Windows/Meta key.</summary>
    Meta = 8
}
