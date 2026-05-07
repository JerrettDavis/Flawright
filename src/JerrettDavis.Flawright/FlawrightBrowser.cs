using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace JerrettDavis.Flawright;

/// <summary>
/// Represents a launched or attached desktop application.
/// Obtain instances only via <see cref="Flawright.LaunchAsync"/> or
/// <see cref="Flawright.AttachAsync"/>; do not instantiate directly.
/// </summary>
/// <example>
/// <code>
/// await using var fw = await Flawright.LaunchAsync(
///     new LaunchOptions { ApplicationPath = "calc.exe" });
/// var page = await fw.Browser.NewPageAsync();
/// </code>
/// </example>
public sealed class FlawrightBrowser : IFlawrightBrowser
{
    private readonly LaunchOptions? _launchOptions;
    private readonly AttachOptions? _attachOptions;
    private readonly FlawrightOptions _fwOptions;

    private Application? _app;
    private UIA3Automation? _automation;
    private bool _disposed;

    internal FlawrightBrowser(LaunchOptions options, FlawrightOptions fwOptions)
    {
        _launchOptions = options;
        _fwOptions = fwOptions;
    }

    internal FlawrightBrowser(AttachOptions options, FlawrightOptions fwOptions)
    {
        _attachOptions = options;
        _fwOptions = fwOptions;
    }

    /// <summary>
    /// Returns a page for the application's main (first) window.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="FlawrightPage"/> representing the main window.</returns>
    public async Task<IFlawrightPage> NewPageAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var window = await Task.Run(
            () => _app!.GetMainWindow(_automation!),
            ct).ConfigureAwait(false);
        return new FlawrightPage(window, _automation!, _fwOptions);
    }

    /// <summary>
    /// Returns pages for all current top-level windows of the application.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A read-only list of <see cref="FlawrightPage"/> instances, one per
    /// top-level window.
    /// </returns>
    public async Task<IReadOnlyList<IFlawrightPage>> GetAllPagesAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var windows = await Task.Run(
            () => _app!.GetAllTopLevelWindows(_automation!),
            ct).ConfigureAwait(false);

        return windows
            .Select(w => (IFlawrightPage)new FlawrightPage(w, _automation!, _fwOptions))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Polls until a top-level window whose title contains
    /// <paramref name="titleOrPredicate"/> appears, then returns a page for it.
    /// </summary>
    /// <param name="titleOrPredicate">Window title substring to wait for.</param>
    /// <param name="timeout">
    /// Maximum wait time.  <see langword="null"/> uses
    /// <see cref="FlawrightOptions.DefaultTimeout"/>.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="FlawrightPage"/> for the matched window.</returns>
    /// <exception cref="FlawrightTimeoutException">
    /// Thrown when no matching window appears within the timeout.
    /// </exception>
    public async Task<IFlawrightPage> WaitForPageAsync(
        string titleOrPredicate,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        var deadline = DateTime.UtcNow + (timeout ?? _fwOptions.DefaultTimeout);
        var interval = _fwOptions.DefaultRetryInterval;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var windows = await Task.Run(
                () => _app!.GetAllTopLevelWindows(_automation!),
                ct).ConfigureAwait(false);

            var match = windows.FirstOrDefault(
                w => w.Title?.Contains(titleOrPredicate, StringComparison.OrdinalIgnoreCase) == true);

            if (match != null)
                return new FlawrightPage(match, _automation!, _fwOptions);

            await Task.Delay(interval, ct).ConfigureAwait(false);
        }

        throw new FlawrightTimeoutException(
            $"No window with title containing '{titleOrPredicate}' found within {timeout ?? _fwOptions.DefaultTimeout}.");
    }

    // ── IAsyncDisposable ────────────────────────────────────────────────────

    /// <summary>
    /// Closes the application and releases all resources.  Safe to call
    /// multiple times (idempotent).
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        await Task.Run(() =>
        {
            _automation?.Dispose();

            if (_app != null)
            {
#pragma warning disable CA1031 // Best-effort close; don't let exceptions escape DisposeAsync
                try { _app.Close(); }
                catch (Exception) { /* best-effort: window may already be gone */ }
#pragma warning restore CA1031

                if (!_app.HasExited)
                {
#pragma warning disable CA1031 // Best-effort kill; process may exit between HasExited check and Kill
                    try
                    {
                        var proc = System.Diagnostics.Process.GetProcessById(_app.ProcessId);
                        proc.Kill(entireProcessTree: true);
                    }
                    catch (Exception) { /* process may have already exited */ }
#pragma warning restore CA1031
                }

                _app.Dispose();
            }
        }).ConfigureAwait(false);
    }

    // ── internals ───────────────────────────────────────────────────────────

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_app != null)
            return;

        await Task.Run(() =>
        {
            if (_launchOptions != null)
            {
                var psi = new System.Diagnostics.ProcessStartInfo(_launchOptions.ApplicationPath)
                {
                    Arguments = string.Join(" ", _launchOptions.Arguments ?? [])
                };
                if (_launchOptions.WorkingDirectory != null)
                    psi.WorkingDirectory = _launchOptions.WorkingDirectory;

                _app = Application.AttachOrLaunch(psi);
            }
            else if (_attachOptions != null)
            {
                _app = Application.Attach(_attachOptions.ProcessId);
            }

            _automation = new UIA3Automation();
        }, ct).ConfigureAwait(false);
    }

    internal Application App =>
        _app ?? throw new InvalidOperationException("Browser not yet initialised. Call NewPageAsync() first.");

    internal UIA3Automation Automation =>
        _automation ?? throw new InvalidOperationException("Browser not yet initialised. Call NewPageAsync() first.");

    internal FlawrightOptions FwOptions => _fwOptions;
}
