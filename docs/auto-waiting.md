# Auto-Waiting

## Why it exists

Desktop applications have the same race conditions as web applications: a button may not be clickable the moment the window opens, a dialog may take a moment to appear after a trigger, a list may populate asynchronously. Playwright proved that building the wait loop into the framework — rather than leaving it to every test author — produces tests that are more reliable and shorter.

Flawright follows the same principle. Actions and assertions that need an element to exist block until the element appears, up to a configurable timeout, before failing.

## How it works

Internally, Flawright uses a polling loop (`AutoWait.UntilAsync` / `AutoWait.UntilTrueAsync`) that:

1. Checks the condition (element present, element visible, text matches, etc.)
2. If the condition is not yet met, sleeps for the retry interval and tries again
3. Continues until the condition is met or the deadline is exceeded
4. Throws `FlawrightTimeoutException` when the deadline is exceeded

The polling loop also catches transient exceptions from FlaUI (e.g., element becoming stale during a redraw) and retries silently.

## Default values

| Setting | Default |
|---|---|
| `DefaultTimeout` | **5 seconds** |
| `DefaultRetryInterval` | **100 ms** |

Configure these via `FlawrightOptions`:

```csharp
await using var fw = await Flawright.LaunchAsync(
    new LaunchOptions { ApplicationPath = "notepad.exe" },
    new FlawrightOptions
    {
        DefaultTimeout       = TimeSpan.FromSeconds(10),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(50)
    });
```

## Overriding per-call

Every locator and page action accepts an optional `timeout` parameter and a `CancellationToken`:

```csharp
using JerrettDavis.Flawright.Locator; // for LocatorWaitForOptions

// Wait up to 30 seconds for a specific element
var locator = page.Locator("name:Loading Complete");
await locator.WaitForAsync(new LocatorWaitForOptions { Timeout = TimeSpan.FromSeconds(30) });

// Use a CancellationToken to cancel early
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await page.Locator("name:Dialog").Expect()
    .ToBeVisibleAsync(ct: cts.Token);
```

## What gets auto-waited

| Operation | Auto-waited |
|---|---|
| `locator.First` / `locator.Nth(n)` | Sync narrowing — auto-wait happens when an action is called on the resulting locator |
| `locator.AllAsync()` | Yes — retries until at least one element exists |
| `locator.CountAsync()` | No — returns current count immediately |
| `page.ClickAsync(selector)` | Yes — waits for element, then clicks |
| `page.FillAsync(selector, text)` | Yes — waits for element, then fills |
| `page.TypeAsync(selector, text)` | Yes — waits for element, then types |
| `page.PressAsync(selector, key)` | Yes — waits for element, then sends key |
| `page.CheckAsync(selector)` | Yes — waits for element, then checks |
| `page.UncheckAsync(selector)` | Yes — waits for element, then unchecks |
| `page.SelectOptionAsync(selector, value)` | Yes — waits for element, then selects |
| `page.WaitForSelectorAsync(selector)` | Yes — waits for the element to appear |
| `browser.WaitForPageAsync(title)` | Yes — polls until window with matching title appears |
| `expect.ToBeVisibleAsync()` | Yes — retries until visible |
| `expect.ToBeHiddenAsync()` | Yes — retries until hidden |
| `expect.ToBeEnabledAsync()` | Yes — retries until enabled |
| `expect.ToBeDisabledAsync()` | Yes — retries until disabled |
| `expect.ToHaveTextAsync(text)` | Yes — retries until text matches |
| `expect.ToHaveValueAsync(value)` | Yes — retries until value matches |
| `expect.ToBeCheckedAsync()` | Yes — retries until checked |
| `expect.ToHaveCountAsync(n)` | No — checks current count, throws immediately on mismatch |
| `element.ClickAsync()` | No — element already resolved |
| `element.FillAsync(text)` | No — element already resolved |
| `page.ScreenshotAsync()` | No — captures current state |

## Timeout exception

When the deadline is exceeded, `FlawrightTimeoutException` is thrown. It carries the selector string and the elapsed duration:

```csharp
using JerrettDavis.Flawright.Locator; // for LocatorWaitForOptions

try
{
    await page.Locator("#save").WaitForAsync(new LocatorWaitForOptions { Timeout = TimeSpan.FromSeconds(2) });
}
catch (FlawrightTimeoutException ex)
{
    Console.WriteLine(ex.Message);    // "Locator '#save' not found within 00:00:02."
    Console.WriteLine(ex.Selector);   // "#save"
    Console.WriteLine(ex.Timeout);    // 00:00:02
}
```

`FlawrightTimeoutException` inherits from `TimeoutException`.

## Best practices

**Prefer locator actions over element actions for top-level interaction.** `page.ClickAsync("name:OK")` auto-waits for the element. If you need to interact with a specific index, use `locator.Nth(n).ClickAsync()` — the auto-wait fires when the action is called on the narrowed locator.

**Use `Expect()` assertions instead of manual element state checks.** `expect.ToBeVisibleAsync()` retries until visible; a raw `element.IsVisibleAsync()` check in `Assert.True(...)` does not.

**Set tighter timeouts in CI.** A test that waits 10 seconds by default can mask a genuine bug. In CI, override to 5 seconds (or leave the 5-second default) so flakiness surfaces quickly.

**Don't hardcode `Task.Delay`.** Hardcoded sleeps hide timing problems and slow down the suite. Rely on the auto-waiting built into every locator action.
