using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Flawright.Reqnroll;

/// <summary>
/// Internal record that captures all launch/attach configuration derived from
/// Reqnroll scenario tags.
/// </summary>
internal sealed record FlawrightLaunchConfig
{
    /// <summary>Path from <c>@launch:&lt;path&gt;</c> tag.</summary>
    public string? ApplicationPath { get; init; }

    /// <summary>AUMID from <c>@aumid:&lt;aumid&gt;</c> tag.</summary>
    public string? Aumid { get; init; }

    /// <summary>Process name from <c>@attach:&lt;name&gt;</c> tag.</summary>
    public string? AttachProcessName { get; init; }

    /// <summary>Process ID from <c>@attachpid:&lt;pid&gt;</c> tag.</summary>
    public int? AttachProcessId { get; init; }
}

/// <summary>
/// Parses Reqnroll scenario tags into a <see cref="FlawrightLaunchConfig"/> record.
/// </summary>
/// <remarks>
/// Supported tag forms:
/// <list type="bullet">
///   <item><description><c>@launch:notepad.exe</c> — launch by executable path</description></item>
///   <item><description><c>@aumid:Microsoft.WindowsCalculator_8wekyb3d8bbwe!App</c> — launch by AUMID</description></item>
///   <item><description><c>@attach:notepad</c> — attach to running process by name</description></item>
///   <item><description><c>@attachpid:12345</c> — attach to running process by PID</description></item>
/// </list>
/// </remarks>
internal static class TagParser
{
    private const string LaunchPrefix = "launch:";
    private const string AumidPrefix = "aumid:";
    private const string AttachPrefix = "attach:";
    private const string AttachPidPrefix = "attachpid:";

    /// <summary>
    /// Parses the supplied <paramref name="tags"/> collection into a
    /// <see cref="FlawrightLaunchConfig"/>.
    /// </summary>
    /// <param name="tags">Scenario or feature tags (without the leading <c>@</c>).</param>
    /// <returns>
    /// A <see cref="FlawrightLaunchConfig"/> populated from the first matching tag of
    /// each type found. Tag precedence (highest to lowest): <c>attachpid</c>, <c>attach</c>,
    /// <c>aumid</c>, <c>launch</c>.
    /// </returns>
    [return: NotNull]
    public static FlawrightLaunchConfig Parse(IEnumerable<string> tags)
    {
        string? applicationPath = null;
        string? aumid = null;
        string? attachProcessName = null;
        int? attachProcessId = null;

        foreach (var tag in tags)
        {
            if (tag.StartsWith(LaunchPrefix, StringComparison.OrdinalIgnoreCase))
            {
                applicationPath ??= tag[LaunchPrefix.Length..];
            }
            else if (tag.StartsWith(AumidPrefix, StringComparison.OrdinalIgnoreCase))
            {
                aumid ??= tag[AumidPrefix.Length..];
            }
            else if (tag.StartsWith(AttachPrefix, StringComparison.OrdinalIgnoreCase))
            {
                attachProcessName ??= tag[AttachPrefix.Length..];
            }
            else if (tag.StartsWith(AttachPidPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (attachProcessId is null
                    && int.TryParse(tag[AttachPidPrefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
                {
                    attachProcessId = pid;
                }
            }
        }

        return new FlawrightLaunchConfig
        {
            ApplicationPath = applicationPath,
            Aumid = aumid,
            AttachProcessName = attachProcessName,
            AttachProcessId = attachProcessId
        };
    }
}
