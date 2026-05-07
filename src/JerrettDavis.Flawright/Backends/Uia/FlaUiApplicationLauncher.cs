using System.Diagnostics.CodeAnalysis;
using FlaUI.Core;
using FlaUI.UIA3;

namespace JerrettDavis.Flawright.Backends.Uia;

/// <summary>
/// FlaUI-backed <see cref="IApplicationLauncher"/> that delegates to
/// FlaUI <c>Application.Launch</c>, <c>Application.LaunchStoreApp</c>,
/// <c>Application.Attach</c>, etc.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "FlaUI I/O; covered by E2E tests only.")]
internal sealed class FlaUiApplicationLauncher : IApplicationLauncher
{
    /// <inheritdoc/>
    public IApplicationHandle Launch(LaunchOptions opts)
    {
        ArgumentNullException.ThrowIfNull(opts);

        var psi = new System.Diagnostics.ProcessStartInfo(opts.ApplicationPath!)
        {
            Arguments = string.Join(" ", opts.Arguments ?? [])
        };

        if (opts.WorkingDirectory != null)
            psi.WorkingDirectory = opts.WorkingDirectory;

        var app = Application.AttachOrLaunch(psi);
        return new FlaUiApplicationHandle(app, new UIA3Automation());
    }

    /// <inheritdoc/>
    public IApplicationHandle LaunchStoreApp(string aumid, string args)
    {
        ArgumentNullException.ThrowIfNull(aumid);
        var app = Application.LaunchStoreApp(aumid, args);
        return new FlaUiApplicationHandle(app, new UIA3Automation());
    }

    /// <inheritdoc/>
    public IApplicationHandle Attach(int pid)
    {
        var app = Application.Attach(pid);
        return new FlaUiApplicationHandle(app, new UIA3Automation());
    }

    /// <inheritdoc/>
    public IApplicationHandle AttachByName(string exeBaseName, int index)
    {
        ArgumentNullException.ThrowIfNull(exeBaseName);

        var processes = System.Diagnostics.Process
            .GetProcessesByName(exeBaseName)
            .OrderBy(p => p.Id)
            .ToArray();

        if (processes.Length == 0)
            throw new InvalidOperationException(
                $"No running process named '{exeBaseName}' was found.");

        if (index >= processes.Length)
            throw new InvalidOperationException(
                $"Process '{exeBaseName}' has {processes.Length} instance(s); index {index} is out of range.");

        var app = Application.Attach(processes[index].Id);
        return new FlaUiApplicationHandle(app, new UIA3Automation());
    }
}
