using System.Diagnostics.CodeAnalysis;
using FlaUI.Core;
using FlaUI.UIA3;
using Flawright.AumidResolver;
using Flawright.Internals;

namespace Flawright.Backends.Uia;

/// <summary>
/// FlaUI-backed <see cref="IApplicationLauncher"/> that delegates to
/// FlaUI <c>Application.Launch</c>, <c>Application.LaunchStoreApp</c>,
/// <c>Application.Attach</c>, etc.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "FlaUI I/O; covered by E2E tests only.")]
internal sealed class FlaUiApplicationLauncher : IApplicationLauncher
{
    /// <inheritdoc/>
    public Task<IApplicationHandle> Launch(LaunchOptions opts, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(opts);

        // Pre-flight: resolve the application path.  The resolver detects
        // Windows AppExecutionAlias stubs and System32 shell-launcher shims
        // (e.g. notepad.exe, calc.exe on Windows 11) and redirects them to
        // LaunchStoreApp so FlaUI binds to the real packaged-app process
        // instead of the short-lived stub/broker PID.
        var resolver = opts.AumidResolver ?? new WindowsAumidResolver();
        var target = resolver.Resolve(opts.ApplicationPath!);

        if (target.Kind == LaunchKind.Aumid)
        {
            var aliasArgs = opts.Arguments == null ? "" : string.Join(' ', opts.Arguments);
            return LaunchStoreApp(target.Value, aliasArgs, ct);
        }

        var psi = new System.Diagnostics.ProcessStartInfo(opts.ApplicationPath!)
        {
            Arguments = string.Join(" ", opts.Arguments ?? [])
        };

        if (opts.WorkingDirectory != null)
            psi.WorkingDirectory = opts.WorkingDirectory;

        var app = Application.AttachOrLaunch(psi);
        return Task.FromResult<IApplicationHandle>(new FlaUiApplicationHandle(app, new UIA3Automation()));
    }

    /// <inheritdoc/>
    public async Task<IApplicationHandle> LaunchStoreApp(string aumid, string args, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(aumid);
        var app = Application.LaunchStoreApp(aumid, args);

        // FlaUI returns an Application tracking the activator/broker PID returned
        // by IApplicationActivationManager::ActivateApplication.  On Windows 11
        // that broker exits within ~1 second after handing off to the actual app
        // process (e.g. Notepad.exe).  Calling WaitWhileMainHandleIsMissing on the
        // stale PID then throws "Process with an Id of N is not running".
        //
        // Fix: poll asynchronously for a process whose main module path is under the
        // package install directory and re-Attach FlaUI's tracking to that live PID.
        var pfn = PackagedAppResolver.GetPackageFamilyName(aumid);
        var realPid = await PackagedAppResolver.WaitForPackagedAppProcessAsync(
            pfn, TimeSpan.FromSeconds(5), ct: ct).ConfigureAwait(false);

        if (realPid != 0 && realPid != app.ProcessId)
        {
#pragma warning disable CA1031 // Best-effort re-attach: fallback to activator if Attach fails
            try
            {
                // Re-attach to the real packaged app process, preserving IsStoreApp=true.
                // Application.Attach(int) defaults IsStoreApp to false, which would lose the
                // packaged-app semantics needed by FlaUiApplicationHandle.  Use the public
                // Application(int, bool) constructor directly to keep IsStoreApp=true.
                app = new Application(realPid, isStoreApp: true);
            }
            catch
            {
                // If re-attach fails, fall back to whatever LaunchStoreApp returned
                // (the activator may still be live in some edge-case scenarios).
            }
#pragma warning restore CA1031
        }

        return new FlaUiApplicationHandle(app, new UIA3Automation());
    }

    /// <inheritdoc/>
    public Task<IApplicationHandle> Attach(int pid, CancellationToken ct = default)
    {
        var app = Application.Attach(pid);
        return Task.FromResult<IApplicationHandle>(new FlaUiApplicationHandle(app, new UIA3Automation()));
    }

    /// <inheritdoc/>
    public Task<IApplicationHandle> AttachByName(string exeBaseName, int index, CancellationToken ct = default)
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
        return Task.FromResult<IApplicationHandle>(new FlaUiApplicationHandle(app, new UIA3Automation()));
    }
}
