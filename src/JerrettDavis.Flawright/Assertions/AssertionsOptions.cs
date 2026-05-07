using System.Diagnostics.CodeAnalysis;

namespace JerrettDavis.Flawright.Assertions;

// ── Page assertions ───────────────────────────────────────────────────────────

/// <summary>Options for page-level <c>ToHaveTitleAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record PageAssertionsToHaveTitleOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Whether the comparison is case-insensitive. Default is <see langword="false"/>.</summary>
    public bool IgnoreCase { get; init; }
}

// ── State assertions ──────────────────────────────────────────────────────────

/// <summary>Options for <c>ToBeVisibleAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToBeVisibleOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>ToBeHiddenAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToBeHiddenOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>ToBeEnabledAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToBeEnabledOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>ToBeDisabledAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToBeDisabledOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>ToBeCheckedAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToBeCheckedOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>ToBeFocusedAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToBeFocusedOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>ToBeEditableAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToBeEditableOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>ToBeEmptyAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToBeEmptyOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>ToBeAttachedAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToBeAttachedOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

// ── Text / value / count assertions ──────────────────────────────────────────

/// <summary>Options for <c>ToHaveTextAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToHaveTextOptions
{
    /// <summary>Whether the comparison is case-insensitive. Default is <see langword="false"/>.</summary>
    public bool IgnoreCase { get; init; }

    /// <summary>Whether to use inner text instead of accessible name. Default is <see langword="false"/>.</summary>
    public bool UseInnerText { get; init; }

    /// <summary>Whether to normalize whitespace before comparison. Default is <see langword="false"/>.</summary>
    public bool Normalized { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>ToContainTextAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToContainTextOptions
{
    /// <summary>Whether the comparison is case-insensitive. Default is <see langword="false"/>.</summary>
    public bool IgnoreCase { get; init; }

    /// <summary>Whether to use inner text instead of accessible name. Default is <see langword="false"/>.</summary>
    public bool UseInnerText { get; init; }

    /// <summary>Whether to normalize whitespace before comparison. Default is <see langword="false"/>.</summary>
    public bool Normalized { get; init; }

    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>ToHaveValueAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToHaveValueOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>ToHaveCountAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToHaveCountOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

// ── Attribute / class / id / role / accessible name assertions ────────────────

/// <summary>Options for <c>ToHaveAttributeAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToHaveAttributeOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Whether the attribute value comparison is case-insensitive. Default is <see langword="false"/>.</summary>
    public bool IgnoreCase { get; init; }
}

/// <summary>Options for <c>ToHaveClassAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToHaveClassOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Whether the class comparison is case-insensitive. Default is <see langword="false"/>.</summary>
    public bool IgnoreCase { get; init; }
}

/// <summary>Options for <c>ToHaveIdAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToHaveIdOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>ToHaveRoleAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToHaveRoleOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Options for <c>ToHaveAccessibleNameAsync</c> assertions.</summary>
[ExcludeFromCodeCoverage]
public sealed record AssertionsToHaveAccessibleNameOptions
{
    /// <summary>Per-call timeout override.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Whether the name comparison is case-insensitive. Default is <see langword="false"/>.</summary>
    public bool IgnoreCase { get; init; }
}
