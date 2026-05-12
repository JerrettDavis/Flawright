namespace Flawright;

/// <summary>
/// Events raised by <see cref="IFlawrightBrowser"/> to provide visibility
/// into Flawright's automatic behaviors (alias resolution, process launch,
/// close, window detection, etc.).
/// </summary>
/// <remarks>
/// <para>
/// Handlers are invoked synchronously on the thread that triggered the event.
/// Misbehaving handlers (those that block or raise unhandled exceptions) will
/// have their exceptions swallowed and will not propagate to the caller.
/// See each event's documentation for specific handler safety guarantees.
/// </para>
/// </remarks>
public interface IFlawrightBrowserEvents
{
    /// <summary>
    /// Raised when an AppExecutionAlias stub or system shell-launcher shim
    /// is transparently redirected to its packaged-app AUMID.
    /// </summary>
    event EventHandler<AppExecutionAliasResolvedEventArgs>? AppExecutionAliasResolved;

    /// <summary>
    /// Raised after an application has been launched or attached to and its
    /// process handle is available for inspection.
    /// </summary>
    event EventHandler<ApplicationLaunchedEventArgs>? ApplicationLaunched;

    /// <summary>
    /// Raised when <see cref="Internals.ProcessReadyGuard.WaitForProcessReady"/>
    /// was invoked and encountered non-trivial retries due to DLL module loading.
    /// </summary>
    event EventHandler<ProcessReadyGuardWaitedEventArgs>? ProcessReadyGuardWaited;

    /// <summary>
    /// Raised when <see cref="Internals.ProcessAttachRetry"/> retries a failed
    /// attach operation due to a transient Win32 error.
    /// </summary>
    event EventHandler<ProcessAttachRetriedEventArgs>? ProcessAttachRetried;

    /// <summary>
    /// Raised at the start of <see cref="IFlawrightBrowser.CloseAsync"/>,
    /// before the configured close behavior is invoked.
    /// </summary>
    event EventHandler<ApplicationClosingEventArgs>? ApplicationClosing;

    /// <summary>
    /// Raised at the end of <see cref="IFlawrightBrowser.CloseAsync"/>,
    /// after the close behavior has run and the process has exited or been killed.
    /// </summary>
    event EventHandler<ApplicationClosedEventArgs>? ApplicationClosed;

    /// <summary>
    /// Raised when a top-level window is detected by the application.
    /// Only raised when <see cref="FlawrightOptions.EnableWindowEvents"/> is
    /// <see langword="true"/>.
    /// </summary>
    event EventHandler<WindowDetectedEventArgs>? WindowDetected;

    /// <summary>
    /// Raised when a dialog window owned by a page's window is first detected.
    /// Fires from <see cref="IFlawrightPage.WaitForDialogAsync"/>,
    /// <see cref="IFlawrightPage.GetOwnedWindowsAsync"/>, and
    /// <see cref="IFlawrightPage.GetModalWindowsAsync"/> for newly-discovered windows.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="WindowDetected"/>, this event does NOT require
    /// <see cref="FlawrightOptions.EnableWindowEvents"/> to be <see langword="true"/>.
    /// The <c>EnableWindowEvents</c> flag gates only the noisy <see cref="WindowDetected"/>
    /// firehose; <see cref="DialogOpened"/> is a focused, opt-in-by-subscription event that
    /// fires whenever a new dialog is detected.
    /// Each unique dialog handle fires at most once per <see cref="IFlawrightPage"/> instance.
    /// </remarks>
    event EventHandler<DialogOpenedEventArgs>? DialogOpened;
}
