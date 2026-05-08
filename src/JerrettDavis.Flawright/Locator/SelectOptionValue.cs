namespace JerrettDavis.Flawright.Locator;

/// <summary>
/// Describes an option to select in a combobox or listbox.
/// Exactly one of <see cref="Label"/>, <see cref="Index"/>, or
/// <see cref="Value"/> should be provided.
/// </summary>
/// <param name="Label">The visible label of the option (matches element <c>Name</c>).</param>
/// <param name="Index">Zero-based index of the option.</param>
/// <param name="Value">The value attribute / AutomationId of the option.</param>
public sealed record SelectOptionValue(
    string? Label = null,
    int? Index = null,
    string? Value = null);
