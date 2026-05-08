# Win11 Calculator

Windows Calculator is a WinUI3 packaged app distributed via the Microsoft Store and pre-installed on Windows 10 and 11. It has stable AutomationIds for every button and the result display, making it a reliable test target.

## Launching

```csharp
// Simplest form — Flawright auto-resolves calc.exe on Windows 11
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    ApplicationPath = "calc.exe"
});
```

To launch by AUMID directly:

```csharp
await using var fw = await Flawright.LaunchAsync(new LaunchOptions
{
    Aumid = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App"
});
```

## Selector cheat sheet

### Number buttons

| Button | AutomationId |
|---|---|
| 0 | `num0Button` |
| 1 | `num1Button` |
| 2 | `num2Button` |
| 3 | `num3Button` |
| 4 | `num4Button` |
| 5 | `num5Button` |
| 6 | `num6Button` |
| 7 | `num7Button` |
| 8 | `num8Button` |
| 9 | `num9Button` |

### Operator buttons (Standard mode)

| Button | AutomationId |
|---|---|
| + | `plusButton` |
| - | `minusButton` |
| × | `multiplyButton` |
| ÷ | `divideButton` |
| = | `equalButton` |
| . (decimal) | `decimalSeparatorButton` |
| +/- | `negateButton` |
| % | `percentButton` |
| CE | `clearEntryButton` |
| C | `clearButton` |
| ⌫ | `backSpaceButton` |

### Display and mode

| Element | Selector | Notes |
|---|---|---|
| Result display | `#CalculatorResults` | AutomationId |
| Display (name match) | `[name*="Display is"]` | Name contains "Display is N"; useful for assertions |
| Mode switcher (hamburger) | `#TogglePaneButton` | Opens the navigation panel |
| Standard mode item | `name:Standard` | In the navigation panel |
| Scientific mode item | `name:Scientific` | In the navigation panel |
| Programmer mode item | `name:Programmer` | In the navigation panel |

> **Display name pattern**
>
> The `CalculatorResults` element's Name property is `"Display is N"` where N is the current value. Use `[name*="Display is"]` (substring match) to assert it is visible, or `ToHaveTextAsync` with the full string including "Display is":
>
> ```csharp
> await page.Locator("#CalculatorResults").Expect().ToHaveTextAsync("Display is 3");
> ```

## Worked example: 1 + 2 = 3

```csharp
using JerrettDavis.Flawright;
using Xunit;

public class CalculatorTests : IAsyncLifetime
{
    private Flawright? _fw;

    public async Task InitializeAsync()
    {
        _fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "calc.exe"
        });
    }

    [Fact]
    public async Task OnePlusTwoEqualsThree()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.ClickAsync("#num1Button");
        await page.ClickAsync("#plusButton");
        await page.ClickAsync("#num2Button");
        await page.ClickAsync("#equalButton");

        // Display shows "Display is 3"
        await page.Locator("#CalculatorResults")
            .Expect()
            .ToHaveTextAsync("Display is 3");
    }

    [Fact]
    public async Task ClearButton_ResetsDisplay()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.ClickAsync("#num5Button");
        await page.ClickAsync("#clearButton");

        await page.Locator("#CalculatorResults")
            .Expect()
            .ToHaveTextAsync("Display is 0");
    }

    [Fact]
    public async Task SwitchToScientificMode()
    {
        var page = await _fw!.Browser.NewPageAsync();

        // Open the navigation panel
        await page.ClickAsync("#TogglePaneButton");

        // Select Scientific mode
        await page.ClickAsync("name:Scientific");

        // The mode header should update
        await page.Locator("name:Scientific Calculator").Expect().ToBeVisibleAsync();
    }

    public async Task DisposeAsync()
    {
        if (_fw != null)
            await _fw.DisposeAsync();
    }
}
```

## Gotchas

**AutomationIds differ from older Calculator**
The pre-Win10-1903 Calculator used single-digit numeric AutomationIds (`"1"`, `"2"`, etc.) and operator names (`"plus"`, `"equals"`). The current WinUI3 Calculator uses the `num1Button` / `plusButton` / `equalButton` pattern documented above. If your machine has a very old Calculator, use Accessibility Insights to verify.

**Mode state persists between sessions**
Calculator remembers the last-used mode (Standard, Scientific, Programmer, Date Calculation). If a previous test switched to Programmer mode, the next launch starts in Programmer mode. Either reset to Standard at the start of each test, or launch a fresh instance per test using `IAsyncLifetime`.

**History panel covers buttons**
If the history panel is open, it may obscure some buttons. Close it with `#HistoryButton` or `#memRecall` before clicking operators. In tests, prefer starting from a known state with `#clearButton`.

**Date Calculation mode**
The Date Calculation mode does not expose the numeric buttons. If your test unexpectedly finds zero buttons, check that Calculator is in Standard mode.

## Related docs

- [Selectors](../selectors.md) — full selector grammar including substring `*=` operator
- [WinUI 3 guide](winui3.md) — general WinUI3 patterns
- [Auto-waiting](../auto-waiting.md) — timing and timeout configuration
