using System.Diagnostics.CodeAnalysis;
using FlaUI.Core.Conditions;

namespace Flawright.Backends.Uia;

/// <summary>
/// FlaUI-backed <see cref="IElementCondition"/> that pairs a native
/// <see cref="ConditionBase"/> with an optional in-memory post-filter.
///
/// The tight coupling to <see cref="UiaElementBackend"/> is intentional:
/// both types are <c>internal</c> and versioned together.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "FlaUI I/O; covered by E2E tests.")]
internal sealed class UiaElementCondition : IElementCondition
{
    private readonly ConditionBase _nativeCondition;
    private readonly Func<IElementBackend, bool>? _postFilter;

    /// <summary>
    /// Initialises a condition backed by a FlaUI <see cref="ConditionBase"/>.
    /// </summary>
    /// <param name="nativeCondition">The FlaUI condition for the native UIA query.</param>
    /// <param name="postFilter">
    /// Optional in-process predicate applied after the native query (used for
    /// substring, prefix, suffix, and word operators that UIA cannot express).
    /// </param>
    internal UiaElementCondition(
        ConditionBase nativeCondition,
        Func<IElementBackend, bool>? postFilter = null)
    {
        _nativeCondition = nativeCondition;
        _postFilter = postFilter;
    }

    /// <summary>Gets the underlying FlaUI condition (for use by <see cref="UiaElementBackend"/>).</summary>
    internal ConditionBase NativeCondition => _nativeCondition;

    /// <summary>Gets the optional post-filter predicate.</summary>
    internal Func<IElementBackend, bool>? PostFilter => _postFilter;

    /// <inheritdoc/>
    public IEnumerable<IElementBackend> FindAllFrom(IElementBackend root)
    {
        // Delegate to the backend so it can apply both the native query and
        // post-filter in one place.
        return root.FindAll(this);
    }
}
