using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Flawright.Internals;

internal static class ProcessAttachRetry
{
    private const int ErrorPartialCopy = 299;

    /// <summary>
    /// Invokes <paramref name="attach"/> with exponential-backoff retry on
    /// <see cref="Win32Exception"/> with NativeErrorCode 299 (ERROR_PARTIAL_COPY).
    /// FlaUI's internal GetMainModuleFilepath diagnostic call re-enumerates
    /// process modules immediately after launch; under CI load the kernel module
    /// table can be transiently unreadable even after ProcessReadyGuard releases.
    /// </summary>
    internal static T Invoke<T>(
        Func<T> attach,
        int maxAttempts = 5,
        int initialDelayMs = 10,
        Action<int>? sleep = null)
    {
        var delayMs = initialDelayMs;
        var doSleep = sleep ?? Thread.Sleep;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return attach();
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorPartialCopy
                                            && attempt < maxAttempts)
            {
                doSleep(delayMs);
                delayMs *= 2;
            }
        }

        throw ThrowUnreachable();
    }

    [ExcludeFromCodeCoverage(Justification = "Defensive unreachable sentinel; the retry loop always returns or rethrows the underlying Win32Exception on its final attempt.")]
    private static Exception ThrowUnreachable()
        => throw new InvalidOperationException("Unreachable.");
}
