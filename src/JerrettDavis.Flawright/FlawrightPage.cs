using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace JerrettDavis.Flawright;

/// <summary>
/// Represents a window or form within a desktop application.
/// </summary>
public sealed class FlawrightPage : IAsyncDisposable
{
    private readonly Window _window;
    private readonly UIA3Automation _automation;
    private readonly FlawrightBrowser _browser;

    internal FlawrightPage(Window window, UIA3Automation automation, FlawrightBrowser browser)
    {
        _window = window;
        _automation = automation;
        _browser = browser;
    }

    public FlawrightLocator Locator(string selector) => new(selector, _window, _automation);

    public async Task ClickAsync(string selector, CancellationToken cancellationToken = default)
    {
        var locator = Locator(selector);
        var element = await locator.FirstAsync(cancellationToken);
        await element.ClickAsync(cancellationToken);
    }

    public async Task FillAsync(string selector, string text, CancellationToken cancellationToken = default)
    {
        var locator = Locator(selector);
        var element = await locator.FirstAsync(cancellationToken);
        await element.FillAsync(text, cancellationToken);
    }

    public async Task<byte[]> ScreenshotAsync(string? path = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (path != null)
            {
                _window.CaptureToFile(path);
            }
            using var bitmap = _window.Capture();
            using var img = new System.Drawing.Bitmap(bitmap);
            using var ms = new System.IO.MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
    }

    internal Window Window => _window;
    internal UIA3Automation Automation => _automation;
}