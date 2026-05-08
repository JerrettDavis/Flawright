namespace JerrettDavis.Flawright.CloseBehaviors;

/// <summary>
/// Runs a sequence of behaviors in order. The first one to return
/// <see langword="true"/> stops the chain. Useful for fallbacks like
/// "try dismissing a dialog, fall back to force-kill if it doesn't work".
/// </summary>
public sealed class CompositeCloseBehavior : ICloseBehavior
{
    private readonly IReadOnlyList<ICloseBehavior> _behaviors;

    /// <summary>
    /// Initialises a composite behavior that runs the given behaviors in order.
    /// </summary>
    /// <param name="behaviors">The behaviors to execute, in order.</param>
    public CompositeCloseBehavior(params ICloseBehavior[] behaviors)
    {
        _behaviors = behaviors ?? [];
    }

    /// <inheritdoc/>
    public async Task<bool> CloseAsync(ICloseContext context)
    {
        foreach (var behavior in _behaviors)
        {
            var result = await behavior.CloseAsync(context).ConfigureAwait(false);
            if (result)
                return true;
        }

        return false;
    }
}
