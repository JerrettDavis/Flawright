namespace Flawright;

/// <summary>
/// Options for attaching to an already-running process via
/// <see cref="Flawright.AttachAsync(AttachOptions, FlawrightOptions?, CancellationToken)"/>.
/// Exactly one of <see cref="ProcessId"/> or <see cref="ProcessName"/> must be set.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="ProcessId"/> when you know the exact OS process ID (most
/// deterministic — no ambiguity when multiple instances are running).
/// </para>
/// <para>
/// Use <see cref="ProcessName"/> to attach by process basename (without
/// <c>.exe</c> extension).  When multiple instances are running, use
/// <see cref="Index"/> to select the zero-based instance (ordered by PID ascending).
/// </para>
/// </remarks>
/// <example>
/// Attach by PID:
/// <code>
/// var opts = new AttachOptions { ProcessId = 12345 };
/// </code>
/// Attach by name (second instance):
/// <code>
/// var opts = new AttachOptions { ProcessName = "notepad", Index = 1 };
/// </code>
/// Attach by name with <c>.exe</c> suffix (stripped automatically):
/// <code>
/// var opts = new AttachOptions { ProcessName = "notepad.exe" };
/// </code>
/// </example>
public sealed record AttachOptions
{
    /// <summary>
    /// The OS process ID to attach to.
    /// Mutually exclusive with <see cref="ProcessName"/> — exactly one must be set.
    /// </summary>
    public int? ProcessId { get; init; }

    /// <summary>
    /// Process base name (with or without the <c>.exe</c> suffix) to attach to,
    /// e.g. <c>"notepad"</c> or <c>"notepad.exe"</c>.
    /// The <c>.exe</c> suffix is stripped automatically before matching.
    /// Mutually exclusive with <see cref="ProcessId"/> — exactly one must be set.
    /// </summary>
    public string? ProcessName { get; init; }

    /// <summary>
    /// Zero-based index among matching processes (ordered by PID ascending)
    /// when <see cref="ProcessName"/> is set.  Defaults to <c>0</c> (first instance).
    /// Ignored when <see cref="ProcessId"/> is used.
    /// </summary>
    public int Index { get; init; }
}
