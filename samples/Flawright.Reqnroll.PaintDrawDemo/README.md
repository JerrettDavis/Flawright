# Flawright.Reqnroll.PaintDrawDemo

Demonstrates BDD automation with Flawright and Reqnroll, showcasing **click-and-drag drawing**
in MS Paint (`mspaint.exe`) using the low-level `page.Mouse` absolute-coordinate API and the
higher-level `DragToAsync` locator API.

## Prerequisites

- Windows 10 or later with MS Paint installed
- .NET 10 or later
- Modern Paint (Windows 11): available via the Microsoft Store or `winget install Microsoft.Paint`
- Classic Paint (Windows 10): bundled with Windows

## Running the Tests

```bash
dotnet test samples/Flawright.Reqnroll.PaintDrawDemo
```

**Note**: These tests will be skipped on CI runners (e.g., `windows-2025-vs2026`) where MS Paint
is not preinstalled. They run automatically on Windows 11 machines with Paint installed.

## What It Tests

The feature file (`Features/PaintDraw.feature`) demonstrates:

- **Drawing a line** via `page.Mouse`: move to start, press button, move (with 20 interpolation
  steps) to end, release — using `IFlawrightMouse.MoveAsync`, `DownAsync`, and `UpAsync`
- **Drawing a rectangle** by first clicking the Rectangle shape tool in the ribbon, then
  performing the same drag gesture — the active tool determines what is rendered
- **DragToAsync**: the higher-level `IFlawrightLocator.DragToAsync` API, demonstrated by
  dragging a color swatch locator to the canvas locator

## Canvas Selector Strategy

Paint's drawing surface does not expose sub-elements for individual pixels, so the entire canvas
is targeted as one element. Two selectors are tried in priority order:

| Selector | Target |
|---|---|
| `automationid:DrawingIsland` | Windows 11 modern Paint (tried first) |
| `class:MSPaintView` | Classic mspaint.exe (fallback) |

The first selector that resolves to a visible element is cached and reused within the scenario.
The working selector on each build is documented in the step output.

## Coordinate System

Step parameters use **canvas-relative coordinates** (offsets from the canvas top-left corner).
The step definitions retrieve the canvas bounding box via `IFlawrightLocator.BoundingBoxAsync`
and add the canvas origin to convert to absolute screen coordinates before calling
`page.Mouse.MoveAsync`.

Coordinates are clamped to the canvas bounds to prevent drawing outside the drawable area.

## Custom Steps

All custom steps are in `PaintDrawStepDefinitions.cs`:

| Step | Purpose |
|---|---|
| `When I draw a line on the Paint canvas from (x1, y1) to (x2, y2)` | Drag with `page.Mouse` |
| `When I click the Rectangle tool in Paint` | Click the ribbon shape button |
| `When I draw a rectangle on the Paint canvas from (x1, y1) to (x2, y2)` | Same drag gesture with Rectangle tool active |
| `When I drag color swatch "selector" to the canvas in Paint` | Higher-level `DragToAsync` demo |
| `Then the Paint canvas should be visible` | Asserts canvas element is visible |

## Flawright APIs Used

- `IFlawrightMouse.MoveAsync(x, y, options)` — absolute screen coordinate mouse move with step interpolation
- `IFlawrightMouse.DownAsync()` — press left mouse button at current position
- `IFlawrightMouse.UpAsync()` — release left mouse button at current position
- `IFlawrightLocator.BoundingBoxAsync()` — get element bounds for coordinate conversion
- `IFlawrightLocator.DragToAsync(target)` — higher-level locator-to-locator drag
- `IFlawrightLocator.IsVisibleAsync()` — check element visibility (canvas resolution + swatch guard)
- `IFlawrightLocator.Expect().ToBeVisibleAsync()` — assert canvas is visible after drawing
- `IFlawrightPage.ClickAsync(selector)` — click the Rectangle tool button
- `IFlawrightPage.WaitForTimeoutAsync(ms)` — brief pause for Paint to render strokes

## Reference

See the [main Flawright README](../../README.md) for more information about selectors and the Flawright framework.
