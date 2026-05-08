# WPF

Windows Presentation Foundation (WPF) applications have first-class UIA support. AutomationId can be set in XAML via `x:Name` or `AutomationProperties.AutomationId`, and WPF control templates expose a richer UIA tree than Win32. This makes WPF good to automate, but the logical vs. visual tree distinction requires some care.

## Launching

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = @"C:\MyApp\MyWpfApp.exe"
});
```

## How WPF sets AutomationId

WPF has two ways to set a control's UIA AutomationId:

**1. `x:Name`** (the most common approach)

```xml
<TextBox x:Name="txtSearch" />
<Button x:Name="btnSearch" Content="Search" />
```

The `x:Name` becomes the AutomationId automatically. Use `#txtSearch` / `#btnSearch`.

**2. `AutomationProperties.AutomationId`** (explicit override)

```xml
<Button AutomationProperties.AutomationId="searchButton" Content="Search" />
```

Use this when `x:Name` conflicts with code-behind binding names or to give a control a cleaner automation identifier. Use `#searchButton`.

**3. No AutomationId set**

Controls without `x:Name` or `AutomationProperties.AutomationId` have an empty string AutomationId. Rely on `name:` (UIA Name — usually the visible text), `controltype:`, or chained locators to narrow to the right element.

## WPF control → UIA ControlType mapping

| WPF control | UIA ControlType | Notes |
|---|---|---|
| `TextBox` | `Edit` | |
| `RichTextBox` | `Document` | |
| `Button` | `Button` | |
| `CheckBox` | `CheckBox` | |
| `RadioButton` | `RadioButton` | |
| `ComboBox` | `ComboBox` | |
| `ListBox` | `List` | |
| `ListView` | `List` | Items are `ListItem` |
| `TreeView` | `Tree` | Items are `TreeItem` |
| `DataGrid` | `DataGrid` | |
| `Label` | `Text` | UIA Name = `Label.Content` |
| `TextBlock` | `Text` | Not interactive; read-only |
| `Border` | `Pane` | Layout-only container |
| `Grid` / `StackPanel` / `DockPanel` | `Pane` | Layout containers |
| `GroupBox` | `Group` | |
| `TabControl` | `Tab` | |
| `TabItem` | `TabItem` | |
| `Menu` / `MenuBar` | `MenuBar` | |
| `MenuItem` | `MenuItem` | |
| `ProgressBar` | `ProgressBar` | |
| `Slider` | `Slider` | |
| `ScrollViewer` | `Pane` | |
| `Window` | `Window` | |

## Selector patterns

```csharp
// By x:Name / AutomationId — preferred
page.Locator("#txtSearch")
page.Locator("#btnSearch")
page.Locator("#lstResults")

// By visible text (UIA Name)
page.Locator("name:Search")    // Button with Content = "Search"
page.Locator("name:Cancel")

// By ControlType
page.Locator("controltype:Edit")            // First text box
page.Locator("controltype:Button").Nth(0)   // First button

// Scoped — search within a named container
var sidebar = page.Locator("#sidebarPanel");
var navItem = sidebar.Locator("controltype:ListItem").Nth(2);

// Chained with >>
page.Locator("#mainContent >> controltype:Button")
```

## Worked example: search form

A WPF window with a search TextBox (`x:Name="txtSearch"`), a Search button (`x:Name="btnSearch"`), and a results list (`x:Name="lstResults"`):

```csharp
using Flawright;
using Flawright.Locator;
using Xunit;

public class WpfSearchTests : IAsyncLifetime
{
    private Flawright? _fw;

    public async Task InitializeAsync()
    {
        _fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = @"C:\MyApp\MyApp.exe"
        });
    }

    [Fact]
    public async Task Search_ReturnsResults()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.FillAsync("#txtSearch", "flawright");
        await page.ClickAsync("#btnSearch");

        // Wait for at least one result
        await page.Locator("#lstResults >> controltype:ListItem")
            .Expect()
            .ToBeVisibleAsync();

        var count = await page.Locator("#lstResults >> controltype:ListItem").CountAsync();
        Assert.True(count > 0);
    }

    [Fact]
    public async Task SearchWithEmptyQuery_ButtonIsDisabled()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Clear the search box
        await page.FillAsync("#txtSearch", "");

        // Button should be disabled if the app disables it on empty input
        await page.Locator("#btnSearch").Expect().ToBeDisabledAsync();
    }

    [Fact]
    public async Task TabControl_SwitchesView()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Click a tab item by its header text
        await page.ClickAsync("name:Advanced");

        // The Advanced panel should now be visible
        await page.Locator("#advancedPanel").Expect().ToBeVisibleAsync();
    }

    public async Task DisposeAsync()
    {
        if (_fw != null)
            await _fw.DisposeAsync();
    }
}
```

## Visual tree vs UIA tree

WPF's UIA tree is derived from the *logical* tree, not the full visual tree. This has several implications:

- **Layout containers** (`Grid`, `StackPanel`, `DockPanel`, `WrapPanel`) appear as `Pane` elements in the UIA tree but may be omitted entirely if they have no accessible children.
- **`Border`** is always exposed as `Pane`; it is rarely useful as a selector anchor.
- **Control templates** — a WPF button's default template includes a `Border`, a `ContentPresenter`, and the button's content. UIA flattens this: the `Button` element's Name comes from its `Content`, not the template's inner elements.
- **`TextBlock`** inside a `DataTemplate` or item template may or may not have an accessible name depending on how the template is written. If `TextBlock.Text` is bound to data, the UIA Name is the bound value.
- **Virtualized `ItemsControl`** (e.g., `ListView` with `VirtualizingStackPanel`) — off-screen items are not materialized in the visual tree and therefore not in the UIA tree. Scroll the control to bring items into view.

Use Accessibility Insights to explore the live UIA tree — it shows exactly what Flawright sees, not the XAML structure.

## Gotchas

**`TextBlock` is `Text`, not `Edit`**
`TextBlock` is a read-only display control. Its UIA ControlType is `Text`. Don't use `FillAsync` on it — it is not editable. Use `InnerTextAsync` to read its content.

**Custom controls with no UIA peer**
WPF custom controls that don't provide a custom `AutomationPeer` fall back to `FrameworkElementAutomationPeer`, which exposes a `Pane` with no useful properties. Ask the development team to implement a proper automation peer.

**`Expander` control**
`Expander` appears as `Group` in the UIA tree. Its header text becomes the group Name. Its content is only accessible when the expander is open — if a test needs to interact with content inside an expander, click the expander header first.

**`DataGrid` cells**
DataGrid cell AutomationIds follow a row-column pattern, but the exact format depends on the control and version. Use Accessibility Insights to verify cell identifiers on your specific application.

**Binding errors**
If data binding throws an exception and the control falls back to a placeholder, its UIA Name may change. Keep test data clean.

## Related docs

- [Selectors](../selectors.md) — full grammar including `>>` combinator
- [WinUI 3 guide](winui3.md) — for modern WinUI3 apps (different from WPF despite visual similarity)
- [Troubleshooting](../troubleshooting.md) — virtualized list and element-not-found patterns
