namespace JerrettDavis.Flawright;

#pragma warning disable CA1724 // Type name 'Flawright' intentionally matches the library / namespace root — this is the standard Playwright pattern
/// <summary>
/// Entry point for the Flawright desktop-automation library.
/// Use <see cref="LaunchAsync"/> to start an application, or
/// <see cref="AttachAsync"/> to connect to one that is already running.
/// </summary>
/// <example>
/// <code>
/// await using var fw = await Flawright.LaunchAsync(
///     new LaunchOptions { ApplicationPath = "notepad.exe" });
/// var page = await fw.Browser.NewPageAsync();
/// await page.FillAsync("controltype:Edit", "hello world");
/// await page.PressAsync("controltype:Edit", "Ctrl+S");
/// </code>
/// </example>
public sealed class Flawright : IFlawright
{
    private readonly FlawrightBrowser _browser;

    private Flawright(FlawrightBrowser browser)
    {
        _browser = browser;
    }

    /// <inheritdoc/>
    public IFlawrightBrowser Browser => _browser;

    /// <summary>
    /// Launches a new application and returns a Flawright instance bound to it.
    /// </summary>
    /// <param name="options">Launch options, including the path to the executable.</param>
    /// <param name="fwOptions">
    /// Global Flawright options (timeouts, screenshot directory, etc.).
    /// <see langword="null"/> uses sensible defaults.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="Flawright"/> instance whose <see cref="Browser"/> is ready
    /// to use.
    /// </returns>
    /// <example>
    /// <code>
    /// await using var fw = await Flawright.LaunchAsync(
    ///     new LaunchOptions { ApplicationPath = "calc.exe" });
    /// </code>
    /// </example>
    public static Task<Flawright> LaunchAsync(
        LaunchOptions options,
        FlawrightOptions? fwOptions = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var browser = new FlawrightBrowser(options, fwOptions ?? new FlawrightOptions());
        return Task.FromResult(new Flawright(browser));
    }

    /// <summary>
    /// Attaches to an already-running process and returns a Flawright instance.
    /// </summary>
    /// <param name="options">Attach options, including the target process ID.</param>
    /// <param name="fwOptions">
    /// Global Flawright options.  <see langword="null"/> uses sensible defaults.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="Flawright"/> instance whose <see cref="Browser"/> is ready
    /// to use.
    /// </returns>
    /// <example>
    /// <code>
    /// await using var fw = await Flawright.AttachAsync(
    ///     new AttachOptions { ProcessId = 12345 });
    /// </code>
    /// </example>
    public static Task<Flawright> AttachAsync(
        AttachOptions options,
        FlawrightOptions? fwOptions = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var browser = new FlawrightBrowser(options, fwOptions ?? new FlawrightOptions());
        return Task.FromResult(new Flawright(browser));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Options for launching a new application via Flawright.
/// </summary>
/// <example>
/// <code>
/// var opts = new LaunchOptions
/// {
///     ApplicationPath  = @"C:\Windows\System32\notepad.exe",
///     Arguments        = ["myfile.txt"],
///     WorkingDirectory = @"C:\Documents"
/// };
/// </code>
/// </example>
public sealed record LaunchOptions
{
    /// <summary>
    /// Full path or filename (resolved via PATH) of the executable to launch.
    /// </summary>
    public string ApplicationPath { get; init; } = null!;

    /// <summary>Optional command-line arguments to pass to the process.</summary>
    public string[]? Arguments { get; init; }

    /// <summary>
    /// Working directory for the launched process.
    /// <see langword="null"/> inherits the current process's working directory.
    /// </summary>
    public string? WorkingDirectory { get; init; }
}

/// <summary>Options for attaching to an existing process via Flawright.</summary>
/// <example>
/// <code>
/// var opts = new AttachOptions { ProcessId = 9876 };
/// </code>
/// </example>
public sealed record AttachOptions
{
    /// <summary>The OS process ID to attach to.</summary>
    public int ProcessId { get; init; }
}

/// <summary>
/// Exception thrown when an assertion made via
/// <see cref="IFlawrightAssertions"/> fails.
/// </summary>
/// <example>
/// <code>
/// try
/// {
///     await page.Locator("#save").Expect().ToBeVisibleAsync();
/// }
/// catch (AssertionException ex)
/// {
///     Console.WriteLine(ex.Message);
/// }
/// </code>
/// </example>
public class AssertionException : Exception
{
    /// <summary>Initialises a default <see cref="AssertionException"/>.</summary>
    public AssertionException() { }

    /// <summary>
    /// Initialises a new <see cref="AssertionException"/> with the given message.
    /// </summary>
    /// <param name="message">Human-readable description of the assertion failure.</param>
    public AssertionException(string message) : base(message) { }

    /// <summary>
    /// Initialises a new <see cref="AssertionException"/> with a message and
    /// inner exception.
    /// </summary>
    /// <param name="message">Human-readable description of the assertion failure.</param>
    /// <param name="innerException">The exception that caused this assertion failure.</param>
    public AssertionException(string message, Exception innerException)
        : base(message, innerException) { }
}
