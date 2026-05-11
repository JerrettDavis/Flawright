using Flawright.Backends;
using Flawright.CloseBehaviors;
using Flawright.Internals;

namespace Flawright;

/// <summary>
/// Represents a launched or attached desktop application.
/// Obtain instances only via <see cref="Flawright.LaunchAsync(LaunchOptions, FlawrightOptions?, CancellationToken)"/> or
/// <see cref="Flawright.AttachAsync(AttachOptions, FlawrightOptions?, CancellationToken)"/>; do not instantiate directly.
/// </summary>
/// <example>
/// <code>
/// await using var fw = await Flawright.LaunchAsync(
///     new LaunchOptions { ApplicationPath = "calc.exe" });
/// var page = await fw.Browser.NewPageAsync();
/// </code>
/// </example>
internal sealed class FlawrightBrowser : IFlawrightBrowser, IAsyncDisposable
{
    private readonly IApplicationLauncher _launcher;
    private readonly IInputBackend _input;
    private readonly IConditionTranslator _translator;
    private readonly LaunchOptions? _launchOptions;
    private readonly AttachOptions? _attachOptions;
    private readonly FlawrightOptions _opts;
    private readonly bool _wasAttached;

    private IApplicationHandle? _app;
    private bool _disposed;
    private bool _closeAlreadyHandled;

    // ── Events ────────────────────────────────────────────────────────────────

    public event EventHandler<AppExecutionAliasResolvedEventArgs>? AppExecutionAliasResolved;
    public event EventHandler<ApplicationLaunchedEventArgs>? ApplicationLaunched;
    public event EventHandler<ProcessReadyGuardWaitedEventArgs>? ProcessReadyGuardWaited;
    public event EventHandler<ProcessAttachRetriedEventArgs>? ProcessAttachRetried;
    public event EventHandler<ApplicationClosingEventArgs>? ApplicationClosing;
    public event EventHandler<ApplicationClosedEventArgs>? ApplicationClosed;
    public event EventHandler<WindowDetectedEventArgs>? WindowDetected;

    internal FlawrightBrowser(
        IApplicationLauncher launcher,
        IInputBackend input,
        IConditionTranslator translator,
        LaunchOptions launchOptions,
        FlawrightOptions opts)
    {
        _launcher = launcher;
        _input = input;
        _translator = translator;
        _launchOptions = launchOptions;
        _opts = opts;
        _wasAttached = false;
    }

    internal FlawrightBrowser(
        IApplicationLauncher launcher,
        IInputBackend input,
        IConditionTranslator translator,
        AttachOptions attachOptions,
        FlawrightOptions opts)
    {
        _launcher = launcher;
        _input = input;
        _translator = translator;
        _attachOptions = attachOptions;
        _opts = opts;
        _wasAttached = true;
    }

    // ── IFlawrightBrowser ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IFlawrightPage> NewPageAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var windowBackend = _app!.GetMainWindow();
        return new FlawrightPage(windowBackend, _input, _opts, _translator);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IFlawrightPage>> GetAllPagesAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        return _app!.GetAllTopLevelWindows()
            .Select(w => (IFlawrightPage)new FlawrightPage(w, _input, _opts, _translator))
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IFlawrightPage> WaitForPageAsync(
        string title,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var t = timeout ?? _opts.DefaultTimeout;
        var match = await AutoWait.UntilAsync(
            _ => Task.FromResult<IElementBackend?>(
                _app!.GetAllTopLevelWindows()
                    .FirstOrDefault(w => w.Name?.Contains(title, StringComparison.OrdinalIgnoreCase) == true)),
            $"window with title containing '{title}'",
            t,
            _opts.DefaultRetryInterval,
            ct).ConfigureAwait(false);
        return new FlawrightPage(match, _input, _opts, _translator);
    }

