namespace JerrettDavis.Flawright.CloseBehaviors;

/// <summary>
/// Force-kills the application's process tree. Use when you don't care about
/// graceful shutdown — fastest path, but skips any save-changes prompts and
/// can leave dirty temp files. Always returns <see langword="true"/>.
/// </summary>
public sealed class KillCloseBehavior : ICloseBehavior
{
    /// <inheritdoc/>
    public Task<bool> CloseAsync(ICloseContext context)
    {
        context.Kill();
        return Task.FromResult(true);
    }
}
