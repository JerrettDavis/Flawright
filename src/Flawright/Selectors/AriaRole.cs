namespace Flawright.Selectors;

/// <summary>
/// ARIA roles mirroring Playwright's <c>AriaRole</c> enum.
/// Use with <c>IFlawrightLocator.GetByRole</c>.
///
/// Roles that have no meaningful UIA equivalent (e.g. web-only semantic
/// roles) will throw <see cref="NotSupportedException"/> when used.
/// See <c>AriaRoleMapper</c> for the full mapping table.
/// </summary>
public enum AriaRole
{
    /// <summary>A type of live region with important, and usually time-sensitive, information.</summary>
    Alert,

    /// <summary>A modal alert dialog.</summary>
    Alertdialog,

    /// <summary>A structure containing one or more focusable elements requiring user input.</summary>
    Application,

    /// <summary>A section of a page that provides summary information.</summary>
    Article,

    /// <summary>A banner landmark region.</summary>
    Banner,

    /// <summary>A block-level quotation.</summary>
    Blockquote,

    /// <summary>A clickable button widget.</summary>
    Button,

    /// <summary>The caption of a table or grid.</summary>
    Caption,

    /// <summary>A cell in a table.</summary>
    Cell,

    /// <summary>A checkable input.</summary>
    Checkbox,

    /// <summary>A header for a column of a table.</summary>
    Columnheader,

    /// <summary>An inline code snippet.</summary>
    Code,

    /// <summary>A listbox that allows selecting from a set of choices.</summary>
    Combobox,

    /// <summary>A complementary landmark region.</summary>
    Complementary,

    /// <summary>A footer landmark region (content info).</summary>
    Contentinfo,

    /// <summary>A definition of a term.</summary>
    Definition,

    /// <summary>Content that has been deleted.</summary>
    Deletion,

    /// <summary>A dialog window.</summary>
    Dialog,

    /// <summary>A directory listing.</summary>
    Directory,

    /// <summary>A document or application.</summary>
    Document,

    /// <summary>Emphasis text.</summary>
    Emphasis,

    /// <summary>A scrollable list of articles.</summary>
    Feed,

    /// <summary>A figure with optional caption.</summary>
    Figure,

    /// <summary>A form element.</summary>
    Form,

    /// <summary>A generic unnamed container element.</summary>
    Generic,

    /// <summary>A cell containing header information for a row or column of a grid.</summary>
    Gridcell,

    /// <summary>A composite widget containing a collection of items.</summary>
    Grid,

    /// <summary>A group of UI objects.</summary>
    Group,

    /// <summary>A heading for a section.</summary>
    Heading,

    /// <summary>An image.</summary>
    Img,

    /// <summary>An inserted piece of content.</summary>
    Insertion,

    /// <summary>A hyperlink.</summary>
    Link,

    /// <summary>A list of items.</summary>
    List,

    /// <summary>A listbox widget.</summary>
    Listbox,

    /// <summary>A single item in a list.</summary>
    Listitem,

    /// <summary>A log of activity.</summary>
    Log,

    /// <summary>A main content landmark region.</summary>
    Main,

    /// <summary>A marquee scrolling widget.</summary>
    Marquee,

    /// <summary>A math expression.</summary>
    Math,

    /// <summary>A gauge / level indicator.</summary>
    Meter,

    /// <summary>A set of menu items.</summary>
    Menu,

    /// <summary>A container for a set of menus.</summary>
    Menubar,

    /// <summary>A menu item.</summary>
    Menuitem,

    /// <summary>A checkable menu item.</summary>
    Menuitemcheckbox,

    /// <summary>A radio menu item.</summary>
    Menuitemradio,

    /// <summary>A navigation landmark region.</summary>
    Navigation,

    /// <summary>No corresponding role (generic/presentation).</summary>
    None,

    /// <summary>A note widget.</summary>
    Note,

    /// <summary>A selectable option in a listbox or combobox.</summary>
    Option,

    /// <summary>A paragraph of text.</summary>
    Paragraph,

    /// <summary>A presentation (non-interactive) container.</summary>
    Presentation,

    /// <summary>A progress indicator.</summary>
    Progressbar,

    /// <summary>A radio button widget.</summary>
    Radio,

    /// <summary>A group of radio buttons.</summary>
    Radiogroup,

    /// <summary>A landmark region.</summary>
    Region,

    /// <summary>A row in a table or grid.</summary>
    Row,

    /// <summary>A row group in a table.</summary>
    Rowgroup,

    /// <summary>A header cell for a row of a table.</summary>
    Rowheader,

    /// <summary>A scrollbar widget.</summary>
    Scrollbar,

    /// <summary>A search landmark region.</summary>
    Search,

    /// <summary>A search input widget.</summary>
    Searchbox,

    /// <summary>A separator element.</summary>
    Separator,

    /// <summary>A slider widget.</summary>
    Slider,

    /// <summary>A spinner / numeric input.</summary>
    Spinbutton,

    /// <summary>A status bar.</summary>
    Status,

    /// <summary>Strong emphasis text.</summary>
    Strong,

    /// <summary>Subscript text.</summary>
    Subscript,

    /// <summary>Superscript text.</summary>
    Superscript,

    /// <summary>An on/off switch widget.</summary>
    Switch,

    /// <summary>A tab in a tab panel.</summary>
    Tab,

    /// <summary>A grid-style table.</summary>
    Table,

    /// <summary>A list of tabs.</summary>
    Tablist,

    /// <summary>A tab panel (content area of a tab).</summary>
    Tabpanel,

    /// <summary>A definition term.</summary>
    Term,

    /// <summary>A text input widget.</summary>
    Textbox,

    /// <summary>An inline time value.</summary>
    Time,

    /// <summary>A countdown timer.</summary>
    Timer,

    /// <summary>A toolbar widget.</summary>
    Toolbar,

    /// <summary>A tooltip widget.</summary>
    Tooltip,

    /// <summary>A tree widget.</summary>
    Tree,

    /// <summary>A treegrid (tree + grid combined) widget.</summary>
    Treegrid,

    /// <summary>An item in a tree widget.</summary>
    Treeitem
}
