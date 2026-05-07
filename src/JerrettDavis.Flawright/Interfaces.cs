namespace JerrettDavis.Flawright;

/// <summary>
/// Entry point for launching or attaching to desktop applications.
/// Obtain an instance via <see cref="Flawright.LaunchAsync"/> or
/// <see cref="Flawright.AttachAsync"/>.
/// </summary>
/// <example>
/// <code>
/// await using IFlawright fw = await Flawright.LaunchAsync(
///     new LaunchOptions { ApplicationPath = "notepad.exe" });
/// IFlawrightPage page = await fw.Browser.NewPageAsync();
/// await page.FillAsync("controltype:Edit", "hello");
/// </code>
/// </example>
public interface IFlawright : IAsyncDisposable
{
    /// <summary>Gets the browser (application) opened by this instance.</summary>
    IFlawrightBrowser Browser { get; }
}

/// <summary>
/// Represents a launched or attached desktop application.  Mirrors
/// Playwright's <c>Browser</c> concept.
/// </summary>
public interface IFlawrightBrowser : IAsyncDisposable
{
    /// <summary>
    /// Returns the main (first) window of the application.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A page representing the main window.</returns>
    Task<IFlawrightPage> NewPageAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns pages for all current top-level windows of the application.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of pages, one per top-level window.</returns>
    Task<IReadOnlyList<IFlawrightPage>> GetAllPagesAsync(CancellationToken ct = default);

    /// <summary>
    /// Polls until a top-level window whose title matches
    /// <paramref name="titleOrPredicate"/> appears, then returns a page for it.
    /// </summary>
    /// <param name="titleOrPredicate">
    /// Window title substring to match, or a predicate that receives the full
    /// title string.
    /// </param>
    /// <param name="timeout">
    /// Maximum time to wait.  <see langword="null"/> uses the default timeout.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A page for the matched window.</returns>
    /// <exception cref="FlawrightTimeoutException">
    /// Thrown when no matching window is found within the timeout.
    /// </exception>
    Task<IFlawrightPage> WaitForPageAsync(
        string titleOrPredicate,
        TimeSpan? timeout = null,
        CancellationToken ct = default);
}

/// <summary>
/// Represents a window or form within a desktop application.  Mirrors
/// Playwright's <c>Page</c> concept.
/// </summary>
public interface IFlawrightPage : IAsyncDisposable
{
    /// <summary>Returns the title of the current window.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<string> TitleAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a locator for elements matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">
    /// Selector string.  See <see cref="Selectors.SelectorParser"/> for syntax.
    /// </param>
    IFlawrightLocator Locator(string selector);

    /// <summary>
    /// Clicks the first element matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Element selector.</param>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ClickAsync(string selector, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Sets the value of the first element matching <paramref name="selector"/>
    /// in one shot via <c>ValuePattern</c>.
    /// </summary>
    /// <param name="selector">Element selector.</param>
    /// <param name="text">Text to fill.</param>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task FillAsync(string selector, string text, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Focuses the element and types <paramref name="text"/> character-by-character
    /// via the keyboard (realistic typing, suitable for inputs with key handlers).
    /// Use <see cref="FillAsync"/> for faster value-set via ValuePattern.
    /// </summary>
    /// <param name="selector">Element selector.</param>
    /// <param name="text">Text to type.</param>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task TypeAsync(string selector, string text, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Focuses the element and sends a key or chord (e.g. "Enter", "Ctrl+S").
    /// </summary>
    /// <param name="selector">Element selector.</param>
    /// <param name="key">Key name or chord string.</param>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PressAsync(string selector, string key, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Checks the checkbox or toggle-button matching <paramref name="selector"/>
    /// (sets it to the <c>On</c> state).
    /// </summary>
    /// <param name="selector">Element selector.</param>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CheckAsync(string selector, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Unchecks the checkbox or toggle-button matching <paramref name="selector"/>
    /// (sets it to the <c>Off</c> state).
    /// </summary>
    /// <param name="selector">Element selector.</param>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UncheckAsync(string selector, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Selects an item by value in a combo-box or list-box matching
    /// <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Element selector (the container, not the item).</param>
    /// <param name="value">
    /// Text of the item to select.  Matched against item <c>Name</c> or
    /// AutomationId.
    /// </param>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SelectOptionAsync(string selector, string value, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Waits for an element matching <paramref name="selector"/> to appear and
    /// returns it (convenience wrapper around <c>Locator(selector).FirstAsync()</c>).
    /// </summary>
    /// <param name="selector">Element selector.</param>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IFlawrightElement> WaitForSelectorAsync(string selector, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Captures a screenshot of the window.
    /// </summary>
    /// <param name="path">
    /// Optional file path to save the screenshot.  If <see langword="null"/>, only
    /// the byte array is returned.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PNG image data as a byte array.</returns>
    Task<byte[]> ScreenshotAsync(string? path = null, CancellationToken ct = default);
}

/// <summary>
/// A lazy reference to one or more UI elements, resolved at action time with
/// auto-waiting.  Mirrors Playwright's <c>Locator</c> concept.
/// </summary>
public interface IFlawrightLocator
{
    /// <summary>Gets the raw selector string used by this locator.</summary>
    string Selector { get; }

    /// <summary>
    /// Auto-waits until at least one matching element exists, then returns the
    /// first one.
    /// </summary>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="FlawrightTimeoutException">
    /// Thrown when no matching element is found within the timeout.
    /// </exception>
    Task<IFlawrightElement> FirstAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Auto-waits until at least <paramref name="index"/> + 1 matching elements
    /// exist, then returns the one at <paramref name="index"/> (0-based).
    /// </summary>
    /// <param name="index">Zero-based index.</param>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IFlawrightElement> NthAsync(int index, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Returns the current count of matching elements without waiting.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>
    /// Auto-waits for at least one matching element, then returns all currently
    /// matching elements.
    /// </summary>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<IFlawrightElement>> AllAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Returns a new locator that only yields elements from this locator that
    /// also satisfy <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">Filter predicate applied to each candidate element.</param>
    IFlawrightLocator Filter(Func<IFlawrightElement, bool> predicate);

