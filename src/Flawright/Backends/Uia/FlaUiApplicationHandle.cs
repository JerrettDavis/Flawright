using System.Diagnostics.CodeAnalysis;
using FlaUI.Core;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace Flawright.Backends.Uia;

/// <summary>
/// FlaUI-backed <see cref="IApplicationHandle"/> wrapping <see cref="Application"/>
/// and <see cref="UIA3Automation"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "FlaUI I/O; covered by E2E tests only.")]
internal sealed class FlaUiApplicationHandle : IApplicationHandle
{
    private readonly Application _app;
    private readonly UIA3Automation _automation;
    private bool _disposed;

    internal FlaUiApplicationHandle(Application app, UIA3Automation automation)
    {
        _app = app;
        _automation = automation;
    }

    /// <inheritdoc/>
    public int ProcessId => _app.ProcessId;

    /// <inheritdoc/>
    /// <remarks>
    /// Returns <see langword="true"/> when the underlying <see cref="System.Diagnostics.Process"/>
    /// handle has been disposed or detached (e.g. because the app-execution-alias stub
    /// exited immediately after handing off to the packaged app).  Callers on dispose
    /// paths rely on this never throwing.
    /// </remarks>
    public bool HasExited
    {
        get
        {
            try
            {
                return _app.HasExited;
            }
            catch (InvalidOperationException)
            {
                // Process handle was disposed or was never associated with a real
                // process (e.g. AppExecutionAlias stub that exited immediately).
                return true;
            }
#pragma warning disable CA1031 // Any other failure on the process handle = treat as exited
            catch (Exception)
            {
                return true;
            }
#pragma warning restore CA1031
        }
    }

    /// <inheritdoc/>
    public bool IsStoreApp => _app.IsStoreApp;

