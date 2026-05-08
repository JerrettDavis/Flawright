using FlaUI.Core.Definitions;

namespace Flawright.Selectors;

/// <summary>
/// Maps <see cref="AriaRole"/> values to FlaUI <see cref="ControlType"/> values.
/// </summary>
/// <remarks>
/// Only roles that have a meaningful UIA/FlaUI equivalent are supported.
/// Web-only semantic roles (landmark regions, typographic roles, etc.) throw
/// <see cref="NotSupportedException"/>. Do not silently fall back to
/// <see cref="ControlType.Custom"/> — that produces spurious matches.
/// </remarks>
internal static class AriaRoleMapper
{
    /// <summary>
    /// Attempts to map an <see cref="AriaRole"/> to a FlaUI <see cref="ControlType"/>.
    /// </summary>
    /// <param name="role">The ARIA role to map.</param>
    /// <param name="controlType">
    /// When this method returns <see langword="true"/>, contains the mapped
    /// <see cref="ControlType"/>; otherwise, the value is undefined.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a mapping exists; <see langword="false"/> for
    /// web-only roles that have no UIA equivalent.
    /// </returns>
    public static bool TryMap(AriaRole role, out ControlType controlType)
    {
        controlType = role switch
        {
            // ── Mapped roles ─────────────────────────────────────────────────

            AriaRole.Alert => ControlType.StatusBar,
            AriaRole.Alertdialog => ControlType.Window,
            AriaRole.Button => ControlType.Button,
            AriaRole.Checkbox => ControlType.CheckBox,
            AriaRole.Columnheader => ControlType.HeaderItem,
            AriaRole.Combobox => ControlType.ComboBox,
            AriaRole.Dialog => ControlType.Window,
            AriaRole.Document => ControlType.Document,
            AriaRole.Form => ControlType.Group,
            AriaRole.Generic => ControlType.Pane,
            AriaRole.Grid => ControlType.Table,
            AriaRole.Group => ControlType.Group,
            AriaRole.Heading => ControlType.Text,
            AriaRole.Img => ControlType.Image,
            AriaRole.Link => ControlType.Hyperlink,
            AriaRole.List => ControlType.List,
            AriaRole.Listbox => ControlType.List,
            AriaRole.Listitem => ControlType.ListItem,
            AriaRole.Log => ControlType.StatusBar,
            AriaRole.Menu => ControlType.Menu,
            AriaRole.Menubar => ControlType.MenuBar,
            AriaRole.Menuitem => ControlType.MenuItem,
            AriaRole.Menuitemcheckbox => ControlType.MenuItem,
            AriaRole.Menuitemradio => ControlType.MenuItem,
            AriaRole.None => ControlType.Pane,
            AriaRole.Option => ControlType.ListItem,
            AriaRole.Presentation => ControlType.Pane,
            AriaRole.Progressbar => ControlType.ProgressBar,
            AriaRole.Radio => ControlType.RadioButton,
            AriaRole.Radiogroup => ControlType.Group,
            AriaRole.Rowheader => ControlType.HeaderItem,
            AriaRole.Scrollbar => ControlType.ScrollBar,
            AriaRole.Searchbox => ControlType.Edit,
            AriaRole.Separator => ControlType.Separator,
            AriaRole.Slider => ControlType.Slider,
            AriaRole.Spinbutton => ControlType.Spinner,
            AriaRole.Status => ControlType.StatusBar,
            AriaRole.Switch => ControlType.CheckBox,
            AriaRole.Tab => ControlType.TabItem,
            AriaRole.Table => ControlType.Table,
            AriaRole.Tablist => ControlType.Tab,
            AriaRole.Tabpanel => ControlType.Pane,
            AriaRole.Textbox => ControlType.Edit,
            AriaRole.Toolbar => ControlType.ToolBar,
            AriaRole.Tooltip => ControlType.ToolTip,
            AriaRole.Tree => ControlType.Tree,
            AriaRole.Treeitem => ControlType.TreeItem,

            // ── Unsupported (web-only) roles — return false ───────────────────
            _ => (ControlType)(-1),
        };

        if ((int)controlType == -1)
        {
            controlType = default;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Maps an <see cref="AriaRole"/> to a FlaUI <see cref="ControlType"/>.
    /// </summary>
    /// <param name="role">The ARIA role to map.</param>
    /// <returns>The corresponding <see cref="ControlType"/>.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="role"/> has no FlaUI <see cref="ControlType"/> equivalent.
    /// Use a different role or supply a custom selector.
    /// </exception>
    public static ControlType Map(AriaRole role)
    {
        if (TryMap(role, out var controlType))
        {
            return controlType;
        }

        throw new NotSupportedException(
            $"AriaRole.{role} has no FlaUI ControlType equivalent. Use a different role or supply a custom selector.");
    }
}
