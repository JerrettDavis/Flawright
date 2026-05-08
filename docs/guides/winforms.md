# WinForms

Windows Forms (WinForms) applications use the .NET WinForms framework, which has built-in UIA support. The key trait: a control's `AutomationId` in the UIA tree comes directly from the control's `Name` property as set in the Visual Studio designer (or in code). This makes WinForms apps straightforward to automate when the developer has given controls meaningful names.

## Launching

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = @"C:\MyApp\MyWinFormsApp.exe"
});
```

With startup arguments:

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = @"C:\MyApp\MyWinFormsApp.exe",
    Arguments = new[] { "--mode", "test" }
});
```

## How WinForms maps to UIA

| WinForms control | UIA ControlType | AutomationId source |
|---|---|---|
| `TextBox` | `Edit` | `TextBox.Name` |
| `RichTextBox` | `Document` | `RichTextBox.Name` |
| `Button` | `Button` | `Button.Name` |
| `CheckBox` | `CheckBox` | `CheckBox.Name` |
| `RadioButton` | `RadioButton` | `RadioButton.Name` |
| `ComboBox` | `ComboBox` | `ComboBox.Name` |
| `ListBox` | `List` | `ListBox.Name` |
| `ListView` | `List` | `ListView.Name` |
| `TreeView` | `Tree` | `TreeView.Name` |
| `DataGridView` | `DataGrid` | `DataGridView.Name` |
| `Label` | `Text` | `Label.Name` (UIA Name = `Label.Text`) |
| `GroupBox` | `Group` | `GroupBox.Name` |
| `TabControl` | `Tab` | `TabControl.Name` |
| `TabPage` | `TabItem` | `TabPage.Name` |
| `MenuStrip` | `MenuBar` | `MenuStrip.Name` |
| `ToolStripMenuItem` | `MenuItem` | `ToolStripMenuItem.Name` |
| `StatusStrip` | `StatusBar` | `StatusStrip.Name` |
| `ToolStrip` | `ToolBar` | `ToolStrip.Name` |
| `NumericUpDown` | `Spinner` | `NumericUpDown.Name` |
| `ProgressBar` | `ProgressBar` | `ProgressBar.Name` |
| `TrackBar` | `Slider` | `TrackBar.Name` |
| `PictureBox` | `Image` | `PictureBox.Name` |
| `Panel` | `Pane` | `Panel.Name` |
| `Form` (window) | `Window` | `Form.Name` |

> **AutomationId = `Control.Name`, not `Control.Text`**
>
> In WinForms, the UIA AutomationId comes from the control's `Name` property (the identifier used in generated code, e.g., `this.txtUsername`), not from the displayed text (`Text` property). The UIA Name property (matched by `name:` / `text:` selectors) comes from the `Text` property (what the user sees). Use `#txtUsername` to target by the designer name; use `name:Username` to target by the label text (if the accessible name is set correctly).

## Selector patterns

```csharp
// By designer Name (AutomationId) — preferred when controls have names
page.Locator("#txtUsername")     // TextBox named "txtUsername"
page.Locator("#btnLogin")        // Button named "btnLogin"
page.Locator("#cmbCountry")      // ComboBox named "cmbCountry"

// By visible text (UIA Name) — for buttons and labels
page.Locator("name:Login")       // Button with Text = "Login"
page.Locator("name:Username")    // Label with Text = "Username"

// By ControlType — when name/automationid are not set
page.Locator("controltype:Edit")         // First text box
page.Locator("controltype:Button").Nth(2) // Third button
```

## Worked example: login form

A typical WinForms login form with a username TextBox (`Name = "txtUsername"`), a password TextBox (`Name = "txtPassword"`), and a Login button (`Name = "btnLogin"`):

```csharp
using Flawright;
using Flawright.Locator;
using Xunit;

public class LoginFormTests : IAsyncLifetime
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
    public async Task ValidLogin_OpensMainWindow()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Wait for the form to be ready
        await page.Locator("#txtUsername").Expect().ToBeVisibleAsync();

        // Fill credentials
        await page.FillAsync("#txtUsername", "testuser");
        await page.FillAsync("#txtPassword", "password123");

        // Click Login
        await page.ClickAsync("#btnLogin");

        // After login, the main form should appear
        // Wait for a window whose title contains "Main"
        var mainWindow = await _fw!.Browser.WaitForPageAsync(
            "Main",
            timeout: TimeSpan.FromSeconds(10));

        Assert.NotNull(mainWindow);
    }

    [Fact]
    public async Task EmptyUsername_ShowsErrorLabel()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Leave username empty, click Login
        await page.FillAsync("#txtUsername", "");
        await page.FillAsync("#txtPassword", "password123");
        await page.ClickAsync("#btnLogin");

        // An error label should appear
        await page.Locator("#lblError").Expect().ToBeVisibleAsync();
        await page.Locator("#lblError").Expect().ToHaveTextAsync("Username is required.");
    }

    [Fact]
    public async Task CountryComboBox_HasOptions()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Select an item by value in the combo box
        await page.SelectOptionAsync("#cmbCountry", "United States");

        // Verify the selection
        var value = await page.Locator("#cmbCountry").InputValueAsync();
        Assert.Equal("United States", value);
    }

    public async Task DisposeAsync()
    {
        if (_fw != null)
            await _fw.DisposeAsync();
    }
}
```

## Gotchas

**Designer-generated names are often generic**
WinForms auto-generates control names like `button1`, `textBox1`, `label1`. If the developer never renamed them, your selectors will be `#button1` etc. — fragile if the form is modified. Ask the development team to set meaningful `Name` values, or use visible text (`name:`) selectors where possible.

**`RichTextBox` maps to `Document`, not `Edit`**
The UIA ControlType for `RichTextBox` is `Document`, not `Edit`. Use `controltype:Document` or `#richTextBoxName` to target it.

**`DataGridView` cell selection**
`DataGridView` exposes individual cells as children of the `DataGrid` element. Navigating to a specific cell requires chaining locators. The exact AutomationId structure for cells is `{column-name} Row {row-index}` — verify with Accessibility Insights on your specific app.

**Form.Name vs Form title**
The WinForms `Form.Name` is the C# identifier (`loginForm`, `mainForm`), not the window title. Flawright's `WaitForPageAsync` matches against the window *title* (the `Text` property of the Form). These are different properties.

**User controls**
User controls expose their child controls' AutomationIds relative to the user control's panel. If a user control named `ucAddress` contains a TextBox named `txtCity`, the city TextBox's AutomationId is `txtCity` (not `ucAddress.txtCity`). Scope with a parent locator if there are multiple instances of the same user control:

```csharp
var addressPanel = page.Locator("#ucAddress");
var city = addressPanel.Locator("#txtCity");
await city.FillAsync("Seattle");
```

## Related docs

- [Selectors](../selectors.md) — full selector grammar
- [Classic Win32 guide](classic-win32.md) — ClassName-based selectors (useful for older WinForms controls)
- [Multi-window apps guide](multi-window.md) — dialog handling
