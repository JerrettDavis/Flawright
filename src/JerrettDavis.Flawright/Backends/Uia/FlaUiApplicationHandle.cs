using System.Diagnostics.CodeAnalysis;
using FlaUI.Core;
using FlaUI.UIA3;

namespace JerrettDavis.Flawright.Backends.Uia;

/// <summary>
/// FlaUI-backed <see cref="IApplicationHandle"/> wrapping <see cref="Application"/>
/// and <see cref="UIA3Automation"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "FlaUI I/O; covered by E2E tests only.")]
internal sealed class FlaUiApplicationHandle : IApplicationHandle
{
    private readonly Application _app;
    private readonly UIA3Automation _automation;
    private bool _disposed;

    internal FlaUiApplicationHandle(Application app, UIA3Automation automation)
    {
        _app = app;
        _automation = automation;
    }

    /// <inheritdoc/>
    public int ProcessId => _app.ProcessId;

    /// <inheritdoc/>
    public bool HasExited => _app.HasExited;

    /// <inheritdoc/>
    public bool IsStoreApp => _app.IsStoreApp;

    /// <inheritdoc/>
    public bool WaitWhileMainHandleIsMissing(TimeSpan timeout)
        => _app.WaitWhileMainHandleIsMissing(timeout);

    /// <inheritdoc/>
    public void Close() => _app.Close();

    /// <inheritdoc/>
    public void KillProcessTree()
    {
#pragma warning disable CA1031 // Best-effort kill
        try
        {
            var proc = System.Diagnostics.Process.GetProcessById(_app.ProcessId);
            proc.Kill(entireProcessTree: true);
        }
        catch (Exception)
        {
            // Process may have already exited
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    public IElementBackend GetMainWindow()
    {
        var window = _app.GetMainWindow(_automation);
        return new UiaElementBackend(window);
    }

    /// <inheritdoc/>
    public IReadOnlyList<IElementBackend> GetAllTopLevelWindows()
    {
        var windows = _app.GetAllTopLevelWindows(_automation);
        return windows
            .Select(w => (IElementBackend)new UiaElementBackend(w))
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _automation.Dispose();
        _app.Dispose();
    }
}
