using JerrettDavis.Flawright.Backends;

namespace JerrettDavis.Flawright.Page;

/// <summary>
/// Implements <see cref="IFlawrightMouse"/> by delegating to <see cref="IInputBackend"/>.
/// Exposes absolute-coordinate mouse operations mirroring Playwright's <c>Mouse</c> class.
/// </summary>
internal sealed class FlawrightMouse : IFlawrightMouse
{
    private readonly IInputBackend _input;

    internal FlawrightMouse(IInputBackend input)
    {
        _input = input;
    }

    /// <inheritdoc/>
    public Task ClickAsync(double x, double y, MouseClickOptions? options = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var button = options?.Button ?? MouseButton.Left;
        var clickCount = options?.ClickCount ?? 1;

        _input.MouseMove((int)x, (int)y, steps: 1);
        _input.MouseClick((int)x, (int)y, button, clickCount);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DoubleClickAsync(double x, double y, MouseDoubleClickOptions? options = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var button = options?.Button ?? MouseButton.Left;

        _input.MouseMove((int)x, (int)y, steps: 1);
        _input.MouseClick((int)x, (int)y, button, clickCount: 2);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DownAsync(MouseDownOptions? options = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var button = options?.Button ?? MouseButton.Left;
        _input.MouseDown(button);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UpAsync(MouseUpOptions? options = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var button = options?.Button ?? MouseButton.Left;
        _input.MouseUp(button);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task MoveAsync(double x, double y, MouseMoveOptions? options = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var steps = options?.Steps ?? 1;
        _input.MouseMove((int)x, (int)y, steps);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task WheelAsync(double deltaX, double deltaY, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _input.MouseWheel((int)deltaX, (int)deltaY);
        return Task.CompletedTask;
    }
}
