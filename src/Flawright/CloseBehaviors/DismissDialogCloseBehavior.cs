namespace Flawright.CloseBehaviors;

/// <summary>
/// Sends WM_CLOSE, then polls for a modal dialog containing a button with
/// one of the configured Names. Clicks the first match found, then waits
/// for the process to exit.
///
/// <para>The default button names are <c>"Don't Save"</c> (classic Win10 Notepad
/// and most Win32 apps) and <c>"Don't save"</c> (Win11 packaged Notepad).
/// Override the constructor parameter to target other apps — for example,
/// <c>new DismissDialogCloseBehavior("Discard")</c> for an app whose
/// discard button is labelled "Discard".</para>
/// </summary>
public sealed class DismissDialogCloseBehavior : ICloseBehavior
{
    /// <summary>The default discard-button names (Win10 + Win11 Notepad).</summary>
    public static readonly IReadOnlyList<string> DefaultDiscardButtonNames =
        new[] { "Don't Save", "Don't save" };

    /// <summary>
    /// Initialises the behavior with explicit button names.
    /// When no names are provided the <see cref="DefaultDiscardButtonNames"/> are used.
    /// </summary>
    /// <param name="buttonNames">
    /// The button Names to try, in priority order. Pass one or more names to override
    /// the defaults; pass none to use <see cref="DefaultDiscardButtonNames"/>.
    /// </param>
    public DismissDialogCloseBehavior(params string[] buttonNames)
    {
        ButtonNames = buttonNames is { Length: > 0 }
            ? buttonNames.ToArray()
            : DefaultDiscardButtonNames.ToArray();
    }

    /// <summary>The button Names to try, in priority order.</summary>
    public IReadOnlyList<string> ButtonNames { get; }

    /// <summary>
    /// How long to poll for the dialog to appear before giving up and waiting
    /// for the WM_CLOSE alone to exit the process. Defaults to 2 seconds.
    /// </summary>
    public TimeSpan DialogPollTimeout { get; init; } = TimeSpan.FromSeconds(2);

    /// <inheritdoc/>
    public async Task<bool> CloseAsync(ICloseContext context)
    {
        context.SendCloseSignal();

        // Poll up to DialogPollTimeout for any of the named buttons to appear.
        var dialogPollDeadline = DateTime.UtcNow.Add(DialogPollTimeout);
        while (DateTime.UtcNow < dialogPollDeadline && !context.HasExited)
        {
            foreach (var name in ButtonNames)
            {
                var button = await context.FindButtonAsync(name).ConfigureAwait(false);
                if (button != null)
                {
#pragma warning disable CA1031
                    try { await button.ClickAsync().ConfigureAwait(false); }
                    catch (Exception) { /* best-effort */ }
#pragma warning restore CA1031
                    goto dialogDismissed;
                }
            }

            await Task.Delay(100).ConfigureAwait(false);
        }

    dialogDismissed:
        return await context.WaitForExitAsync(context.Timeout).ConfigureAwait(false);
    }
}
