namespace Flawright;

#pragma warning disable CA1711 // EventArgs suffix is the standard naming convention for event argument classes

/// <summary>
/// Raised when a dialog window owned by a page's window is first detected.
/// </summary>
/// <remarks>
/// <para>
/// This event fires from <see cref="IFlawrightPage.WaitForDialogAsync"/> when a
/// matching owned window appears, and from <see cref="IFlawrightPage.GetOwnedWindowsAsync"/>
/// for each newly-discovered owned window when
/// <see cref="FlawrightOptions.EnableWindowEvents"/> is <see langword="true"/>.
/// </para>
/// <para>
/// <b>Thread safety:</b> Handlers are invoked synchronously on the thread that
/// triggered the dialog detection.  Misbehaving handlers must not block or raise
/// unhandled exceptions, as handler exceptions are swallowed and not propagated
/// to the caller.
/// </para>
/// </remarks>
public sealed class DialogOpenedEventArgs : EventArgs
{
    /// <summary>The OS process ID of the application that owns the dialog.</summary>
    public int ParentProcessId { get; }

    /// <summary>The native window handle (HWND) of the parent (owner) window.</summary>
    public nint ParentWindowHandle { get; }

    /// <summary>The native window handle (HWND) of the dialog window.</summary>
    public nint DialogWindowHandle { get; }

    /// <summary>The title of the dialog window, or <see langword="null"/> if unavailable.</summary>
    public string? DialogTitle { get; }

    /// <summary>
    /// <see langword="true"/> if the dialog was identified as modal via UIA WindowPattern;
    /// <see langword="false"/> if it is a non-modal owned window.
    /// </summary>
    public bool IsModal { get; }

    /// <summary>Initializes a new instance of <see cref="DialogOpenedEventArgs"/>.</summary>
    public DialogOpenedEventArgs(
        int parentProcessId,
        nint parentWindowHandle,
        nint dialogWindowHandle,
        string? dialogTitle,
        bool isModal)
    {
        ParentProcessId = parentProcessId;
        ParentWindowHandle = parentWindowHandle;
        DialogWindowHandle = dialogWindowHandle;
        DialogTitle = dialogTitle;
        IsModal = isModal;
    }
}

#pragma warning restore CA1711
