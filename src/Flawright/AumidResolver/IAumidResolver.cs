namespace Flawright.AumidResolver;

/// <summary>
/// Strategy for resolving an executable path (or bare filename) to a launch
/// descriptor that tells Flawright whether to use
/// <c>Application.Launch</c> (standard Win32 path) or
/// <c>Application.LaunchStoreApp</c> (packaged app via AUMID).
/// </summary>
/// <remarks>
/// <para>
/// The default implementation (<see cref="WindowsAumidResolver"/>) detects
/// Windows AppExecutionAlias stubs and System32 launcher shims (e.g.
/// <c>notepad.exe</c>, <c>calc.exe</c>, <c>mspaint.exe</c>) and
/// transparently redirects them to the corresponding packaged-app AUMID so
/// that FlaUI binds to the real application process.
/// </para>
/// <para>
/// Inject a custom implementation via
/// <see cref="LaunchOptions.AumidResolver"/> when you need to override the
/// default resolution — for example in unit tests or when targeting an app
/// whose AUMID mapping is not in the built-in table.
/// </para>
/// </remarks>
/// <example>
/// Custom resolver:
/// <code>
/// var opts = new LaunchOptions
/// {
///     ApplicationPath = "myapp.exe",
///     AumidResolver   = new MyCustomAumidResolver()
/// };
/// </code>
/// Fake resolver for unit tests:
/// <code>
/// var fake = new FakeAumidResolver();
/// fake.Register("notepad.exe",
///     new LaunchTarget(LaunchKind.Aumid, "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App"));
/// </code>
/// </example>
public interface IAumidResolver
{
    /// <summary>
    /// Resolves <paramref name="applicationPath"/> to a <see cref="LaunchTarget"/>
    /// indicating how the application should be launched.
    /// </summary>
    /// <param name="applicationPath">
    /// The value of <see cref="LaunchOptions.ApplicationPath"/> — may be a bare
    /// filename (<c>"notepad.exe"</c>), a full path, or any PATH-resolvable name.
    /// </param>
    /// <returns>
    /// A <see cref="LaunchTarget"/> with <see cref="LaunchKind.Aumid"/> and the
    /// resolved AUMID when the path is detected as an alias stub or shell-launcher
    /// shim; or a <see cref="LaunchTarget"/> with <see cref="LaunchKind.Path"/> and
    /// the original path when no alias is detected.
    /// </returns>
    LaunchTarget Resolve(string applicationPath);
}

/// <summary>
/// Describes how an application should be launched — either via a Win32 path
/// or via a packaged-app AUMID.
/// </summary>
/// <param name="Kind">
/// Whether <see cref="Value"/> is a Win32 path or a packaged-app AUMID.
/// </param>
/// <param name="Value">
/// The launch value: a Win32 executable path when <see cref="Kind"/> is
/// <see cref="LaunchKind.Path"/>, or an AUMID string such as
/// <c>"Microsoft.WindowsCalculator_8wekyb3d8bbwe!App"</c> when
/// <see cref="Kind"/> is <see cref="LaunchKind.Aumid"/>.
/// </param>
public readonly record struct LaunchTarget(LaunchKind Kind, string Value);

/// <summary>
/// Discriminator for <see cref="LaunchTarget"/> — whether to use a Win32
/// executable path or a packaged-app AUMID.
/// </summary>
public enum LaunchKind
{
    /// <summary>Launch via a Win32 executable path using <c>Application.Launch</c>.</summary>
    Path,

    /// <summary>Launch via a packaged-app AUMID using <c>Application.LaunchStoreApp</c>.</summary>
    Aumid,
}