    /// <inheritdoc/>
    public async Task<bool> CloseAsync(TimeSpan? timeout = null)
    {
        // Idempotent: calling twice is a no-op after the first call succeeds.
        if (_closeAlreadyHandled)
            return true;

        _closeAlreadyHandled = true;

        if (_app == null)
            return true;

        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(5);

        RaiseEvent(ApplicationClosing, new ApplicationClosingEventArgs(
            CloseBehaviorName: _opts.CloseBehavior.GetType().Name,
            Timeout: effectiveTimeout,
            ProcessId: _app.ProcessId));

        var context = new CloseContext(_app, this, _input, effectiveTimeout, CancellationToken.None);

        var graceful = await _opts.CloseBehavior.CloseAsync(context).ConfigureAwait(false);

        // Safety net: if the behavior returned false (app still running), force-kill —
        // unless we attached to the process (we do not own its lifecycle).
        if (!graceful && !_app.HasExited && !_app.IsStoreApp && !_wasAttached)
        {
#pragma warning disable CA1031
            try { _app.KillProcessTree(); }
            catch (Exception) { /* process may have already exited */ }
#pragma warning restore CA1031
        }

        RaiseEvent(ApplicationClosed, new ApplicationClosedEventArgs(
            CloseBehaviorName: _opts.CloseBehavior.GetType().Name,
            Graceful: graceful,
            Timeout: effectiveTimeout,
            ProcessId: _app.ProcessId,
            ExitedCleanly: _app.HasExited));

        return graceful;
    }

    // ── IAsyncDisposable ──────────────────────────────────────────────────────

    /// <summary>
    /// Closes the application and releases all resources.  Safe to call
    /// multiple times (idempotent).
    /// </summary>
    /// <remarks>
    /// When the browser was created via <see cref="Flawright.AttachAsync(AttachOptions, FlawrightOptions?, CancellationToken)"/>,
    /// the attached process will not be terminated on dispose — only framework
    /// resources are released. This preserves external process ownership.
    /// <para>
    /// When <see cref="CloseAsync"/> has already been called, this method only
    /// releases handles — it does not re-run the close/kill loop.  This preserves
    /// backward-compatible behavior: callers who rely on <c>await using</c> without
    /// calling <see cref="CloseAsync"/> first will still see force-kill after 2 s.
    /// DisposeAsync does NOT invoke the configured <see cref="FlawrightOptions.CloseBehavior"/>
    /// — it is a force-kill safety net. Call <see cref="CloseAsync"/> first for
    /// graceful, behavior-driven teardown.
    /// </para>
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_app == null)
            return;

        // For attached processes, skip close/kill and only release framework resources.
        if (_wasAttached)
        {
#pragma warning disable CA1031
            try { _app.Dispose(); }
            catch (Exception) { /* best-effort */ }
#pragma warning restore CA1031
            _app = null;
            return;
        }

        // Only run close/kill if CloseAsync has not already handled teardown.
        if (!_closeAlreadyHandled)
        {
            // Backward-compatible dispose path: close signal + 2 s wait + force kill.
            // DisposeAsync deliberately does NOT invoke the configured CloseBehavior —
            // it is a "force-kill safety net", not a graceful close. Callers who want
            // graceful dialog-handling should call CloseAsync() explicitly first.
#pragma warning disable CA1031
            try { _app.Close(); }
            catch (Exception) { /* best-effort: window may already be gone */ }
#pragma warning restore CA1031

            // Wait briefly for clean exit (up to 2 seconds, 100 ms intervals).
            for (var i = 0; i < 20 && !_app.HasExited; i++)
                await Task.Delay(100).ConfigureAwait(false);

            if (!_app.HasExited && !_app.IsStoreApp)
            {
                // For store apps we never KillProcessTree — it could cascade-kill
                // ApplicationFrameHost.exe and bring down other apps.
#pragma warning disable CA1031
                try { _app.KillProcessTree(); }
                catch (Exception) { /* process may have already exited */ }
#pragma warning restore CA1031
            }
        }

#pragma warning disable CA1031
        try { _app.Dispose(); }
        catch (Exception) { /* best-effort */ }
