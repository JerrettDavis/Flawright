using System.Diagnostics.CodeAnalysis;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;
using JerrettDavis.Flawright.Backends;
using JerrettDavis.Flawright.Backends.Uia;
using JerrettDavis.Flawright.Selectors;

namespace JerrettDavis.Flawright;

#pragma warning disable CA1724 // Type name 'Flawright' intentionally matches the library / namespace root — this is the standard Playwright pattern
/// <summary>
/// Entry point for the Flawright desktop-automation library.
/// Use <see cref="LaunchAsync(LaunchOptions, FlawrightOptions?, CancellationToken)"/> to
/// start an application, or
/// <see cref="AttachAsync(AttachOptions, FlawrightOptions?, CancellationToken)"/> to
/// connect to one that is already running.
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

    // ── Public factory ────────────────────────────────────────────────────────

    /// <summary>
    /// Launches a new application and returns a Flawright instance bound to it.
    /// </summary>
    /// <param name="options">
    /// Launch options.  Exactly one of <see cref="LaunchOptions.ApplicationPath"/> or
    /// <see cref="LaunchOptions.Aumid"/> must be set.
    /// </param>
    /// <param name="fwOptions">
    /// Global Flawright options (timeouts, screenshot directory, etc.).
    /// <see langword="null"/> uses sensible defaults.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="Flawright"/> instance whose <see cref="Browser"/> is ready to use.
    /// </returns>
    /// <example>
    /// <code>
    /// // Win32 app
    /// await using var fw = await Flawright.LaunchAsync(
    ///     new LaunchOptions { ApplicationPath = "calc.exe" });
    ///
    /// // Store / UWP app
    /// await using var fw = await Flawright.LaunchAsync(
    ///     new LaunchOptions { Aumid = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App" });
    /// </code>
    /// </example>
    [ExcludeFromCodeCoverage] // Requires real FlaUI/UIA — covered by E2E tests only
    public static Task<IFlawright> LaunchAsync(
        LaunchOptions options,
        FlawrightOptions? fwOptions = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var automation = new UIA3Automation();
        var translator = new UiaConditionTranslator(
            new ConditionFactory(automation.PropertyLibrary),
            AriaRoleMapper.Map);
        return LaunchAsync(
            options,
            fwOptions,
            new FlaUiApplicationLauncher(),
            new FlaUiInputBackend(),
            translator,
            ct);
    }

    /// <summary>
    /// Attaches to an already-running process and returns a Flawright instance.
    /// </summary>
    /// <param name="options">
    /// Attach options.  Exactly one of <see cref="AttachOptions.ProcessId"/> or
    /// <see cref="AttachOptions.ProcessName"/> must be set.
    /// </param>
    /// <param name="fwOptions">
    /// Global Flawright options.  <see langword="null"/> uses sensible defaults.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="Flawright"/> instance whose <see cref="Browser"/> is ready to use.
    /// </returns>
    /// <example>
    /// <code>
    /// await using var fw = await Flawright.AttachAsync(
    ///     new AttachOptions { ProcessId = 12345 });
    /// </code>
    /// </example>
    [ExcludeFromCodeCoverage] // Requires real FlaUI/UIA — covered by E2E tests only
    public static Task<IFlawright> AttachAsync(
        AttachOptions options,
        FlawrightOptions? fwOptions = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var automation = new UIA3Automation();
        var translator = new UiaConditionTranslator(
            new ConditionFactory(automation.PropertyLibrary),
            AriaRoleMapper.Map);
        return AttachAsync(
            options,
            fwOptions,
            new FlaUiApplicationLauncher(),
            new FlaUiInputBackend(),
            translator,
            ct);
    }

    // ── Internal factory overloads (for testing) ──────────────────────────────

    /// <summary>
    /// Launches a new application using the provided launcher, input backend, and translator.
    /// Intended for unit tests that inject fake backends.
    /// </summary>
    internal static Task<IFlawright> LaunchAsync(
        LaunchOptions options,
        FlawrightOptions? fwOptions,
        IApplicationLauncher launcher,
        IInputBackend input,
        IConditionTranslator translator,
        CancellationToken ct = default)
    {
        var opts = fwOptions ?? new FlawrightOptions();
        var browser = new FlawrightBrowser(launcher, input, translator, options, opts);
        return Task.FromResult<IFlawright>(new Flawright(browser));
    }

    /// <summary>
    /// Attaches to an already-running process using the provided launcher, input backend, and translator.
    /// Intended for unit tests that inject fake backends.
    /// </summary>
    internal static Task<IFlawright> AttachAsync(
        AttachOptions options,
        FlawrightOptions? fwOptions,
        IApplicationLauncher launcher,
        IInputBackend input,
        IConditionTranslator translator,
        CancellationToken ct = default)
    {
        var opts = fwOptions ?? new FlawrightOptions();
        var browser = new FlawrightBrowser(launcher, input, translator, options, opts);
        return Task.FromResult<IFlawright>(new Flawright(browser));
    }

    // ── IAsyncDisposable ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync().ConfigureAwait(false);
    }
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
