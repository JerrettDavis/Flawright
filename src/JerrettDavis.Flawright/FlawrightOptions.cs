using JerrettDavis.Flawright.CloseBehaviors;

namespace JerrettDavis.Flawright;

/// <summary>
/// Global configuration options for Flawright, controlling timeouts, retry
/// intervals, screenshot output, and close behavior.
/// </summary>
/// <example>
/// <code>
/// var options = new FlawrightOptions
/// {
///     DefaultTimeout      = TimeSpan.FromSeconds(10),
///     DefaultRetryInterval = TimeSpan.FromMilliseconds(50),
///     ScreenshotDirectory  = @"C:\TestScreenshots",
///     CloseBehavior        = new DismissDialogCloseBehavior()
/// };
/// await using var fw = await Flawright.LaunchAsync(
///     new LaunchOptions { ApplicationPath = "notepad.exe" },
///     options);
/// </code>
/// </example>
public sealed record FlawrightOptions
{
    /// <summary>
    /// How long auto-waiting locator operations will poll before throwing
    /// <see cref="FlawrightTimeoutException"/>.  Defaults to 5 seconds.
    /// </summary>
    public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How frequently auto-waiting operations re-check the condition.
    /// Defaults to 100 milliseconds.
    /// </summary>
    public TimeSpan DefaultRetryInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Directory in which screenshots are saved when a path is not supplied to
    /// <c>ScreenshotAsync</c>.  <see langword="null"/> means the current
    /// working directory is used.
    /// </summary>
    public string? ScreenshotDirectory { get; init; }

    /// <summary>
    /// Strategy for closing the launched application. Defaults to
    /// <see cref="WindowMessageCloseBehavior"/> — sends WM_CLOSE and waits
    /// for exit. Override to handle modal dialogs (e.g.
    /// <see cref="DismissDialogCloseBehavior"/>) or force-kill
    /// (<see cref="KillCloseBehavior"/>).
    /// </summary>
    public ICloseBehavior CloseBehavior { get; init; } = new WindowMessageCloseBehavior();
}
