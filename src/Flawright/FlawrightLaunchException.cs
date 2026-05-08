namespace Flawright;

/// <summary>
/// Thrown when an application is launched but exits immediately with no main window.
/// This typically indicates that the executable is an App Execution Alias stub targeting
/// a UWP/packaged app that is not installed on the current machine.
/// </summary>
/// <example>
/// <code>
/// try
/// {
///     await using var fw = await Flawright.LaunchAsync(
///         new LaunchOptions { ApplicationPath = "calc.exe" });
/// }
/// catch (FlawrightLaunchException ex)
/// {
///     Console.WriteLine(ex.Message);
/// }
/// </code>
/// </example>
public sealed class FlawrightLaunchException : Exception
{
    /// <summary>Initialises a default <see cref="FlawrightLaunchException"/>.</summary>
    public FlawrightLaunchException() { }

    /// <summary>
    /// Initialises a new instance of <see cref="FlawrightLaunchException"/>
    /// with a custom message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public FlawrightLaunchException(string message) : base(message) { }

    /// <summary>
    /// Initialises a new instance of <see cref="FlawrightLaunchException"/>
    /// with a message and inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public FlawrightLaunchException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initialises a new instance of <see cref="FlawrightLaunchException"/> with the
    /// original path that was requested, the resolved path (executable that was
    /// actually invoked), and the elapsed time before the process exited.
    /// </summary>
    /// <param name="originalPath">
    /// The <see cref="LaunchOptions.ApplicationPath"/> value supplied by the caller.
    /// </param>
    /// <param name="resolvedPath">
    /// The executable path that was actually launched (after AUMID resolution /
    /// PATH lookup).
    /// </param>
    /// <param name="elapsedMs">
    /// Approximate milliseconds from process start to detected exit.
    /// </param>
    /// <param name="innerException">Optional underlying FlaUI or OS exception.</param>
    public FlawrightLaunchException(
        string originalPath,
        string resolvedPath,
        int elapsedMs,
        Exception? innerException = null)
        : base(BuildMessage(originalPath, resolvedPath, elapsedMs), innerException)
    {
        OriginalPath = originalPath;
        ResolvedPath = resolvedPath;
        ElapsedMs = elapsedMs;
    }

    /// <summary>
    /// Gets the <see cref="LaunchOptions.ApplicationPath"/> value supplied by the caller.
    /// </summary>
    public string? OriginalPath { get; }

    /// <summary>
    /// Gets the executable path that was actually invoked (after AUMID resolution /
    /// PATH lookup). May equal <see cref="OriginalPath"/> when no resolution occurred.
    /// </summary>
    public string? ResolvedPath { get; }

    /// <summary>
    /// Gets the approximate number of milliseconds elapsed between process start and
    /// detected exit (without a main window appearing).
    /// </summary>
    public int? ElapsedMs { get; }

    private static string BuildMessage(string originalPath, string resolvedPath, int elapsedMs)
    {
        var pathNote = string.Equals(originalPath, resolvedPath, StringComparison.OrdinalIgnoreCase)
            ? $"'{originalPath}'"
            : $"'{originalPath}' (resolved to '{resolvedPath}')";

        return $"""
                Application {pathNote} launched but exited within {elapsedMs}ms with no main window.
                This typically means the executable is an App Execution Alias stub for an
                uninstalled UWP package on this machine.

                To resolve:
                - Install the target package on this machine (e.g. for Calculator:
                  'winget install Microsoft.WindowsCalculator --silent').
                - Or pass an explicit AUMID via LaunchOptions.Aumid.
                - Or supply a custom IAumidResolver via LaunchOptions.AumidResolver
                  for non-default app mappings.
                """;
    }
}
