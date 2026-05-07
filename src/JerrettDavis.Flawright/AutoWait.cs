namespace JerrettDavis.Flawright;

/// <summary>
/// Internal utility for auto-waiting polling loops used by locators and assertions.
/// </summary>
internal static class AutoWait
{
    /// <summary>
    /// Polls <paramref name="condition"/> until it returns a non-<see langword="null"/>
    /// result or the timeout expires.
    /// </summary>
    /// <typeparam name="T">Non-nullable result type.</typeparam>
    /// <param name="condition">Factory that returns a result when the condition is met, or <see langword="null"/> while pending.</param>
    /// <param name="selector">Selector string used in timeout error messages.</param>
    /// <param name="timeout">Maximum wait duration.</param>
    /// <param name="retryInterval">Time between polls.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The first non-<see langword="null"/> value returned by <paramref name="condition"/>.</returns>
    /// <exception cref="FlawrightTimeoutException">
    /// Thrown when <paramref name="timeout"/> is exceeded.
    /// </exception>
    internal static async Task<T> UntilAsync<T>(
        Func<T?> condition,
        string selector,
        TimeSpan timeout,
        TimeSpan retryInterval,
        CancellationToken ct)
        where T : class
    {
        var deadline = DateTime.UtcNow + timeout;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            T? result = null;
            try
            {
                result = await Task.Run(condition, ct).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Swallow all transient failures during polling; timeout will surface the real error
            catch (Exception) when (!ct.IsCancellationRequested)
#pragma warning restore CA1031
            {
                // Swallow transient failures during polling
            }

            if (result != null)
                return result;

            if (DateTime.UtcNow >= deadline)
                throw new FlawrightTimeoutException(selector, timeout);

            await Task.Delay(retryInterval, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Polls until <paramref name="condition"/> returns <see langword="true"/>
    /// or the timeout expires.
    /// </summary>
    /// <param name="condition">Predicate to evaluate each poll.</param>
    /// <param name="description">Description used in timeout error messages.</param>
    /// <param name="timeout">Maximum wait duration.</param>
    /// <param name="retryInterval">Time between polls.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="FlawrightTimeoutException">Thrown when timeout is exceeded.</exception>
    internal static async Task UntilTrueAsync(
        Func<bool> condition,
        string description,
        TimeSpan timeout,
        TimeSpan retryInterval,
        CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            bool met = false;
            try
            {
                met = await Task.Run(condition, ct).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Swallow all transient failures during polling; timeout will surface the real error
            catch (Exception) when (!ct.IsCancellationRequested)
#pragma warning restore CA1031
            {
                // Swallow transient failures
            }

            if (met)
                return;

            if (DateTime.UtcNow >= deadline)
                throw new FlawrightTimeoutException(description, timeout);

            await Task.Delay(retryInterval, ct).ConfigureAwait(false);
        }
    }
}
