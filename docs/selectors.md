# Selectors

Flawright selectors are strings that describe how to find a UIA element. They map to FlaUI `ConditionFactory` calls under the hood.

## Syntax

```
[prefix:]value
```

If no `:` is present, the entire string is treated as a `name:` match (UIA Name). If the string starts with `#`, it is treated as an AutomationId match. If the string is wrapped in `[...]`, it uses the attribute selector syntax.

## Prefix Reference

### `text:` — UIA Name

Matches elements whose `AutomationElement.Name` equals the given string. This is the default when no prefix is supplied.

```csharp
page.Locator("text:Save")      // explicit
page.Locator("Save")           // same — bare string falls back to Name match
```

Use this for labeled buttons, menu items, and static text controls.

### `name:` — UIA Name (alias)

Identical to `text:`. Use whichever reads more naturally in context.

```csharp
page.Locator("name:File")
page.Locator("name:OK")
```

### `#` — AutomationId

Matches elements by `AutomationElement.AutomationId`. The `#` must be the first character.

```csharp
page.Locator("#btn_ok")
page.Locator("#3")          // Calculator digit buttons use numeric IDs
```

AutomationIds are set by the application developer. They are stable across application runs (unlike runtime-assigned element names) and are the preferred selector for anything that has one. Check with Accessibility Insights or `inspect.exe`.

### `automationid:` — AutomationId (explicit)

Explicit form of the `#` shorthand. Prefer `#` for brevity; use this form if the ID contains special characters that could confuse parsing.

```csharp
page.Locator("automationid:btn_ok")
```

### `class:` or `classname:` — ClassName

Matches elements by `AutomationElement.ClassName`.

```csharp
page.Locator("class:Button")
page.Locator("classname:RichTextBox")
```

### `role:` — UIA ControlType

Matches by `AutomationElement.ControlType`. Use to select all controls of a given type, or when an element has no stable Name or AutomationId.

```csharp
page.Locator("role:Button")
page.Locator("role:Edit")
page.Locator("role:ListItem")
```

### `controltype:` — UIA ControlType (alias)

Identical to `role:`. Use whichever reads more naturally.

```csharp
page.Locator("controltype:Button")
page.Locator("controltype:Edit")
page.Locator("controltype:ListItem")
```

### `[attr=value]` — Attribute selector

Matches elements by a named UIA attribute. Supported attribute names: `name`, `automationid`, `class`, `classname`, `role`, `controltype`.

```csharp
page.Locator("[name=OK]")
page.Locator("[automationid=btn_save]")
page.Locator("[class=Button]")
page.Locator("[role=Button]")
page.Locator("[controltype=Edit]")
```

## Supported ControlType Values

| Selector value | UIA ControlType |
|---|---|
| `button` | `ControlType.Button` |
| `checkbox` | `ControlType.CheckBox` |
| `combobox`, `dropdown` | `ControlType.ComboBox` |
| `edit`, `textbox`, `input` | `ControlType.Edit` |
| `list` | `ControlType.List` |
| `listitem` | `ControlType.ListItem` |
| `menu` | `ControlType.Menu` |
| `menubar` | `ControlType.MenuBar` |
| `menuitem` | `ControlType.MenuItem` |
| `radiobutton` | `ControlType.RadioButton` |
| `tab` | `ControlType.Tab` |
| `tabitem` | `ControlType.TabItem` |
| `text`, `label` | `ControlType.Text` |
| `window` | `ControlType.Window` |
| `group` | `ControlType.Group` |
| `image` | `ControlType.Image` |
| `link`, `hyperlink` | `ControlType.Hyperlink` |
| `progressbar` | `ControlType.ProgressBar` |
| `scrollbar` | `ControlType.ScrollBar` |
| `slider` | `ControlType.Slider` |
| `spinner` | `ControlType.Spinner` |
| `statusbar` | `ControlType.StatusBar` |
| `table` | `ControlType.Table` |
| `toolbar` | `ControlType.ToolBar` |
| `tooltip` | `ControlType.ToolTip` |
| `tree` | `ControlType.Tree` |
| `treeitem` | `ControlType.TreeItem` |
| `separator` | `ControlType.Separator` |
| `pane` | `ControlType.Pane` |
| `document` | `ControlType.Document` |
| `header` | `ControlType.Header` |
| `headeritem` | `ControlType.HeaderItem` |

Any unrecognized value maps to `ControlType.Custom`.

## Fallback Rules

When no recognized prefix is found and the string does not start with `#` or `[`, the whole string is passed to the `name:` (Name match) branch. This means short strings like `"OK"` or `"Cancel"` resolve as Name matches without requiring the prefix.

An unknown colon-prefix (`foo:bar`) throws `ArgumentException`. The `xpath:` prefix explicitly throws `NotSupportedException` since UIA3 does not have an XPath-compatible tree.

When no element matches, `FirstAsync` throws `FlawrightTimeoutException` after the configured timeout (default 5 seconds).

## Nth Element

When a selector matches multiple elements, `FirstAsync` returns the first one in UIA tree order (depth-first). To get a specific match by index (0-based):

```csharp
// Second list item (index 1)
var item = await page.Locator("controltype:ListItem").NthAsync(1);

// Count total — no waiting, returns current count
var n = await page.Locator("controltype:Button").CountAsync();

// All matching elements — auto-waits for at least one
var all = await page.Locator("controltype:ListItem").AllAsync();
```

## Filtering

Use `Filter` to narrow down a locator by an additional predicate. The predicate receives each `IFlawrightElement` and returns `true` to keep it.

```csharp
// Only list items whose text contains "Save"
var saveItems = page.Locator("controltype:ListItem")
    .Filter(el => el.TextAsync().GetAwaiter().GetResult().Contains("Save"));

var first = await saveItems.FirstAsync();
```

Filters compose: chaining multiple `Filter` calls ANDs the predicates.

## Common Gotchas

**Localized control names**

Windows localizes the names of standard controls in some applications. A button labelled "OK" in English may be different in other locales. Prefer AutomationId over Name for stability across locales.

**Virtualized lists**

Large list controls (e.g., ListView with virtualization) may not expose off-screen items via UIA. If `CountAsync` returns fewer items than expected, scroll the list first to materialize the elements.

**Dynamic names**

Some applications change an element's Name based on state (e.g., a toggle button that reads "Mute" / "Unmute"). Use `controltype:` or `#id` selectors when the name is unstable.

**Foreground window stealing**

If another window takes focus during a test, locator resolution against a specific window still works — FlaUI searches the UIA tree rooted at the recorded `Window` element, not the active foreground window. However, `ClickAsync` synthesizes input to the element's on-screen position, so the window should be visible when clicking.

**XPath**

`xpath:` selectors throw `NotSupportedException`. UIA3 does not expose an XPath-compatible tree.