#pragma warning restore CA1031

        _app = null;
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Raises an event, swallowing any exceptions thrown by handlers.
    /// </summary>
    /// <remarks>
    /// Handler exceptions are swallowed so that misbehaving event handlers
    /// cannot crash the browser or abort its operations. Exceptions are logged
    /// to the debug output so developers can see them during troubleshooting.
    /// </remarks>
    private void RaiseEvent<T>(EventHandler<T>? handler, T args) where T : EventArgs
    {
        if (handler == null)
            return;

        foreach (var d in handler.GetInvocationList())
        {
            try
            {
                ((EventHandler<T>)d)(this, args);
            }
#pragma warning disable CA1031 // Handler exceptions must not crash the browser; intentionally swallowed.
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Flawright] Event handler threw: {ex}");
            }
#pragma warning restore CA1031
        }
    }

    /// <summary>
    /// Initialises the application handle on first use.  Idempotent — subsequent
    /// calls return immediately without re-launching or re-attaching.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the supplied options are invalid (both/neither path and AUMID set, etc.).
    /// </exception>
    /// <exception cref="FlawrightTimeoutException">
    /// Thrown when the application's main window does not appear within
    /// <see cref="LaunchOptions.StartupTimeout"/> (or 30 seconds by default).
    /// </exception>
    internal async Task EnsureInitializedAsync(CancellationToken ct = default)
    {
        if (_app != null)
            return;

        _app = _launchOptions != null
            ? await LaunchAppAsync(_launchOptions, ct).ConfigureAwait(false)
            : await AttachAppAsync(_attachOptions!, ct).ConfigureAwait(false);

        RaiseEvent(ApplicationLaunched, new ApplicationLaunchedEventArgs(
            ProcessId: _app.ProcessId,
            ExecutablePath: _launchOptions?.ApplicationPath,
            Aumid: _launchOptions?.Aumid,
            WasAttached: _wasAttached,
            IsPackagedApp: _app.IsStoreApp,
            Timestamp: DateTimeOffset.UtcNow));

        var startupTimeout = _launchOptions?.StartupTimeout ?? TimeSpan.FromSeconds(30);

        // WaitWhileMainHandleIsMissing is a blocking call; run it off the thread-pool
        // to avoid starving the caller's synchronisation context.
        var appeared = await Task.Run(
            () => _app.WaitWhileMainHandleIsMissing(startupTimeout), ct).ConfigureAwait(false);

        if (!appeared)
            throw new FlawrightTimeoutException(
                $"Application main window did not appear within {startupTimeout}.");
    }

    private async Task<IApplicationHandle> LaunchAppAsync(LaunchOptions lo, CancellationToken ct)
    {
        var hasPath = !string.IsNullOrWhiteSpace(lo.ApplicationPath);
        var hasAumid = !string.IsNullOrWhiteSpace(lo.Aumid);

        if (hasPath == hasAumid)
            throw new ArgumentException(
                "LaunchOptions: exactly one of ApplicationPath or Aumid must be set.",
                nameof(lo));

        if (hasAumid && !string.IsNullOrEmpty(lo.WorkingDirectory))
            throw new ArgumentException(
                "WorkingDirectory is not supported for AUMID launches.",
                nameof(lo));

        var args = lo.Arguments == null ? "" : string.Join(' ', lo.Arguments);

        return hasAumid
            ? await _launcher.LaunchStoreApp(lo.Aumid!, args, ct).ConfigureAwait(false)
            : await _launcher.Launch(lo, ct).ConfigureAwait(false);
    }

    private async Task<IApplicationHandle> AttachAppAsync(AttachOptions ao, CancellationToken ct)
    {
        var hasPid = ao.ProcessId.HasValue;
        var hasName = !string.IsNullOrWhiteSpace(ao.ProcessName);

        if (hasPid == hasName)
            throw new ArgumentException(
                "AttachOptions: exactly one of ProcessId or ProcessName must be set.",
                nameof(ao));

        return hasPid
            ? await _launcher.Attach(ao.ProcessId!.Value, ct).ConfigureAwait(false)
            : await _launcher.AttachByName(StripExeSuffix(ao.ProcessName!), ao.Index, ct).ConfigureAwait(false);
    }

    private static string StripExeSuffix(string name)
        => name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
}
