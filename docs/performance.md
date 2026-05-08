# Performance Guide

## Async and threading model

All Flawright APIs are `async` and return `Task` or `Task<T>`. Internally, FlaUI COM calls are synchronous (they block the calling thread), so Flawright runs blocking FlaUI operations inside `Task.Run` when called from a non-thread-pool context.

**Never call Flawright APIs from a UI thread or STA thread synchronously.** Blocking on `task.Result` or `.GetAwaiter().GetResult()` from a single-threaded synchronization context (WPF, WinForms, or WinUI) will deadlock.

For test runners (`xunit`, `NUnit`, `MSTest`):
- xUnit test methods with `async Task` signatures work correctly — the runner awaits the task.
- NUnit `[Test]` methods that are `async Task` work correctly.
- Avoid `.Result` / `.Wait()` patterns; always `await`.

## Auto-wait timing

The default auto-wait settings are:

| Setting | Default | Property |
|---|---|---|
| Timeout | **5 seconds** | `FlawrightOptions.DefaultTimeout` |
| Poll interval | **100 ms** | `FlawrightOptions.DefaultRetryInterval` |

For each action (click, fill, assertion), Flawright polls the UIA tree every 100 ms for up to 5 seconds. This means:
- Best case (element immediately present): ~1 poll, ~0 ms overhead.
- Typical case (element appears after a render): 1–3 polls, ~100–300 ms.
- Worst case (timeout): up to 50 polls, 5 seconds.

**Tuning for slow machines**: Increase `DefaultTimeout`; keep `DefaultRetryInterval` at 100 ms.

**Tuning for fast CI with predictable apps**: Decrease `DefaultTimeout` to 2–3 seconds to fail fast when elements are missing.

```csharp
await using var fw = await Flawright.LaunchAsync(
    new LaunchOptions { ApplicationPath = "myapp.exe" },
    new FlawrightOptions
    {
        DefaultTimeout       = TimeSpan.FromSeconds(10),  // slow machine
        DefaultRetryInterval = TimeSpan.FromMilliseconds(100)
    });
```

Per-call override (does not affect global default):

```csharp
await page.Locator("#slowElement").WaitForAsync(
    new LocatorWaitForOptions { Timeout = TimeSpan.FromSeconds(30) });
```

## Selector cost

Not all selectors are equal in cost:

| Selector type | Cost | Notes |
|---|---|---|
| `#id` (AutomationId) | O(tree size) | FlaUI walks the tree stopping at first match |
| `name:` / `text:` | O(tree size) | Same tree walk |
| `controltype:` | O(tree size) | ControlType is cheapest property to filter on early |
| `class:` | O(tree size) | Slightly more expensive than ControlType |
| `[name*=...]` (contains) | O(tree size) × string op | Post-filter substring match on each node |
| `[name^=...]` (starts-with) | O(tree size) × string op | Post-filter |
| `[name$=...]` (ends-with) | O(tree size) × string op | Post-filter |
| Chained `>>` | O(subtree size) | Narrows the search to a subtree — faster for large trees |

**Prefer AutomationId (`#id`) when available.** FlaUI stops traversal at first match, and AutomationId is set by the application developer with uniqueness in mind.

**Use `controltype:` filters with chained locators** to narrow the search space early:

```csharp
// Less efficient: searches entire tree for any button named "Save"
page.Locator("name:Save")

// More efficient: searches only within the toolbar subtree
page.Locator("#mainToolbar >> name:Save")
page.Locator("#mainToolbar >> controltype:Button")
    .Filter(new LocatorFilterOptions { HasText = "Save" })
```

**Substring operators (`*=`, `^=`, `$=`) are post-filter** — Flawright walks the entire tree first, collects all candidates, then filters by the substring condition. Prefer exact `name:` or `#id` when possible.

## Repeated locators and caching

A `Locator` object is a lazy query description — it does not hold a reference to a resolved UIA element. Every time you call an action on a locator (`.ClickAsync()`, `.FillAsync()`, `.Expect()...`), the locator is re-resolved against the live UIA tree.

This means:
- **Caching the `Locator` object** is fine and saves the `page.Locator("...")` call, but does not cache the element resolution.
- **Calling many actions on the same locator** re-resolves the element each time. This is correct behavior (the element may change state between calls), but has cost.

