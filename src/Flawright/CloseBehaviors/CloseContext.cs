using Flawright.Backends;

namespace Flawright.CloseBehaviors;

/// <summary>
/// Internal implementation of <see cref="ICloseContext"/> that wraps an
/// <see cref="IApplicationHandle"/> and the owning <see cref="IFlawrightBrowser"/>.
/// </summary>
internal sealed class CloseContext : ICloseContext
{
    private readonly IApplicationHandle _app;
    private readonly IInputBackend _input;

    internal CloseContext(
        IApplicationHandle app,
        IFlawrightBrowser browser,
        IInputBackend input,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        _app = app;
        _input = input;
        Browser = browser;
        Timeout = timeout;
        CancellationToken = cancellationToken;
    }

    /// <inheritdoc/>
    public IFlawrightBrowser Browser { get; }

    /// <inheritdoc/>
    public TimeSpan Timeout { get; }

    /// <inheritdoc/>
    public CancellationToken CancellationToken { get; }

    /// <inheritdoc/>
    public bool HasExited => _app.HasExited;

    /// <inheritdoc/>
    public void SendCloseSignal()
    {
#pragma warning disable CA1031
        try { _app.Close(); }
        catch (Exception) { /* best-effort: window may already be gone */ }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    public async Task<bool> WaitForExitAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline && !_app.HasExited)
            await Task.Delay(100, CancellationToken).ConfigureAwait(false);

        return _app.HasExited;
    }

    /// <inheritdoc/>
    public Task<IFlawrightElement?> FindButtonAsync(string buttonName)
    {
#pragma warning disable CA1031
        IElementBackend? backend;
        try { backend = _app.FindButtonByName(buttonName); }
        catch (Exception) { backend = null; }
#pragma warning restore CA1031

        if (backend == null)
            return Task.FromResult<IFlawrightElement?>(null);

        IFlawrightElement element = new FlawrightElement(backend, _input);
        return Task.FromResult<IFlawrightElement?>(element);
    }

    /// <inheritdoc/>
    public void Kill()
    {
        if (!_app.IsStoreApp)
        {
#pragma warning disable CA1031
            try { _app.KillProcessTree(); }
            catch (Exception) { /* process may have already exited */ }
#pragma warning restore CA1031
        }
    }
}
