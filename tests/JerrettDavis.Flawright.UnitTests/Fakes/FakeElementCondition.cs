using JerrettDavis.Flawright.Backends;

namespace JerrettDavis.Flawright.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="IElementCondition"/> backed by a predicate.
/// Used in unit tests instead of a FlaUI-backed condition.
/// </summary>
internal sealed class FakeElementCondition : IElementCondition
{
    private readonly Func<IElementBackend, bool> _predicate;

    /// <summary>
    /// Initialises a condition that matches backends satisfying <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    public FakeElementCondition(Func<IElementBackend, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _predicate = predicate;
    }

    /// <summary>A condition that matches all elements.</summary>
    public static FakeElementCondition All { get; } = new(_ => true);

    /// <summary>A condition that matches no elements.</summary>
    public static FakeElementCondition None { get; } = new(_ => false);

    /// <summary>Returns whether this condition matches <paramref name="backend"/>.</summary>
    /// <param name="backend">The backend to test.</param>
    public bool Matches(IElementBackend backend) => _predicate(backend);

    /// <inheritdoc/>
    public IEnumerable<IElementBackend> FindAllFrom(IElementBackend root)
    {
        return root.FindAll(this);
    }
}