```csharp
// Good: cache the locator object if you use it many times
var saveButton = page.Locator("#btnSave");
await saveButton.Expect().ToBeVisibleAsync();
await saveButton.Expect().ToBeEnabledAsync();
await saveButton.ClickAsync();
// Three resolutions, but the string parsing only happens once

// Bad: re-parse and re-resolve every time
await page.Locator("#btnSave").Expect().ToBeVisibleAsync();
await page.Locator("#btnSave").Expect().ToBeEnabledAsync();
await page.Locator("#btnSave").ClickAsync();
// Three string parses + three resolutions
```

## Launch and startup time

| Launch mode | Default startup timeout | Property |
|---|---|---|
| AUMID (packaged app) | **30 seconds** | `LaunchOptions.StartupTimeout` (if exposed) |
| `ApplicationPath` (exe) | **5 seconds** | Inherits `FlawrightOptions.DefaultTimeout` |

Packaged apps (WinUI3, UWP) often take longer to start on first cold launch (app data not yet initialized, package activation chain). The 30-second default covers most cases. On very slow CI machines, increase it:

```csharp
await using var fw = await Flawright.LaunchAsync(
    new LaunchOptions { Aumid = "..." },
    new FlawrightOptions { DefaultTimeout = TimeSpan.FromSeconds(45) });
```

## Memory profile

Flawright does **not** cache resolved UIA elements. The only long-lived state held by Flawright is:

- `IApplicationHandle` — one per launched/attached application (the process + FlaUI automation object). This is released on `DisposeAsync`.
- `FlawrightOptions` — a small record.
- The `UIA3Automation` instance (inside `FlaUiApplicationLauncher`) — holds the UIA COM connection.

Resolved elements returned by `AllAsync()` or `ElementHandleAsync()` are `IFlawrightElement` wrappers around FlaUI `AutomationElement` objects. FlaUI elements are COM wrappers; they are collected by the GC normally and do not need explicit disposal, but releasing them promptly (letting local variables go out of scope) reduces COM proxy pressure.

## Cancellation tokens

All Flawright `Async` methods accept an optional `CancellationToken` as the last parameter. Cancellation is propagated through the auto-wait polling loop — the loop exits immediately on cancellation with `OperationCanceledException`, not `FlawrightTimeoutException`.

Use cancellation tokens to implement test-level timeouts or to stop a test run cleanly:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

await page.Locator("#slowElement").WaitForAsync(
    new LocatorWaitForOptions { Timeout = TimeSpan.FromSeconds(30) },
    ct: cts.Token);
```

## Code coverage and the FlaUI bridge

Several classes in `Flawright` are annotated `[ExcludeFromCodeCoverage]`:

- `UiaElementBackend`, `FlaUiApplicationLauncher`, `FlaUiApplicationHandle`, `FlaUiInputBackend` — these make real UIA COM calls. They cannot be meaningfully unit-tested without a live Windows desktop session running the target application.
- All `*Options` record types — pure data, no logic.

The 90% coverage gate in CI is measured against the covered lines only. The excluded classes are not counted in the denominator. See `codecov.yml` for the coverage configuration.

## Parallel test execution

Running multiple UIA tests in parallel is possible but requires care:

- Each test instance launches its own application process — no sharing.
- Parallel tests that all create new windows will fight for keyboard focus and Z-order.
- If your test calls `ClickAsync`, the target window should be the foreground window. In practice, run UIA tests **sequentially within a test assembly** and parallelize at the assembly level only if each assembly uses a different application.

xUnit configuration for sequential execution:

```json
// xunit.runner.json
{
  "parallelizeAssembly": false,
  "parallelizeTestCollections": false
}
```

## Benchmarking your selector

To measure how long a selector resolution takes in your specific app, use a simple timing wrapper:

```csharp
var sw = Stopwatch.StartNew();
var el = await page.Locator("#targetElement").ElementHandleAsync();
sw.Stop();
Console.WriteLine($"Resolution: {sw.ElapsedMilliseconds} ms");
```

Compare `#id`, `name:`, and `controltype:` selectors against your app's UIA tree depth to pick the fastest option.
