using Reqnroll;
using Reqnroll.BoDi;

namespace JerrettDavis.Flawright.Reqnroll;

/// <summary>
/// Reqnroll lifecycle hooks that create and dispose a <see cref="IFlawright"/> instance
/// per scenario.
/// </summary>
/// <remarks>
/// <para>
/// <b>DI strategy:</b> BoDi (<see cref="IObjectContainer"/>) is used directly rather
/// than <c>Reqnroll.Microsoft.Extensions.DependencyInjection</c>. The
/// <c>[ScenarioDependencies]</c> lifecycle is unsuitable for per-scenario browser
/// instances due to issue #716 in the Reqnroll.Actions repo.
/// </para>
/// <para>
/// The hooks register three interfaces into the per-scenario container so that your own
/// <c>[Binding]</c> classes can receive them via constructor injection:
/// <list type="bullet">
///   <item><description><see cref="IFlawright"/></description></item>
///   <item><description><see cref="IFlawrightBrowser"/></description></item>
///   <item><description><see cref="IFlawrightPage"/></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Tag resolution order (highest priority first):</b>
/// <list type="number">
///   <item><description><c>@attachpid:&lt;pid&gt;</c></description></item>
///   <item><description><c>@attach:&lt;name&gt;</c></description></item>
///   <item><description><c>@aumid:&lt;aumid&gt;</c></description></item>
///   <item><description><c>@launch:&lt;path&gt;</c></description></item>
///   <item><description><see cref="FlawrightReqnrollOptions.DefaultAumid"/></description></item>
///   <item><description><see cref="FlawrightReqnrollOptions.DefaultApplicationPath"/></description></item>
/// </list>
/// </para>
/// </remarks>
[Binding]
public sealed class FlawrightReqnrollHooks
{
    private readonly IObjectContainer _container;
    private readonly ScenarioContext _scenarioContext;

    /// <summary>
    /// Initialises a new instance of <see cref="FlawrightReqnrollHooks"/>.
    /// </summary>
    /// <param name="container">The per-scenario BoDi container.</param>
    /// <param name="scenarioContext">The current scenario context, used to read tags.</param>
    public FlawrightReqnrollHooks(IObjectContainer container, ScenarioContext scenarioContext)
    {
        _container = container;
        _scenarioContext = scenarioContext;
    }

    /// <summary>
    /// Creates the Flawright instance before each scenario and registers
    /// <see cref="IFlawright"/>, <see cref="IFlawrightBrowser"/>, and
    /// <see cref="IFlawrightPage"/> into the BoDi container.
    /// </summary>
    /// <remarks>
    /// Reads tags from both the scenario and the parent feature, giving feature-level
    /// tags lower priority than scenario-level tags.
    /// </remarks>
    [BeforeScenario(Order = 0)]
    public async Task InitializeAsync()
    {
        // Resolve global options — optional; defaults are used when not registered.
        FlawrightReqnrollOptions? globalOptions = null;
        try { globalOptions = _container.Resolve<FlawrightReqnrollOptions>(); }
        catch (ObjectContainerException) { /* not registered — use defaults */ }

        var opts = globalOptions ?? new FlawrightReqnrollOptions();

        // Collect tags from both scenario and feature (scenario tags take precedence
        // because TagParser stops at the first matching tag of each type).
        var tags = _scenarioContext.ScenarioInfo.Tags
            .Concat(_scenarioContext.ScenarioInfo.CombinedTags ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var config = TagParser.Parse(tags);

        IFlawright fw = await ResolveAndLaunchAsync(config, opts).ConfigureAwait(false);
        IFlawrightBrowser browser = fw.Browser;
        IFlawrightPage page = await browser.NewPageAsync().ConfigureAwait(false);

        _container.RegisterInstanceAs<IFlawright>(fw);
        _container.RegisterInstanceAs<IFlawrightBrowser>(browser);
        _container.RegisterInstanceAs<IFlawrightPage>(page);
    }

    /// <summary>
    /// Disposes the Flawright instance after each scenario, ensuring the target
    /// application is closed (for launch scenarios) or detached (for attach scenarios).
    /// </summary>
    [AfterScenario(Order = int.MaxValue)]
    public async Task TeardownAsync()
    {
        // Resolve and dispose in reverse order: page first, then fw (owns browser).
        // Both are always registered by InitializeAsync; guard against partial
        // initialisation failures by resolving inside a try/catch.
        try
        {
            var page = _container.Resolve<IFlawrightPage>();
            await page.DisposeAsync().ConfigureAwait(false);
        }
        catch (ObjectContainerException) { /* never registered — InitializeAsync must have failed */ }

        try
        {
            var fw = _container.Resolve<IFlawright>();
            // Runs the CloseBehavior configured in FlawrightReqnrollOptions.FlawrightOptions.
            // Default is WindowMessageCloseBehavior; configure DismissDialogCloseBehavior
            // for apps that show a save-changes dialog on exit.
            await fw.Browser.CloseAsync().ConfigureAwait(false);
            await fw.DisposeAsync().ConfigureAwait(false);
        }
        catch (ObjectContainerException) { /* never registered — InitializeAsync must have failed */ }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static async Task<IFlawright> ResolveAndLaunchAsync(
        FlawrightLaunchConfig config,
        FlawrightReqnrollOptions opts)
    {
        var fwOptions = opts.FlawrightOptions;

        // Attach by PID — highest priority
        if (config.AttachProcessId.HasValue)
        {
            return await JerrettDavis.Flawright.Flawright.AttachAsync(
                new AttachOptions { ProcessId = config.AttachProcessId.Value },
                fwOptions).ConfigureAwait(false);
        }

        // Attach by process name
        if (config.AttachProcessName is { Length: > 0 } processName)
        {
            return await JerrettDavis.Flawright.Flawright.AttachAsync(
                new AttachOptions { ProcessName = processName },
                fwOptions).ConfigureAwait(false);
        }

        // Launch by AUMID
        if (config.Aumid is { Length: > 0 } aumid)
        {
            return await JerrettDavis.Flawright.Flawright.LaunchAsync(
                new LaunchOptions { Aumid = aumid },
                fwOptions).ConfigureAwait(false);
        }

        // Launch by path (tag or global default)
        var path = config.ApplicationPath ?? opts.DefaultApplicationPath;
        if (path is { Length: > 0 })
        {
            return await JerrettDavis.Flawright.Flawright.LaunchAsync(
                new LaunchOptions { ApplicationPath = path },
                fwOptions).ConfigureAwait(false);
        }

        // Global AUMID fallback
        if (opts.DefaultAumid is { Length: > 0 } defaultAumid)
        {
            return await JerrettDavis.Flawright.Flawright.LaunchAsync(
                new LaunchOptions { Aumid = defaultAumid },
                fwOptions).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            "Flawright.Reqnroll: no launch/attach configuration found. " +
            "Add one of the following to your scenario or feature: " +
            "@launch:<path>, @aumid:<aumid>, @attach:<name>, @attachpid:<pid>. " +
            "Alternatively, set FlawrightReqnrollOptions.DefaultApplicationPath or " +
            "FlawrightReqnrollOptions.DefaultAumid in a [BeforeTestRun] hook.");
    }
}
