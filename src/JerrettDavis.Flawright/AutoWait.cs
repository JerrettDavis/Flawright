// This file is a shim that re-exports the AutoWait type from its new home in
// JerrettDavis.Flawright.Internals. It exists so that existing files in the
// JerrettDavis.Flawright namespace can continue to reference AutoWait without
// a using directive while Wave C/D migrates the call sites.
// Wave C/D: Remove this shim once all references are updated to use
// JerrettDavis.Flawright.Internals.AutoWait directly.

namespace JerrettDavis.Flawright;

/// <summary>
/// Re-export shim. See <see cref="Internals.AutoWait"/> for the actual implementation.
/// </summary>
internal static class AutoWait
{
    internal static Task<T> UntilAsync<T>(
        Func<T?> condition,
        string selector,
        TimeSpan timeout,
        TimeSpan retryInterval,
        CancellationToken ct)
        where T : class
    {
        return Internals.AutoWait.UntilAsync(condition, selector, timeout, retryInterval, ct);
    }

    internal static Task UntilTrueAsync(
        Func<bool> condition,
        string description,
        TimeSpan timeout,
        TimeSpan retryInterval,
        CancellationToken ct)
    {
        return Internals.AutoWait.UntilTrueAsync(condition, description, timeout, retryInterval, ct);
    }

    internal static Task<T> UntilAsync<T>(
        Func<CancellationToken, Task<T?>> condition,
        string selector,
        TimeSpan timeout,
        TimeSpan retryInterval,
        CancellationToken ct)
        where T : class
    {
        return Internals.AutoWait.UntilAsync(condition, selector, timeout, retryInterval, ct);
    }

    internal static Task UntilTrueAsync(
        Func<CancellationToken, Task<bool>> condition,
        string description,
        TimeSpan timeout,
        TimeSpan retryInterval,
        CancellationToken ct)
    {
        return Internals.AutoWait.UntilTrueAsync(condition, description, timeout, retryInterval, ct);
    }
}
