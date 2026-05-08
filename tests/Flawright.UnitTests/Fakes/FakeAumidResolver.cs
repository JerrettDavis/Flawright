using Flawright.AumidResolver;

namespace Flawright.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="IAumidResolver"/> for unit tests.
///
/// Pre-register application paths with their expected <see cref="LaunchTarget"/>
/// values via <see cref="Register"/>.  Any path not registered returns a
/// <see cref="LaunchKind.Path"/> target using the original path.
/// </summary>
internal sealed class FakeAumidResolver : IAumidResolver
{
    private readonly Dictionary<string, LaunchTarget> _map =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>All recorded <see cref="Resolve"/> calls, in order.</summary>
    public IReadOnlyList<string> ResolveCalls => _calls.AsReadOnly();
    private readonly List<string> _calls = [];

    // ── Configuration ─────────────────────────────────────────────────────────

    /// <summary>
    /// Registers a predetermined <see cref="LaunchTarget"/> for the given
    /// <paramref name="applicationPath"/>.
    /// </summary>
    /// <param name="applicationPath">
    /// The path (bare name or full path) that the resolver should intercept.
    /// Case-insensitive.
    /// </param>
    /// <param name="target">The target to return for this path.</param>
    public void Register(string applicationPath, LaunchTarget target)
        => _map[applicationPath] = target;

    /// <summary>
    /// Convenience overload: register <paramref name="applicationPath"/> as
    /// an AUMID target.
    /// </summary>
    public void RegisterAumid(string applicationPath, string aumid)
        => Register(applicationPath, new LaunchTarget(LaunchKind.Aumid, aumid));

    // ── IAumidResolver ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public LaunchTarget Resolve(string applicationPath)
    {
        _calls.Add(applicationPath);

        return _map.TryGetValue(applicationPath, out var target)
            ? target
            : new LaunchTarget(LaunchKind.Path, applicationPath);
    }
}
