using JerrettDavis.Flawright.Backends;

namespace JerrettDavis.Flawright.Locator;

/// <summary>
/// Index kind for a <see cref="LocatorContext"/>, controlling which element
/// from the matched result set is picked.
/// </summary>
internal enum LocatorIndex
{
    /// <summary>No index restriction — all matches are returned.</summary>
    Any,

    /// <summary>Only the first match is returned.</summary>
    First,

    /// <summary>Only the last match is returned.</summary>
    Last,

    /// <summary>Only the match at <see cref="LocatorContext.NthIndex"/> is returned.</summary>
    Nth,
}

/// <summary>
/// Immutable context carried by <see cref="FlawrightLocator"/>.
/// All chaining/filter/composition methods produce a new <see cref="LocatorContext"/>
/// with updated fields; no field is mutated after construction.
/// </summary>
internal sealed record LocatorContext
{
    // ── Required fields ───────────────────────────────────────────────────────

    /// <summary>The root element from which searches begin (typically a window).</summary>
    public required IElementBackend Root { get; init; }

    /// <summary>Input backend for mouse/keyboard actions.</summary>
    public required IInputBackend Input { get; init; }

    /// <summary>Translates parsed AST nodes into backend-native conditions.</summary>
    public required IConditionTranslator Translator { get; init; }

    /// <summary>Human-readable selector string (used in error messages).</summary>
    public required string Selector { get; init; }

    /// <summary>
    /// The resolved pipeline of backend conditions corresponding to
    /// <see cref="Selector"/>.  Each step in the pipeline narrows the result set
    /// by searching descendants of the previous step's matches.
    /// </summary>
    public required SelectorPipeline Pipeline { get; init; }

    /// <summary>Global options (default timeout, retry interval, etc.).</summary>
    public required FlawrightOptions Options { get; init; }

    // ── Optional / defaulted fields ───────────────────────────────────────────

    /// <summary>
    /// Accumulated post-resolution filter options from <c>.Filter(opts)</c> calls.
    /// Applied in order after the pipeline resolves the candidate set.
    /// </summary>
    public IReadOnlyList<LocatorFilterOptions> Filters { get; init; } = [];

    /// <summary>
    /// Controls which element from the result set is picked.
    /// </summary>
    public LocatorIndex IndexKind { get; init; } = LocatorIndex.Any;

    /// <summary>
    /// Zero-based index used when <see cref="IndexKind"/> is <see cref="LocatorIndex.Nth"/>.
    /// </summary>
    public int NthIndex { get; init; }

    /// <summary>
    /// When non-<see langword="null"/>, the result set is intersected with the
    /// results of this locator (AND composition).
    /// </summary>
    public IFlawrightLocator? AndWith { get; init; }

    /// <summary>
    /// When non-<see langword="null"/>, the result set is unioned with the
    /// results of this locator (OR composition).
    /// </summary>
    public IFlawrightLocator? OrWith { get; init; }
}
