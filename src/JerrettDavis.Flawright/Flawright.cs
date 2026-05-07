namespace JerrettDavis.Flawright;

public sealed class Flawright : IAsyncDisposable
{
    private readonly List<FlawrightBrowser> _browsers = [];

    private Flawright() { }

    public static Task<Flawright> CreateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Flawright());
    }

    public Task<FlawrightBrowser> LaunchAsync(LaunchOptions options, CancellationToken cancellationToken = default)
    {
        var browser = new FlawrightBrowser(options);
        _browsers.Add(browser);
        return Task.FromResult(browser);
    }

    public Task<FlawrightBrowser> AttachAsync(AttachOptions options, CancellationToken cancellationToken = default)
    {
        var browser = new FlawrightBrowser(options);
        _browsers.Add(browser);
        return Task.FromResult(browser);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var browser in _browsers)
        {
            await browser.DisposeAsync();
        }
        _browsers.Clear();
    }
}

public sealed record LaunchOptions
{
    public string ApplicationPath { get; init; } = null!;
    public string[]? Arguments { get; init; }
    public string? WorkingDirectory { get; init; }
    public bool? ForceAsyncio { get; init; }
}

public sealed record AttachOptions
{
    public int ProcessId { get; init; }
}