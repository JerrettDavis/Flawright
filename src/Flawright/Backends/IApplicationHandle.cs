namespace Flawright.Backends;

/// <summary>
/// Abstracts a launched or attached desktop application handle.
/// The sole production implementation is <c>FlaUiApplicationHandle</c>.
/// Tests use <c>FakeApplicationHandle</c>.
/// </summary>
internal interface IApplicationHandle : IDisposable
{
    /// <summary>Gets the OS process ID of the application.</summary>
    int ProcessId { get; }

    /// <summary>Gets whether the application process has exited.</summary>
    bool HasExited { get; }

    /// <summary>
    /// Gets whether the application was launched as a store (UWP/packaged) app.
    /// When <see langword="true"/>, <c>DisposeAsync</c> must not call
    /// <c>KillProcessTree</c>.
    /// </summary>
    bool IsStoreApp { get; }

    /// <summary>
    /// Blocks until the main window handle appears or the timeout elapses.
    /// </summary>
    /// <param name="timeout">Maximum duration to wait.</param>
    /// <returns><see langword="true"/> if the handle appeared; <see langword="false"/> on timeout.</returns>
    bool WaitWhileMainHandleIsMissing(TimeSpan timeout);

    /// <summary>Sends a close signal to the application (graceful).</summary>
    void Close();

    /// <summary>Kills the entire process tree (forceful, non-graceful).</summary>
    void KillProcessTree();

    /// <summary>Returns an element backend for the application's main window.</summary>
    IElementBackend GetMainWindow();

    /// <summary>Returns element backends for all current top-level windows.</summary>
    IReadOnlyList<IElementBackend> GetAllTopLevelWindows();

    /// <summary>
    /// Returns visible top-level windows in this process, excluding the window
    /// identified by <paramref name="ownerWindowHandle"/>.
    /// Used to discover dialogs, popups, and floating tool windows.
    /// </summary>
    /// <param name="ownerWindowHandle">
    /// The HWND of the caller's own window, which is excluded from the result.
    /// </param>
    /// <remarks>
    /// Enumeration uses Win32 EnumWindows directly because FlaUI's
    /// Application.GetAllTopLevelWindows does not surface owned dialogs spawned by
    /// frameworks like WPF Window.ShowDialog.
    ///
    /// Each returned HWND is resolved to a UIA AutomationElement via the automation's
    /// FromHandle method. Handles that fail to resolve (e.g., the window closed between
    /// enumeration and resolution) are silently skipped.
    ///
    /// Apps that legitimately host multiple independent top-level windows in one process
    /// will see all of them returned. Filter at the call site for stricter scoping.
    /// </remarks>
    IReadOnlyList<IElementBackend> GetOwnedWindows(nint ownerWindowHandle);

    /// <summary>
    /// Searches all current top-level windows for a button descendant whose UIA Name
    /// exactly matches <paramref name="buttonName"/>.  Returns the first match, or
    /// <see langword="null"/> if no such button exists.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="CloseBehaviors.ICloseContext.FindButtonAsync"/> (via
    /// <see cref="CloseBehaviors.CloseContext"/>) to locate dialog buttons without
    /// introducing new backend abstractions.
    /// </remarks>
    /// <param name="buttonName">The exact UIA Name of the button to find.</param>
    IElementBackend? FindButtonByName(string buttonName);
}
