using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace JerrettDavis.Flawright;

/// <summary>
/// Represents a launched or attached desktop application.
/// </summary>
public sealed class FlawrightBrowser : IAsyncDisposable
{
    private readonly LaunchOptions? _launchOptions;
    private readonly AttachOptions? _attachOptions;
    private Application? _app;
    private UIA3Automation? _automation;

    internal FlawrightBrowser(LaunchOptions options)
    {
        _launchOptions = options;
    }

    internal FlawrightBrowser(AttachOptions options)
    {
        _attachOptions = options;
    }

    public async Task<FlawrightPage> NewPageAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var window = _app!.GetMainWindow(_automation!);
        return new FlawrightPage(window, _automation!, this);
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            if (_launchOptions != null)
            {
                _app = Application.AttachOrLaunch(
                    new System.Diagnostics.ProcessStartInfo(_launchOptions.ApplicationPath)
                    {
                        Arguments = string.Join(" ", _launchOptions.Arguments ?? [])
                    });
            }
            else if (_attachOptions != null)
            {
                _app = Application.Attach(_attachOptions.ProcessId);
            }

            _automation = new UIA3Automation();
        }, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Run(() =>
        {
            _automation?.Dispose();
            _app?.Close();
            _app?.Dispose();
        });
    }

    internal Application App => _app ?? throw new InvalidOperationException("Browser not initialized");
    internal UIA3Automation Automation => _automation ?? throw new InvalidOperationException("Browser not initialized");
}