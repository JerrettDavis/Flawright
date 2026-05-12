using Flawright.Locator;
using Reqnroll;

namespace Flawright.Reqnroll.PaintDrawDemo;

/// <summary>
/// Custom step definitions for MS Paint drawing scenarios.
/// Demonstrates <see cref="IFlawrightMouse"/> absolute-coordinate drag operations
/// and the higher-level <c>DragToAsync</c> API on <see cref="IFlawrightLocator"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Canvas selector strategy:</b>
/// <list type="bullet">
///   <item><description>
///     Windows 11 modern Paint exposes the drawing surface with
///     <c>automationid:DrawingIsland</c>. This is tried first.
///   </description></item>
///   <item><description>
///     Older / classic mspaint.exe uses <c>class:MSPaintView</c>.
///     This is tried as a fallback when <c>automationid:DrawingIsland</c> is absent.
///   </description></item>
/// </list>
/// Whichever selector resolves first is stored in <see cref="_canvasSelector"/> for
/// reuse within the scenario.
/// </para>
/// <para>
/// <b>Coordinate strategy:</b>
/// Step parameters are logical offsets from the canvas origin (the top-left corner
/// of the canvas bounding box). The steps retrieve the canvas bounding box via
/// <see cref="IFlawrightLocator.BoundingBoxAsync"/> and add the canvas top-left to
/// convert to absolute screen coordinates before calling
/// <see cref="IFlawrightMouse.MoveAsync"/>, <see cref="IFlawrightMouse.DownAsync"/>,
/// and <see cref="IFlawrightMouse.UpAsync"/>.
/// </para>
/// <para>
/// <b>DragToAsync demo:</b>
/// The "drag color swatch" scenario demonstrates the higher-level
/// <see cref="IFlawrightLocator.DragToAsync"/> overload that takes a target locator.
/// Paint does not have draggable tool elements in the classical sense; this scenario
/// drags from a named color swatch (e.g. "name:Black") into the canvas locator.
/// If the swatch name selector does not resolve on a particular Paint build, the step
/// throws with an explanatory message rather than silently no-oping.
/// </para>
/// </remarks>
[Binding]
#pragma warning disable CA1515 // Reqnroll discovers binding classes via reflection — public is required even in test assemblies
public sealed class PaintDrawStepDefinitions
#pragma warning restore CA1515
{
    /// <summary>Candidate selectors for the Paint canvas, tried in priority order.</summary>
    private static readonly string[] CanvasCandidates =
    [
        "automationid:DrawingIsland",  // Windows 11 modern Paint
        "class:MSPaintView",           // Classic mspaint.exe
    ];

    private readonly IFlawrightPage _page;

    /// <summary>
    /// The canvas selector that successfully resolved for this scenario.
    /// Populated lazily on first access via <see cref="ResolveCanvasAsync"/>.
    /// </summary>
    private string? _canvasSelector;

    public PaintDrawStepDefinitions(IFlawrightPage page)
    {
        _page = page;
    }

    // ── Canvas visibility assertion ───────────────────────────────────────────

    /// <summary>
    /// Asserts that the Paint canvas element is visible, using the resolved selector.
    /// </summary>
    [Then(@"the Paint canvas should be visible")]
    public async Task PaintCanvasShouldBeVisibleAsync()
    {
        var selector = await ResolveCanvasAsync().ConfigureAwait(false);
        await _page.Locator(selector).Expect().ToBeVisibleAsync().ConfigureAwait(false);
    }

    // ── Line drawing ──────────────────────────────────────────────────────────

    /// <summary>
    /// Draws a straight line on the Paint canvas by:
    /// <list type="number">
    ///   <item><description>Resolving the canvas bounding box.</description></item>
    ///   <item><description>Computing absolute screen coordinates from canvas-relative offsets.</description></item>
    ///   <item><description>Moving the mouse to the start point, pressing the button, moving to the end point, then releasing.</description></item>
    /// </list>
    /// </summary>
    /// <param name="x1">Canvas-relative X of the start point.</param>
    /// <param name="y1">Canvas-relative Y of the start point.</param>
    /// <param name="x2">Canvas-relative X of the end point.</param>
    /// <param name="y2">Canvas-relative Y of the end point.</param>
    [When(@"I draw a line on the Paint canvas from \((\d+), (\d+)\) to \((\d+), (\d+)\)")]
    public async Task DrawLineAsync(int x1, int y1, int x2, int y2)
    {
        var (absX1, absY1, absX2, absY2) = await ResolveAbsoluteCoordinatesAsync(x1, y1, x2, y2)
            .ConfigureAwait(false);

        // Move to start, press left mouse button, drag to end, release.
        await _page.Mouse.MoveAsync(absX1, absY1).ConfigureAwait(false);
        await _page.Mouse.DownAsync().ConfigureAwait(false);
        await _page.Mouse.MoveAsync(absX2, absY2, new Page.MouseMoveOptions { Steps = 20 })
            .ConfigureAwait(false);
        await _page.Mouse.UpAsync().ConfigureAwait(false);

        // Brief pause so Paint renders the stroke before subsequent steps.
        await _page.WaitForTimeoutAsync(200).ConfigureAwait(false);
    }

    // ── Rectangle shape ───────────────────────────────────────────────────────

    /// <summary>
    /// Clicks the Rectangle tool in the Paint ribbon.
    /// Modern Paint exposes the rectangle shape button as <c>"name:Rectangle"</c>.
    /// </summary>
    /// <remarks>
    /// If the Rectangle button name differs on a particular build (e.g. it is labelled
    /// "Rect" or exposed via AutomationId), update the selector here. The feature file
    /// documents this as the working selector for Windows 11 Paint 11.2402 and later.
    /// </remarks>
    [When(@"I click the Rectangle tool in Paint")]
    public async Task ClickRectangleToolAsync()
    {
        // Windows 11 Paint ribbon exposes the shape as "name:Rectangle".
        await _page.ClickAsync("name:Rectangle").ConfigureAwait(false);
    }

    /// <summary>
    /// Draws a rectangle on the Paint canvas by dragging from one corner to the opposite corner.
    /// Uses the same absolute-coordinate mouse drag technique as <see cref="DrawLineAsync"/>.
    /// </summary>
    [When(@"I draw a rectangle on the Paint canvas from \((\d+), (\d+)\) to \((\d+), (\d+)\)")]
    public async Task DrawRectangleAsync(int x1, int y1, int x2, int y2)
    {
        // Rectangle drawing is identical to line drawing at the input level —
        // the active tool determines what is rendered on the canvas.
        await DrawLineAsync(x1, y1, x2, y2).ConfigureAwait(false);
    }

    // ── DragToAsync demonstration ─────────────────────────────────────────────

    /// <summary>
    /// Demonstrates the higher-level <see cref="IFlawrightLocator.DragToAsync"/> API
    /// by dragging a named color swatch onto the Paint canvas.
    /// </summary>
    /// <param name="swatchSelector">
    /// The Flawright selector for the color swatch, e.g. <c>"name:Black"</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// Paint's color palette exposes swatches as UIA elements with <c>Name</c>
    /// properties such as <c>"Black"</c>, <c>"White"</c>, <c>"Red"</c>, etc.
    /// Dragging a swatch to the canvas does not change the foreground color via
    /// drag-and-drop (Paint uses click-to-select), so the visual effect is minimal —
    /// this step exists purely to showcase the <c>DragToAsync</c> API surface.
    /// </para>
    /// <para>
    /// If the swatch selector cannot be resolved (e.g. Paint on the current build
    /// uses AutomationIds instead of Names for swatches), the step throws an
    /// <see cref="InvalidOperationException"/> with a descriptive message rather
    /// than silently no-oping.
    /// </para>
    /// </remarks>
    [When(@"I drag color swatch ""([^""]*)"" to the canvas in Paint")]
    public async Task DragSwatchToCanvasAsync(string swatchSelector)
    {
        var canvasSelector = await ResolveCanvasAsync().ConfigureAwait(false);

        var swatch = _page.Locator(swatchSelector);
        var isSwatchVisible = await swatch.IsVisibleAsync().ConfigureAwait(false);

        if (!isSwatchVisible)
        {
            throw new InvalidOperationException(
                $"Color swatch '{swatchSelector}' is not visible. " +
                "This scenario requires Paint to expose color swatches as named UIA elements. " +
                "On builds where swatches use AutomationIds instead of Names, update the " +
                "selector in the feature file (e.g. 'automationid:colorSwatch_Black').");
        }

        var canvasLocator = _page.Locator(canvasSelector);

        // DragToAsync: drag from the swatch to the center of the canvas.
        await swatch.DragToAsync(canvasLocator).ConfigureAwait(false);

        await _page.WaitForTimeoutAsync(200).ConfigureAwait(false);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the Paint canvas selector, caching the result for reuse within the scenario.
    /// Tries each candidate in <see cref="CanvasCandidates"/> in order and returns the
    /// first selector whose element is visible.
    /// </summary>
    /// <returns>The first candidate selector that resolves to a visible element.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when none of the candidate selectors match a visible canvas element.
    /// </exception>
    private async Task<string> ResolveCanvasAsync()
    {
        if (_canvasSelector != null)
            return _canvasSelector;

        foreach (var candidate in CanvasCandidates)
        {
            try
            {
                var locator = _page.Locator(candidate);
                if (await locator.IsVisibleAsync().ConfigureAwait(false))
                {
                    _canvasSelector = candidate;
                    return _canvasSelector;
                }
            }
#pragma warning disable CA1031 // Intentional: try each candidate selector in turn
            catch (Exception)
#pragma warning restore CA1031
            {
                // Not found — try the next candidate.
            }
        }

        throw new InvalidOperationException(
            "Paint canvas element not found. " +
            $"Tried selectors: {string.Join(", ", CanvasCandidates)}. " +
            "Ensure MS Paint is open and the canvas is visible. " +
            "On Windows 11 modern Paint the canvas uses 'automationid:DrawingIsland'; " +
            "on classic mspaint.exe it uses 'class:MSPaintView'.");
    }

    /// <summary>
    /// Resolves canvas-relative coordinates to absolute screen coordinates by fetching
    /// the canvas bounding box and offsetting by its top-left corner.
    /// </summary>
    private async Task<(double absX1, double absY1, double absX2, double absY2)>
        ResolveAbsoluteCoordinatesAsync(int x1, int y1, int x2, int y2)
    {
        var selector = await ResolveCanvasAsync().ConfigureAwait(false);
        var bbox = await _page.Locator(selector).BoundingBoxAsync().ConfigureAwait(false);

        if (bbox == null)
        {
            throw new InvalidOperationException(
                $"Could not obtain bounding box for canvas selector '{selector}'. " +
                "The element may not have a screen presence.");
        }

        // Clamp coordinates so they stay within the canvas bounds.
        var clampedX1 = Math.Clamp(x1, 0, (int)bbox.Width - 1);
        var clampedY1 = Math.Clamp(y1, 0, (int)bbox.Height - 1);
        var clampedX2 = Math.Clamp(x2, 0, (int)bbox.Width - 1);
        var clampedY2 = Math.Clamp(y2, 0, (int)bbox.Height - 1);

        return (
            bbox.X + clampedX1,
            bbox.Y + clampedY1,
            bbox.X + clampedX2,
            bbox.Y + clampedY2
        );
    }
}
