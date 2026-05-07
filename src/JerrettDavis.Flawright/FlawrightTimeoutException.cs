namespace JerrettDavis.Flawright;

/// <summary>
/// Thrown when an auto-waiting locator or assertion operation does not succeed
/// within the configured timeout period.
/// </summary>
/// <example>
/// <code>
/// try
/// {
///     await page.Locator("#save").FirstAsync(timeout: TimeSpan.FromSeconds(2));
/// }
/// catch (FlawrightTimeoutException ex)
/// {
///     Console.WriteLine(ex.Message);
///     // "Locator '#save' not found within 00:00:02."
/// }
/// </code>
/// </example>
public sealed class FlawrightTimeoutException : TimeoutException
{
    /// <summary>Initialises a default <see cref="FlawrightTimeoutException"/>.</summary>
    public FlawrightTimeoutException() { }

    /// <summary>
    /// Initialises a new instance of <see cref="FlawrightTimeoutException"/>
    /// with a custom message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public FlawrightTimeoutException(string message) : base(message) { }

    /// <summary>
    /// Initialises a new instance of <see cref="FlawrightTimeoutException"/>
    /// with a message and inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public FlawrightTimeoutException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initialises a new instance of <see cref="FlawrightTimeoutException"/>
    /// with the selector that was not found and the duration that was waited.
    /// </summary>
    /// <param name="selector">The selector string that timed out.</param>
    /// <param name="timeout">How long the operation polled before giving up.</param>
    public FlawrightTimeoutException(string selector, TimeSpan timeout)
        : base($"Locator '{selector}' not found within {timeout}.")
    {
        Selector = selector;
        Timeout = timeout;
    }

    /// <summary>Gets the selector that caused the timeout, if available.</summary>
    public string? Selector { get; }

    /// <summary>Gets the timeout duration that was exceeded, if available.</summary>
    public TimeSpan? Timeout { get; }
}
