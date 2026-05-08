namespace JerrettDavis.Flawright.Reqnroll;

/// <summary>
/// Global configuration options for the <c>Flawright.Reqnroll</c> package.
/// Register an instance via a <c>[BeforeTestRun]</c> hook using
/// <c>IObjectContainer.RegisterInstanceAs&lt;FlawrightReqnrollOptions&gt;()</c>
/// to override defaults for the entire test run.
/// </summary>
/// <example>
/// <code>
/// [Binding]
/// public static class TestRunHooks
/// {
///     [BeforeTestRun]
///     public static void RegisterOptions(IObjectContainer container)
///     {
///         container.RegisterInstanceAs(new FlawrightReqnrollOptions
///         {
///             DefaultApplicationPath = @"C:\MyApp\app.exe",
///             FlawrightOptions = new FlawrightOptions
///             {
///                 DefaultTimeout = TimeSpan.FromSeconds(15)
///             }
///         });
///     }
/// }
/// </code>
/// </example>
public class FlawrightReqnrollOptions
{
    /// <summary>
    /// Default executable path used when no <c>@launch:</c> tag is present on a scenario
    /// and no <c>@aumid:</c> tag is used.
    /// <para>
    /// A scenario must supply at least one of the launch/attach tags or this property must
    /// be set; otherwise <see cref="FlawrightReqnrollHooks"/> throws an
    /// <see cref="InvalidOperationException"/> during scenario setup.
    /// </para>
    /// </summary>
    public string? DefaultApplicationPath { get; set; }

    /// <summary>
    /// Default AUMID used when no <c>@aumid:</c> tag is present and
    /// <see cref="DefaultApplicationPath"/> is also <see langword="null"/>.
    /// </summary>
    public string? DefaultAumid { get; set; }

    /// <summary>
    /// <see cref="JerrettDavis.Flawright.FlawrightOptions"/> applied to every scenario launch.
    /// Defaults to <see cref="FlawrightOptions"/> default values (5 s timeout, 100 ms retry).
    /// </summary>
    public FlawrightOptions FlawrightOptions { get; set; } = new();
}
