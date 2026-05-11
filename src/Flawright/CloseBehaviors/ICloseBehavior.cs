namespace Flawright.CloseBehaviors;

/// <summary>
/// Strategy for closing an application. Implement this to define custom
/// shutdown logic — for example, dismissing a "save changes?" dialog,
/// invoking a "Quit" menu item, or sending an application-specific shutdown
/// command. Built-in implementations live in this namespace.
/// </summary>
public interface ICloseBehavior
{
    /// <summary>
    /// Performs the close action. Return <see langword="true"/> when the
    /// application has exited (or is considered closed) so the caller can stop;
    /// return <see langword="false"/> to signal the caller should fall through
    /// to a force-kill.
    /// </summary>
    /// <param name="context">Context providing access to the application's page and timing.</param>
    Task<bool> CloseAsync(ICloseContext context);
}
