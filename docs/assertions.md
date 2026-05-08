# Assertions

Flawright assertions are accessed via `locator.Expect()`, which returns a `FlawrightAssertions` instance. Each assertion method auto-waits and throws `FlawrightTimeoutException` on timeout (or `AssertionException` for count assertions). All methods are async.

## `ToBeVisibleAsync`

Passes when the element is present in the UIA tree and not offscreen (`IsOffscreen == false`).

```csharp
await page.Locator("name:Submit").Expect().ToBeVisibleAsync();
```

Fails with: `FlawrightTimeoutException: Expected element 'name:Submit' to be visible`

## `ToBeHiddenAsync`

Passes when the element is either absent from the UIA tree or offscreen (`IsOffscreen == true`).

```csharp
await page.Locator("name:LoadingSpinner").Expect().ToBeHiddenAsync();
```

Fails with: `FlawrightTimeoutException: Expected element 'name:LoadingSpinner' to be hidden`

## `ToBeEnabledAsync`

Passes when the element's `IsEnabled` property is `true`.

```csharp
await page.Locator("name:OK").Expect().ToBeEnabledAsync();
```

Useful for asserting that a button becomes clickable after form validation completes.

Fails with: `FlawrightTimeoutException: Expected element 'name:OK' to be enabled`

## `ToBeDisabledAsync`

Passes when `IsEnabled` is `false`.

```csharp
await page.Locator("name:Delete").Expect().ToBeDisabledAsync();
```

Fails with: `FlawrightTimeoutException: Expected element 'name:Delete' to be disabled`

## `ToHaveTextAsync`

Passes when the element's text content equals the expected string (exact match, case-sensitive).

Text resolution order:
1. `ValuePattern.Value` — for edit/input controls
2. `TextPattern.DocumentRange.GetText(-1)` — for document and rich-text controls
3. `AutomationElement.Name` — fallback for labels, buttons, etc.

```csharp
await page.Locator("#result").Expect().ToHaveTextAsync("42");
```

Fails with: `FlawrightTimeoutException: Expected element '#result' to have text '42'`

## `ToHaveValueAsync`

Passes when the element's `ValuePattern.Value` equals the expected string (exact match, case-sensitive). Use this for edit controls when you specifically want the value pattern rather than the name fallback.

```csharp
await page.Locator("controltype:Edit").Expect().ToHaveValueAsync("hello world");
```

Fails with: `FlawrightTimeoutException: Expected element 'controltype:Edit' to have value 'hello world'`

## `ToBeCheckedAsync`

Passes when the element's `TogglePattern.ToggleState` is `On`. Use for checkboxes and toggle buttons.

```csharp
await page.Locator("controltype:CheckBox").Expect().ToBeCheckedAsync();
```

Fails with: `FlawrightTimeoutException: Expected element 'controltype:CheckBox' to be checked`

## `ToHaveCountAsync`

Passes when the number of elements matching the locator equals the expected count. This assertion does **not** auto-wait; it checks the current count immediately.

```csharp
await page.Locator("controltype:ListItem").Expect().ToHaveCountAsync(5);
```

Fails with: `AssertionException: Expected 5 elements for 'controltype:ListItem' but found 3`

## Negation — the `Not` modifier

Every positive assertion has a `Not` counterpart accessible via `locator.Expect().Not`:

```csharp
// Not visible (absent or offscreen)
await page.Locator("name:Spinner").Expect().Not.ToBeVisibleAsync();

// Not hidden (is visible)
await page.Locator("name:Content").Expect().Not.ToBeHiddenAsync();

// Not enabled (is disabled)
await page.Locator("name:Delete").Expect().Not.ToBeEnabledAsync();

// Not disabled (is enabled)
await page.Locator("name:Save").Expect().Not.ToBeDisabledAsync();

// Text is not the given value
await page.Locator("#counter").Expect().Not.ToHaveTextAsync("0");

// Value is not the given string
await page.Locator("controltype:Edit").Expect().Not.ToHaveValueAsync("old");

// Not checked
await page.Locator("controltype:CheckBox").Expect().Not.ToBeCheckedAsync();
```

All `Not` assertions auto-wait (except there is no `Not.ToHaveCountAsync` — use `CountAsync` + manual assertion for that case).

## Composing assertions

Multiple assertions on the same locator are independent calls. Each one re-resolves the element:

```csharp
var saveButton = page.Locator("name:Save");

await saveButton.Expect().ToBeVisibleAsync();
await saveButton.Expect().ToBeEnabledAsync();
```

## Custom assertions

For assertions not in the built-in set, retrieve the element directly and assert with your test framework:

```csharp
var text = await page.Locator("name:Title").InnerTextAsync();
Assert.StartsWith("Welcome", text);
```

## Exception types

| Assertion | Thrown on failure |
|---|---|
| `ToBeVisibleAsync` | `FlawrightTimeoutException` (after timeout) |
| `ToBeHiddenAsync` | `FlawrightTimeoutException` (after timeout) |
| `ToBeEnabledAsync` | `FlawrightTimeoutException` (after timeout) |
| `ToBeDisabledAsync` | `FlawrightTimeoutException` (after timeout) |
| `ToHaveTextAsync` | `FlawrightTimeoutException` (after timeout) |
| `ToHaveValueAsync` | `FlawrightTimeoutException` (after timeout) |
| `ToBeCheckedAsync` | `FlawrightTimeoutException` (after timeout) |
| `ToHaveCountAsync` | `AssertionException` (immediate) |
| `Not.*` | `FlawrightTimeoutException` (after timeout) |

xUnit, NUnit, and MSTest all treat unexpected exceptions as test failures, so no special adapter is needed.

`FlawrightTimeoutException` inherits from `TimeoutException`; `AssertionException` inherits from `Exception`.
