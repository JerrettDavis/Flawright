using Flawright;
using Flawright.CloseBehaviors;
using Flawright.InputModes;
using Flawright.Locator;
using Xunit;

namespace Flawright.E2ETests;

/// <summary>
/// E2E coverage for the on-disk screenshot path.
/// <see cref="TestAppTests.Screenshot_ReturnsNonEmptyPng"/> covers the in-memory
/// byte-array return; these tests cover the file-write path of
/// <see cref="IFlawrightPage.ScreenshotAsync(string, CancellationToken)"/> and
/// <see cref="IFlawrightPage.ScreenshotAsync(LocatorScreenshotOptions, CancellationToken)"/>.
/// </summary>
/// <remarks>
/// PNG signature bytes (89 50 4E 47 0D 0A 1A 0A) are validated against the
/// file contents to ensure the bytes written to disk match what the in-memory
/// API returns.
/// </remarks>
public class TestAppScreenshotFileTests : IAsyncLifetime
{
    private static readonly string TestAppPath =
        Path.Combine(AppContext.BaseDirectory, "TestApp", "Flawright.E2ETests.TestApp.exe");

    /// <summary>PNG file signature.</summary>
    private static readonly byte[] PngSignature =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private IFlawright? _fw;
    private string? _scratchDir;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _scratchDir = Path.Combine(
            Path.GetTempPath(),
            "Flawright.E2E.Screenshots." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_scratchDir);

        _fw = await global::Flawright.Flawright.LaunchAsync(
            new LaunchOptions { ApplicationPath = TestAppPath },
            new FlawrightOptions
            {
                InputMode = new VirtualInputMode(),
                CloseBehavior = new DismissDialogCloseBehavior("Don't Save"),
                DefaultTimeout = TimeSpan.FromSeconds(5),
                ScreenshotDirectory = _scratchDir,
            });
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_fw != null)
        {
            await _fw.Browser.CloseAsync();
            await _fw.DisposeAsync();
        }

        if (_scratchDir != null && Directory.Exists(_scratchDir))
        {
            try { Directory.Delete(_scratchDir, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }

    /// <summary>
    /// <see cref="IFlawrightPage.ScreenshotAsync(string, CancellationToken)"/>
    /// writes a non-empty PNG file at the supplied path and returns the same
    /// bytes that were written.
    /// </summary>
    [Fact]
    public async Task ScreenshotAsync_StringPath_WritesPngToDisk()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var path = Path.Combine(_scratchDir!, "screenshot.png");
        var returned = await page.ScreenshotAsync(path);

        Assert.True(File.Exists(path), $"Expected screenshot file at '{path}'.");
        var fileBytes = await File.ReadAllBytesAsync(path);

        Assert.True(fileBytes.Length > 8, "Screenshot file is shorter than the PNG header.");
        Assert.Equal(PngSignature, fileBytes[..8]);
        Assert.Equal(returned.Length, fileBytes.Length);
        Assert.Equal(returned, fileBytes);
    }

    /// <summary>
    /// <see cref="IFlawrightPage.ScreenshotAsync(LocatorScreenshotOptions, CancellationToken)"/>
    /// with <see cref="LocatorScreenshotOptions.Path"/> writes the bytes to
    /// disk at the supplied location.
    /// </summary>
    [Fact]
    public async Task ScreenshotAsync_OptionsPath_WritesPngToDisk()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var path = Path.Combine(_scratchDir!, "screenshot-via-options.png");
        var returned = await page.ScreenshotAsync(new LocatorScreenshotOptions { Path = path });

        Assert.True(File.Exists(path), $"Expected screenshot file at '{path}'.");
        var fileBytes = await File.ReadAllBytesAsync(path);
        Assert.True(fileBytes.Length > 8);
        Assert.Equal(PngSignature, fileBytes[..8]);
        Assert.Equal(returned, fileBytes);
    }

    /// <summary>
    /// <see cref="IFlawrightPage.ScreenshotAsync(string, CancellationToken)"/>
    /// with a path inside a directory that does not yet exist must create the
    /// directory automatically (matching <c>FlawrightLocator.ScreenshotAsync</c>
    /// behavior) and successfully write the file.
    /// </summary>
    [Fact]
    public async Task ScreenshotAsync_PathInMissingDirectory_CreatesDirectoryAndWritesFile()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var missingDir = Path.Combine(_scratchDir!, "does-not-exist-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(missingDir, "out.png");

        Assert.False(Directory.Exists(missingDir), "Pre-condition: directory must not exist.");

        await page.ScreenshotAsync(path);

        Assert.True(Directory.Exists(missingDir), "ScreenshotAsync must create the directory.");
        Assert.True(File.Exists(path), "Screenshot file must exist after directory creation.");
    }
}
