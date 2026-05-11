namespace Flawright;

#pragma warning disable CA1711 // EventArgs suffix is the standard naming convention for event argument classes

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
public sealed class AppExecutionAliasResolvedEventArgs : EventArgs
{
    /// <summary>The application path as originally specified by the caller.</summary>
    public string OriginalPath { get; }

    /// <summary>The Application User Model ID that the alias was resolved to.</summary>
    public string ResolvedAumid { get; }

    /// <summary>The package family name extracted from the AUMID.</summary>
    public string PackageFamilyName { get; }

    /// <summary>Initializes a new instance of AppExecutionAliasResolvedEventArgs.</summary>
    public AppExecutionAliasResolvedEventArgs(
        string originalPath,
        string resolvedAumid,
        string packageFamilyName)
    {
        OriginalPath = originalPath;
        ResolvedAumid = resolvedAumid;
        PackageFamilyName = packageFamilyName;
    }
}

#pragma warning restore CA1711
