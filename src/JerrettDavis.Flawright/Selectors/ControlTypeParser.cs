using FlaUI.Core.Definitions;

namespace JerrettDavis.Flawright.Selectors;

/// <summary>
/// Parses a selector control-type string into a <see cref="ControlType"/>.
///
/// <para>
/// Supports both exact FlaUI enum names (case-insensitive) and the set of
/// documented aliases listed in <c>docs/selectors.md</c>:
/// <list type="bullet">
///   <item><term>dropdown</term><description>→ ControlType.ComboBox</description></item>
///   <item><term>textbox, input</term><description>→ ControlType.Edit</description></item>
///   <item><term>label</term><description>→ ControlType.Text</description></item>
///   <item><term>hyperlink</term><description>→ ControlType.Hyperlink</description></item>
/// </list>
/// Unrecognised values throw <see cref="ArgumentException"/>.
/// </para>
/// </summary>
internal static class ControlTypeParser
{
    // Aliases for documented selector values that don't match FlaUI enum names exactly.
    // All other documented values (Button, CheckBox, Edit, …) are exact enum names
    // already handled by Enum.TryParse below.
    private static readonly Dictionary<string, ControlType> Aliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "dropdown",  ControlType.ComboBox   },
            { "textbox",   ControlType.Edit        },
            { "input",     ControlType.Edit        },
            { "label",     ControlType.Text        },
            { "hyperlink", ControlType.Hyperlink   },
        };

    /// <summary>
    /// Parses <paramref name="value"/> into a <see cref="ControlType"/>.
    /// </summary>
    /// <param name="value">The selector control-type string (e.g. "Button", "dropdown").</param>
    /// <returns>The matching <see cref="ControlType"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> does not match any known alias or enum member.
    /// </exception>
    internal static ControlType Parse(string value)
    {
        if (Aliases.TryGetValue(value, out var aliased))
            return aliased;

        if (Enum.TryParse<ControlType>(value, ignoreCase: true, out var ct))
            return ct;

        throw new ArgumentException(
            $"'{value}' is not a recognised ControlType. " +
            $"Use a valid FlaUI ControlType name (e.g. Button, Edit, List) or a documented alias " +
            $"(dropdown, textbox, input, label, hyperlink).",
            nameof(value));
    }
}