    /// <inheritdoc/>
    /// <remarks>
    /// For packaged (store) apps hosted in <c>ApplicationFrameHost.exe</c>, FlaUI's
    /// process-handle-based poll — <c>Process.MainWindowHandle != IntPtr.Zero</c> —
    /// never becomes true because the packaged-app process itself never owns the
    /// visible HWND.  In that case we use a UIA desktop walk: poll all top-level
    /// desktop windows for one that contains descendants belonging to our process ID,
    /// which indicates the hosting <c>ApplicationFrameHost</c> window is ready.
    /// </remarks>
    public bool WaitWhileMainHandleIsMissing(TimeSpan timeout)
    {
        if (!_app.IsStoreApp)
            return _app.WaitWhileMainHandleIsMissing(timeout);

        // Store / packaged app: ApplicationFrameHost hosts the real window.
        // Poll the UIA desktop tree for a top-level window that has descendants
        // belonging to our process.
        return WaitForPackagedAppWindowViaUia(timeout);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// For packaged (store) apps hosted in <c>ApplicationFrameHost.exe</c>, the
    /// "main window" from the automation perspective is the <c>ApplicationFrameHost</c>
    /// top-level element that contains descendants belonging to this process.
    /// FlaUI's default <c>GetMainWindow</c> (which uses <c>MainWindowHandle</c>) would
    /// return <see langword="null"/> in this case, so we use the same UIA desktop walk
    /// as <see cref="WaitWhileMainHandleIsMissing"/>.
    /// </remarks>
    public IElementBackend GetMainWindow()
    {
        if (_app.IsStoreApp)
        {
            var storeWindow = FindPackagedAppWindowViaUia();
            if (storeWindow != null)
                return storeWindow;

            throw new InvalidOperationException(
                "Application main window could not be found. " +
                "The packaged app may not be fully initialised yet — " +
                "ensure WaitWhileMainHandleIsMissing has returned true before calling GetMainWindow.");
        }

        var window = _app.GetMainWindow(_automation)
            ?? throw new InvalidOperationException("Application main window could not be found.");
        return new UiaElementBackend(window);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// For packaged (store) apps, the top-level UIA window element belongs to
    /// <c>ApplicationFrameHost.exe</c>, not to the packaged-app process.  We find
    /// those windows by searching the desktop for top-level windows that have
    /// descendants with this handle's <see cref="ProcessId"/>.
    /// </remarks>
    public IReadOnlyList<IElementBackend> GetAllTopLevelWindows()
    {
        if (_app.IsStoreApp)
            return GetPackagedAppTopLevelWindowsViaUia();

        var windows = _app.GetAllTopLevelWindows(_automation);
        return windows
            .Select(w => (IElementBackend)new UiaElementBackend(w))
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc/>
    public void Close() => _app.Close();

    /// <inheritdoc/>
    public void KillProcessTree()
    {
#pragma warning disable CA1031 // Best-effort kill
        try
        {
            var proc = System.Diagnostics.Process.GetProcessById(_app.ProcessId);
            proc.Kill(entireProcessTree: true);
        }
        catch (Exception)
        {
            // Process may have already exited
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Enumerates all top-level windows in this process and returns every window
    /// whose native handle is not <paramref name="ownerWindowHandle"/> itself.
    /// This mirrors the enumeration used by <see cref="FindButtonByName"/>, which
    /// reliably locates WPF <c>Window.ShowDialog</c> dialogs that the previous
    /// Win32-owner-chain filter missed.
    ///
    /// The intentionally permissive filter works because WPF (and WinForms / WinUI)
    /// do not always propagate <c>GWL_HWNDPARENT</c>, so strict <c>GW_OWNER</c> /
    /// <c>GA_ROOTOWNER</c> / style-bit checks all fail to find these dialogs.
    /// In practice an automated application opens dialogs in its own process, so
    /// "all top-levels minus self" matches the common case.
    ///
    /// Applications that host multiple independent top-level windows in a single
    /// process will see all of them returned; call sites should filter by title or
    /// window properties when stricter scoping is required.
    /// </remarks>
    public IReadOnlyList<IElementBackend> GetOwnedWindows(nint ownerWindowHandle)
    {
        if (ownerWindowHandle == IntPtr.Zero)
            return Array.Empty<IElementBackend>();

        var allWindows = GetAllTopLevelWindows();
        var result = new List<IElementBackend>();

        foreach (var w in allWindows)
        {
#pragma warning disable CA1031 // Tolerate failures for individual windows
            try
            {
                var hwnd = w.NativeWindowHandle;
                // Skip the owner itself, but include windows with a zero handle
                // (some UIA top-level elements for WPF/WinUI dialogs report
                //  NativeWindowHandle == 0; we still want to surface them).
                if (hwnd != IntPtr.Zero && hwnd == ownerWindowHandle)
                    continue;

                result.Add(w);
            }
            catch (Exception)
            {
                // Window may have been destroyed during enumeration — skip it.
            }
#pragma warning restore CA1031
        }

        return result.AsReadOnly();
    }

    /// <inheritdoc/>
    public IElementBackend? FindButtonByName(string buttonName)
    {
        var cf = _automation.ConditionFactory;
        var condition = cf.ByName(buttonName).And(cf.ByControlType(ControlType.Button));

        foreach (var window in GetAllTopLevelWindowElements())
        {
            var found = window.FindFirstDescendant(condition);
            if (found != null)
                return new UiaElementBackend(found);
        }

        return null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _automation.Dispose();
        _app.Dispose();
    }

    // ── Store-app UIA helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Polls the UIA desktop tree for a top-level window that contains descendants
    /// belonging to <see cref="ProcessId"/> until one appears or <paramref name="timeout"/>
    /// elapses.
    /// </summary>
    private bool WaitForPackagedAppWindowViaUia(TimeSpan timeout)
    {
        const int PollIntervalMs = 50;
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
#pragma warning disable CA1031 // Tolerate transient UIA failures during startup
            try
            {
                if (FindPackagedAppWindowViaUia() != null)
                    return true;
            }
            catch
            {
                // UIA may throw during app startup (e.g. COMException, ElementNotAvailableException).
                // Swallow and retry until deadline.
            }
#pragma warning restore CA1031

            Thread.Sleep(PollIntervalMs);
        }

        return false;
    }

    /// <summary>
    /// Searches the UIA desktop for the first top-level window that contains at
    /// least one descendant element belonging to <see cref="ProcessId"/>.
    /// Returns <see langword="null"/> if none is found.
    /// </summary>
    private UiaElementBackend? FindPackagedAppWindowViaUia()
    {
        var windows = FindPackagedAppWindowElementsViaUia();
        return windows.Count > 0 ? new UiaElementBackend(windows[0]) : null;
    }

    /// <summary>
    /// Searches the UIA desktop for all top-level windows that contain descendants
    /// belonging to <see cref="ProcessId"/>, returning them as
    /// <see cref="UiaElementBackend"/> wrappers.
    /// </summary>
    private List<IElementBackend> GetPackagedAppTopLevelWindowsViaUia()
    {
        var elements = FindPackagedAppWindowElementsViaUia();
        return elements
            .Select(e => (IElementBackend)new UiaElementBackend(e))
            .ToList();
    }

    /// <summary>
    /// Core UIA search: walks every top-level desktop window and returns those that
    /// contain at least one descendant element with <see cref="ProcessId"/> as the
    /// owning process.
    ///
    /// This is necessary for packaged apps hosted in <c>ApplicationFrameHost.exe</c>:
    /// the top-level UIA element belongs to the frame host process, but its UIA
    /// descendants are reported under the packaged-app process ID.  A direct
    /// <c>ByProcessId</c> search at the desktop level therefore misses these windows.
    /// </summary>
    private List<FlaUI.Core.AutomationElements.AutomationElement> FindPackagedAppWindowElementsViaUia()
    {
        var result = new List<FlaUI.Core.AutomationElements.AutomationElement>();
        var desktop = _automation.GetDesktop();
        var windowCondition = _automation.ConditionFactory.ByControlType(ControlType.Window);
        var topLevelWindows = desktop.FindAllChildren(windowCondition);

        foreach (var topWindow in topLevelWindows)
        {
#pragma warning disable CA1031 // Individual windows may be inaccessible; skip them
            try
            {
                // Quick check: if the window itself already belongs to our process, include it.
                if (topWindow.Properties.ProcessId.TryGetValue(out var wndProcId) &&
                    wndProcId == ProcessId)
                {
                    result.Add(topWindow);
                    continue;
                }

                // Slower check: search descendants for any element belonging to our process.
                // Limit to a fast first-match (ControlType.Window or Pane under the frame host).
                var descendantCond = _automation.ConditionFactory.ByProcessId(ProcessId);
                var match = topWindow.FindFirstDescendant(descendantCond);
                if (match != null)
                    result.Add(topWindow);
            }
            catch
            {
                // Window may have been destroyed or be inaccessible — skip it.
            }
#pragma warning restore CA1031
        }

        return result;
    }

    /// <summary>
    /// Returns all top-level window elements for the application, using the
    /// packaged-app search path when <see cref="IsStoreApp"/> is <see langword="true"/>.
    /// </summary>
    private IEnumerable<FlaUI.Core.AutomationElements.AutomationElement> GetAllTopLevelWindowElements()
    {
        if (_app.IsStoreApp)
            return FindPackagedAppWindowElementsViaUia();

        return _app.GetAllTopLevelWindows(_automation);
    }

}
