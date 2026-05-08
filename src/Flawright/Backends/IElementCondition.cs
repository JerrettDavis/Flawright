namespace Flawright.Backends;

/// <summary>
/// Opaque condition abstraction that both carries a backend-native query
/// (e.g. a FlaUI <c>ConditionBase</c>) and an optional in-memory post-filter.
///
/// <see cref="IElementBackend.FindAll"/> implementations cast the condition to
/// their concrete type, run the native query, then apply the post-filter.
/// </summary>
public interface IElementCondition
{
    /// <summary>
    /// Finds all elements reachable from <paramref name="root"/> that satisfy
    /// this condition's native query and optional post-filter predicate.
    /// </summary>
    /// <param name="root">The backend element to search from.</param>
    /// <returns>All matching descendant backends.</returns>
    IEnumerable<IElementBackend> FindAllFrom(IElementBackend root);
}
