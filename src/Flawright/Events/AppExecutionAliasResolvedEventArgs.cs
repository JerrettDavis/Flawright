namespace Flawright;

/// <summary>
/// Raised when an AppExecutionAlias stub or system shell-launcher shim
/// (e.g. calc.exe, notepad.exe on Windows 11) is transparently redirected
/// to the real packaged-app AUMID.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised before the actual store-app launch begins, allowing
/// subscribers to observe the alias resolution behavior that Flawright
/// performs automatically.
/// </para>
/// <para>
/// <b>Thread safety:</b> Handlers are invoked synchronously on the thread
/// that triggered the resolution.  Misbehaving handlers must not block or
/// raise unhandled exceptions, as handler exceptions are swallowed and not
/// propagated to the caller.
/// </para>
/// </remarks>
/// <param name="OriginalPath">
/// The application path as originally specified by the caller
/// (e.g. "calc.exe" or "notepad.exe").
/// </param>
/// <param name="ResolvedAumid">
/// The Application User Model ID that the alias was resolved to
/// (e.g. "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App").
/// </param>
/// <param name="PackageFamilyName">
/// The package family name extracted from the AUMID
/// (everything before the "!" separator).
/// </param>
public sealed record AppExecutionAliasResolvedEventArgs(
    string OriginalPath,
    string ResolvedAumid,
    string PackageFamilyName);
