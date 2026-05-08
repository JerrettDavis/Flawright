namespace JerrettDavis.Flawright.Internals;

/// <summary>
/// Tolerant wrappers around <see cref="System.Diagnostics.Process"/> queries
/// that can throw <see cref="InvalidOperationException"/> when the underlying
/// process handle has been disposed or was never associated with a real process
/// (e.g. an AppExecutionAlias stub that exited immediately).
///
/// Dispose paths must never throw, so all callers on those paths go through
/// this class.
/// </summary>
internal static class SafeProcessQueries
{
    /// <summary>
    /// Evaluates <paramref name="readHasExited"/> and returns the result.
    /// Returns <see langword="true"/> if the delegate throws
    /// <see cref="InvalidOperationException"/> (process handle gone = exited).
    /// </summary>
    /// <param name="readHasExited">Delegate that reads the underlying HasExited property.</param>
    /// <returns>
    /// The value returned by <paramref name="readHasExited"/>, or
    /// <see langword="true"/> when any exception is thrown.
    /// </returns>
    internal static bool HasExitedSafe(Func<bool> readHasExited)
    {
        try
        {
            return readHasExited();
        }
        catch (InvalidOperationException)
        {
            // Process handle was disposed or was never associated with a real
            // process — treat as exited (the safe, conservative default).
            return true;
        }
#pragma warning disable CA1031 // Tolerant wrapper: any unexpected failure = treat as exited
        catch (Exception)
        {
            return true;
        }
#pragma warning restore CA1031
    }
}
