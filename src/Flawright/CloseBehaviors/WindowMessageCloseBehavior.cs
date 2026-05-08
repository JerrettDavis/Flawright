namespace Flawright.CloseBehaviors;

/// <summary>
/// Sends WM_CLOSE to the main window and waits for the process to exit.
/// Does not handle modal dialogs. Returns false if the process is still
/// running when the timeout elapses, signaling the caller to force-kill.
/// </summary>
public sealed class WindowMessageCloseBehavior : ICloseBehavior
{
    /// <inheritdoc/>
    public async Task<bool> CloseAsync(ICloseContext context)
    {
        context.SendCloseSignal();
        return await context.WaitForExitAsync(context.Timeout).ConfigureAwait(false);
    }
}