    /// <summary>
    /// Clicks the first matching element (auto-waited).
    /// </summary>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ClickAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Fills the first matching element with <paramref name="text"/> via
    /// ValuePattern (auto-waited).
    /// </summary>
    /// <param name="text">Text to fill.</param>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task FillAsync(string text, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Returns assertion helpers for this locator.</summary>
    IFlawrightAssertions Expect();
}

/// <summary>
/// A resolved UI element with async action methods.  Mirrors Playwright's
/// <c>ElementHandle</c> concept.
/// </summary>
public interface IFlawrightElement
{
    /// <summary>Gets the locator that produced this element.</summary>
    IFlawrightLocator Locator { get; }

    /// <summary>Clicks the element.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task ClickAsync(CancellationToken ct = default);

    /// <summary>Double-clicks the element.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task DoubleClickAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the element's value in one shot via <c>ValuePattern</c>.
    /// </summary>
    /// <param name="text">Text to fill.</param>
    /// <param name="ct">Cancellation token.</param>
    Task FillAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Returns the element's visible text content.  Uses <c>ValuePattern</c>
    /// first (edit controls), then <c>TextPattern</c> (document/text
    /// controls), and falls back to the <c>Name</c> property.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<string> TextAsync(CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if the element is on-screen.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> IsVisibleAsync(CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if the element is enabled.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> IsEnabledAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns <see langword="true"/> if the element is checked (toggle state
    /// is <c>On</c>).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> IsCheckedAsync(CancellationToken ct = default);

    /// <summary>Moves the mouse over the element (hover).</summary>
    /// <param name="ct">Cancellation token.</param>
    Task HoverAsync(CancellationToken ct = default);

    /// <summary>Gives keyboard focus to the element.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task FocusAsync(CancellationToken ct = default);

    /// <summary>
    /// Scrolls the element into view using <c>ScrollItemPattern</c>, if
    /// supported.  This is a best-effort operation; no exception is thrown if
    /// the pattern is not available.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task ScrollIntoViewIfNeededAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a common automation attribute by name.
    /// </summary>
    /// <param name="name">
    /// One of: <c>"AutomationId"</c>, <c>"Name"</c>, <c>"ClassName"</c>,
    /// <c>"ControlType"</c>, <c>"Value"</c>.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The attribute value as a string, or <see langword="null"/>.</returns>
    Task<string?> GetAttributeAsync(string name, CancellationToken ct = default);
}

/// <summary>
/// Assertion helpers for a locator, providing Playwright-style
/// <c>expect(locator).toBeVisible()</c> semantics.
/// </summary>
public interface IFlawrightAssertions
{
    /// <summary>
    /// Gets the negated assertions object.  Each method on the returned value
    /// asserts the opposite condition.
    /// </summary>
    /// <remarks>
    /// The name <c>Not</c> follows the Playwright convention.  VB.NET consumers
    /// should use the explicit interface name if this conflicts with the
    /// <c>Not</c> operator.
    /// </remarks>
#pragma warning disable CA1716 // 'Not' intentionally matches the Playwright API convention
    IFlawrightNotAssertions Not { get; }
#pragma warning restore CA1716

    /// <summary>Asserts that the element is visible (exists and is not off-screen).</summary>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToBeVisibleAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is hidden (off-screen or absent).</summary>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToBeHiddenAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is enabled.</summary>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToBeEnabledAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is disabled.</summary>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToBeDisabledAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's text equals <paramref name="expectedText"/>.</summary>
    /// <param name="expectedText">Expected text value.</param>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToHaveTextAsync(string expectedText, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Asserts that the locator matches exactly <paramref name="expectedCount"/> elements.
    /// </summary>
    /// <param name="expectedCount">Expected element count.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToHaveCountAsync(int expectedCount, CancellationToken ct = default);

    /// <summary>
    /// Asserts that the element's value (via <c>ValuePattern</c>) equals
    /// <paramref name="expected"/>.
    /// </summary>
    /// <param name="expected">Expected value string.</param>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToHaveValueAsync(string expected, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is checked (toggle state is <c>On</c>).</summary>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToBeCheckedAsync(TimeSpan? timeout = null, CancellationToken ct = default);
}

/// <summary>
/// Negated counterpart of <see cref="IFlawrightAssertions"/>.  Each method
/// asserts the opposite condition.
/// </summary>
public interface IFlawrightNotAssertions
{
    /// <summary>Asserts that the element is <em>not</em> visible.</summary>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToBeVisibleAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is <em>not</em> hidden.</summary>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToBeHiddenAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is <em>not</em> enabled.</summary>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToBeEnabledAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is <em>not</em> disabled.</summary>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToBeDisabledAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's text does <em>not</em> equal <paramref name="expectedText"/>.</summary>
    /// <param name="expectedText">Text that must <em>not</em> be present.</param>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToHaveTextAsync(string expectedText, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's value does <em>not</em> equal <paramref name="expected"/>.</summary>
    /// <param name="expected">Value that must <em>not</em> be present.</param>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToHaveValueAsync(string expected, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is <em>not</em> checked.</summary>
    /// <param name="timeout">Per-call timeout override.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ToBeCheckedAsync(TimeSpan? timeout = null, CancellationToken ct = default);
}
