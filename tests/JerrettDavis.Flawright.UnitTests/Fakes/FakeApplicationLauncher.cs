using JerrettDavis.Flawright.Backends;

namespace JerrettDavis.Flawright.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="IApplicationLauncher"/> for unit tests.
///
/// Records all method calls with their arguments. Returns a configurable
/// <see cref="FakeApplicationHandle"/>.
/// </summary>
internal sealed class FakeApplicationLauncher : IApplicationLauncher
{
    // ── Configuration ─────────────────────────────────────────────────────────

    /// <summary>
    /// The handle returned by all launch/attach methods.
    /// Tests can replace this with a custom fake.
    /// </summary>
    public FakeApplicationHandle Handle { get; set; } = new();

    // ── Call recording ────────────────────────────────────────────────────────

    /// <summary>All recorded <see cref="Launch"/> calls, in order.</summary>
    public IReadOnlyList<LaunchOptions> LaunchCalls => _launchCalls.AsReadOnly();
    private readonly List<LaunchOptions> _launchCalls = [];

    /// <summary>All recorded <see cref="LaunchStoreApp"/> calls, in order.</summary>
    public IReadOnlyList<(string Aumid, string Args)> LaunchStoreAppCalls => _launchStoreAppCalls.AsReadOnly();
    private readonly List<(string Aumid, string Args)> _launchStoreAppCalls = [];

    /// <summary>All recorded <see cref="Attach(int)"/> calls, in order.</summary>
    public IReadOnlyList<int> AttachByPidCalls => _attachByPidCalls.AsReadOnly();
    private readonly List<int> _attachByPidCalls = [];

    /// <summary>All recorded <see cref="AttachByName"/> calls, in order.</summary>
    public IReadOnlyList<(string ExeBaseName, int Index)> AttachByNameCalls => _attachByNameCalls.AsReadOnly();
    private readonly List<(string ExeBaseName, int Index)> _attachByNameCalls = [];

    // ── IApplicationLauncher ──────────────────────────────────────────────────

    /// <inheritdoc/>
    public IApplicationHandle Launch(LaunchOptions opts)
    {
        ArgumentNullException.ThrowIfNull(opts);
        _launchCalls.Add(opts);
        return Handle;
    }

    /// <inheritdoc/>
    public IApplicationHandle LaunchStoreApp(string aumid, string args)
    {
        ArgumentNullException.ThrowIfNull(aumid);
        _launchStoreAppCalls.Add((aumid, args));
        return Handle;
    }

    /// <inheritdoc/>
    public IApplicationHandle Attach(int pid)
    {
        _attachByPidCalls.Add(pid);
        return Handle;
    }

    /// <inheritdoc/>
    public IApplicationHandle AttachByName(string exeBaseName, int index)
    {
        ArgumentNullException.ThrowIfNull(exeBaseName);
        _attachByNameCalls.Add((exeBaseName, index));
        return Handle;
    }
}
