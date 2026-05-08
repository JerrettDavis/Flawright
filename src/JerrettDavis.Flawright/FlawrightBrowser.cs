using JerrettDavis.Flawright.Backends;
using JerrettDavis.Flawright.Internals;

namespace JerrettDavis.Flawright;

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

    private IApplicationHandle? _app;
    private bool _disposed;

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

    // ── IAsyncDisposable ──────────────────────────────────────────────────────

    /// <summary>
    /// Closes the application and releases all resources.  Safe to call
    /// multiple times (idempotent).
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_app == null)
            return;

#pragma warning disable CA1031 // Best-effort close; don't let exceptions escape DisposeAsync
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

#pragma warning disable CA1031
        try { _app.Dispose(); }
        catch (Exception) { /* best-effort */ }
#pragma warning restore CA1031

        _app = null;
    }

    // ── Internals ─────────────────────────────────────────────────────────────

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
            ? LaunchApp(_launchOptions)
            : AttachApp(_attachOptions!);

        var startupTimeout = _launchOptions?.StartupTimeout ?? TimeSpan.FromSeconds(30);

        // WaitWhileMainHandleIsMissing is a blocking call; run it off the thread-pool
        // to avoid starving the caller's synchronisation context.
        var appeared = await Task.Run(
            () => _app.WaitWhileMainHandleIsMissing(startupTimeout), ct).ConfigureAwait(false);

        if (!appeared)
            throw new FlawrightTimeoutException(
                $"Application main window did not appear within {startupTimeout}.");
    }

    private IApplicationHandle LaunchApp(LaunchOptions lo)
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
            ? _launcher.LaunchStoreApp(lo.Aumid!, args)
            : _launcher.Launch(lo);
    }

    private IApplicationHandle AttachApp(AttachOptions ao)
    {
        var hasPid = ao.ProcessId.HasValue;
        var hasName = !string.IsNullOrWhiteSpace(ao.ProcessName);

        if (hasPid == hasName)
            throw new ArgumentException(
                "AttachOptions: exactly one of ProcessId or ProcessName must be set.",
                nameof(ao));

        return hasPid
            ? _launcher.Attach(ao.ProcessId!.Value)
            : _launcher.AttachByName(StripExeSuffix(ao.ProcessName!), ao.Index);
    }

    private static string StripExeSuffix(string name)
        => name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
}
