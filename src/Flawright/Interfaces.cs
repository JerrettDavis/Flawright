namespace Flawright;

/// <summary>
/// Entry point for launching or attaching to desktop applications.
/// Obtain an instance via <see cref="Flawright.LaunchAsync(LaunchOptions, FlawrightOptions?, CancellationToken)"/> or
/// <see cref="Flawright.AttachAsync(AttachOptions, FlawrightOptions?, CancellationToken)"/>.
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
public interface IFlawrightBrowser : IFlawrightBrowserEvents, IAsyncDisposable
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
    /// Polls until a top-level window whose title contains
    /// <paramref name="title"/> appears, then returns a page for it.
    /// </summary>
    /// <param name="title">
    /// Window title substring to match (case-insensitive).
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
        string title,
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    /// <summary>
    /// Closes the application using the <see cref="FlawrightOptions.CloseBehavior"/>
    /// configured when the browser was created. Falls back to a process kill if
    /// the behavior returns <see langword="false"/> or if the app does not exit
    /// within <paramref name="timeout"/>.
    /// </summary>
    /// <param name="timeout">
    ///   How long to allow the close behavior to run before falling back to a
    ///   process kill. Defaults to 5 seconds when <see langword="null"/>.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the application exited gracefully (the
    ///   configured behavior returned <see langword="true"/>).
    ///   <see langword="false"/> if the method had to fall back to a process kill.
    /// </returns>
    Task<bool> CloseAsync(TimeSpan? timeout = null);
}

/// <summary>
/// Represents a window or form within a desktop application.  Mirrors
/// Playwright's <c>Page</c> concept.
/// </summary>
public interface IFlawrightPage : IAsyncDisposable
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Returns the title of the current window.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<string> TitleAsync(CancellationToken ct = default);

    /// <summary>Brings the window to the front of the z-order.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task BringToFrontAsync(CancellationToken ct = default);

    /// <summary>Waits for the specified number of milliseconds.</summary>
    /// <param name="milliseconds">Duration to wait.</param>
    /// <param name="ct">Cancellation token.</param>
    Task WaitForTimeoutAsync(double milliseconds, CancellationToken ct = default);

    /// <summary>Gets the options used to configure this page.</summary>
    FlawrightOptions Options { get; }

    // ── Locator factory ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates a locator for elements matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">
    /// Selector string.  See <see cref="Selectors.SelectorParser"/> for syntax.
    /// </param>
    IFlawrightLocator Locator(string selector);

    /// <summary>Returns a locator for elements with the given ARIA role.</summary>
    IFlawrightLocator GetByRole(Selectors.AriaRole role, Locator.LocatorGetByRoleOptions? options = null);

    /// <summary>Returns a locator for elements with a label matching <paramref name="text"/>.</summary>
    IFlawrightLocator GetByLabel(string text, Locator.LocatorGetByLabelOptions? options = null);

    /// <summary>Returns a locator for elements whose visible text matches <paramref name="text"/>.</summary>
    IFlawrightLocator GetByText(string text, Locator.LocatorGetByTextOptions? options = null);

    /// <summary>Returns a locator for elements with a matching test ID (AutomationId).</summary>
    IFlawrightLocator GetByTestId(string testId);

    /// <summary>Returns a locator for elements with a placeholder matching <paramref name="text"/>.</summary>
    IFlawrightLocator GetByPlaceholder(string text, Locator.LocatorGetByPlaceholderOptions? options = null);

    /// <summary>Returns a locator for elements with a title matching <paramref name="text"/>.</summary>
    IFlawrightLocator GetByTitle(string text, Locator.LocatorGetByTitleOptions? options = null);

    // ── Convenience action methods (delegate to Locator(...).XxxAsync) ─────────

    /// <summary>Clicks the first element matching <paramref name="selector"/>.</summary>
    Task ClickAsync(string selector, Locator.LocatorClickOptions? options = null, CancellationToken ct = default);

    /// <summary>Right-clicks the first element matching <paramref name="selector"/>.</summary>
    Task RightClickAsync(string selector, Locator.LocatorClickOptions? options = null, CancellationToken ct = default);

    /// <summary>Double-clicks the first element matching <paramref name="selector"/>.</summary>
    Task DoubleClickAsync(string selector, Locator.LocatorDoubleClickOptions? options = null, CancellationToken ct = default);

    /// <summary>Sets the value of the first element matching <paramref name="selector"/> via <c>ValuePattern</c>.</summary>
    Task FillAsync(string selector, string value, Locator.LocatorFillOptions? options = null, CancellationToken ct = default);

    /// <summary>Types <paramref name="text"/> character-by-character into the first element matching <paramref name="selector"/>.</summary>
    Task TypeAsync(string selector, string text, Locator.LocatorTypeOptions? options = null, CancellationToken ct = default);

    /// <summary>Focuses the element and sends a key or chord.</summary>
    Task PressAsync(string selector, string key, Locator.LocatorPressOptions? options = null, CancellationToken ct = default);

    /// <summary>Checks the element matching <paramref name="selector"/> (sets it to the <c>On</c> state).</summary>
    Task CheckAsync(string selector, Locator.LocatorCheckOptions? options = null, CancellationToken ct = default);

    /// <summary>Unchecks the element matching <paramref name="selector"/> (sets it to the <c>Off</c> state).</summary>
    Task UncheckAsync(string selector, Locator.LocatorUncheckOptions? options = null, CancellationToken ct = default);

    /// <summary>Sets the checked state of the element matching <paramref name="selector"/>.</summary>
#pragma warning disable CA1716
    Task SetCheckedAsync(string selector, bool @checked, Locator.LocatorSetCheckedOptions? options = null, CancellationToken ct = default);
#pragma warning restore CA1716

    /// <summary>Selects an item by value in a combo-box or list-box matching <paramref name="selector"/>.</summary>
    Task SelectOptionAsync(string selector, string value, Locator.LocatorSelectOptionOptions? options = null, CancellationToken ct = default);

    /// <summary>Hovers over the first element matching <paramref name="selector"/>.</summary>
    Task HoverAsync(string selector, Locator.LocatorHoverOptions? options = null, CancellationToken ct = default);

    /// <summary>Focuses the first element matching <paramref name="selector"/>.</summary>
    Task FocusAsync(string selector, CancellationToken ct = default);

    /// <summary>Drags the element matching <paramref name="source"/> to the element matching <paramref name="target"/>.</summary>
    Task DragAndDropAsync(string source, string target, Locator.LocatorDragToOptions? options = null, CancellationToken ct = default);

    /// <summary>Waits for an element matching <paramref name="selector"/> to appear and returns it.</summary>
    Task<IFlawrightElement> WaitForSelectorAsync(string selector, Locator.LocatorWaitForOptions? options = null, CancellationToken ct = default);

    // ── Owned-window / dialog discovery ──────────────────────────────────────

    /// <summary>
    /// Returns pages for all top-level windows in this process other than this
    /// page's own window.
    /// Use this to discover dialogs, popups, and floating tool windows.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// Returns all visible top-level windows in this process other than this page's own window.
    /// This is intentionally permissive — Flawright cannot reliably determine Win32 ownership
    /// for dialogs spawned by UI frameworks that don't propagate GWL_HWNDPARENT (WPF, WinForms,
    /// WinUI). In practice, an automated application opens dialogs and popups in its own
    /// process, so "all top-levels minus self" matches the common case.
    ///
    /// Apps that legitimately host multiple independent top-level windows in one process
    /// (e.g. an app + a separate splash screen) will see all of them returned here. Filter
    /// by title or window properties at the call site if you need stricter scoping.
    ///
    /// May fire the DialogOpened event for newly-discovered windows (deduplicated per page instance).
    ///
    /// Validated against: WPF Window.ShowDialog/Show, WPF ToolWindow style, WPF
    /// nested dialogs, WinForms Form.ShowDialog/Show, Win32 MessageBox, comdlg32
    /// common dialogs (OpenFileDialog, SaveFileDialog).
    ///
    /// Known limitations:
    /// - WinUI/UWP ContentDialog and Popup are in-process visual overlays with
    ///   no top-level HWND; they are not returned by this method. A future
    ///   UIA-tree-walking method may surface them separately.
    /// - MDI children are embedded in the MDI client area and have no independent
    ///   top-level HWND; they are not returned by this method.
    /// </remarks>
    Task<IReadOnlyList<IFlawrightPage>> GetOwnedWindowsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns pages for modal windows currently active on this page's window,
    /// via UIA WindowPattern. This is a subset of <see cref="GetOwnedWindowsAsync"/>
    /// — only modal owned windows.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// This method may raise the <see cref="IFlawrightBrowserEvents.DialogOpened"/> event
    /// for each newly-discovered modal window (once per unique modal handle per page instance).
    /// </remarks>
    Task<IReadOnlyList<IFlawrightPage>> GetModalWindowsAsync(CancellationToken ct = default);

    /// <summary>
    /// Waits for a dialog window owned by this page to appear, returning a page
    /// bound to it.
    /// </summary>
    /// <param name="titlePattern">
    /// Optional substring to match against the dialog's window title
    /// (case-insensitive). When <see langword="null"/>, returns the first owned
    /// window to appear.
    /// </param>
    /// <param name="timeout">
    /// Maximum time to wait. Defaults to the browser's default timeout.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="FlawrightTimeoutException">
    /// No matching owned window appeared within timeout.
    /// </exception>
    /// <remarks>
    /// This method raises the <see cref="IFlawrightBrowserEvents.DialogOpened"/> event
    /// for the newly-detected dialog (once per unique dialog handle per page instance).
    /// </remarks>
    Task<IFlawrightPage> WaitForDialogAsync(
        string? titlePattern = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    // ── Screenshot ────────────────────────────────────────────────────────────

    /// <summary>Captures a screenshot of the window.</summary>
    /// <param name="options">Screenshot options (e.g. save path).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PNG image data as a byte array.</returns>
    Task<byte[]> ScreenshotAsync(Locator.LocatorScreenshotOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Captures a screenshot of the window and saves it to <paramref name="path"/>.
    /// Convenience overload — equivalent to passing
    /// <c>new LocatorScreenshotOptions { Path = path }</c>.
    /// </summary>
    /// <param name="path">File path where the PNG screenshot will be saved.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PNG image data as a byte array.</returns>
    Task<byte[]> ScreenshotAsync(string path, CancellationToken ct = default);

    // ── Sub-APIs ──────────────────────────────────────────────────────────────

    /// <summary>Gets the mouse sub-API for absolute-coordinate mouse operations.</summary>
    IFlawrightMouse Mouse { get; }

    /// <summary>Gets the keyboard sub-API for global keyboard operations.</summary>
    IFlawrightKeyboard Keyboard { get; }
}

/// <summary>
/// Low-level mouse operations at absolute screen coordinates.
/// Mirrors Playwright's <c>Mouse</c> class.
/// Obtain via <see cref="IFlawrightPage.Mouse"/>.
/// </summary>
public interface IFlawrightMouse
{
    /// <summary>Clicks at the specified screen coordinates.</summary>
    Task ClickAsync(double x, double y, Page.MouseClickOptions? options = null, CancellationToken ct = default);

    /// <summary>Double-clicks at the specified screen coordinates.</summary>
    Task DoubleClickAsync(double x, double y, Page.MouseDoubleClickOptions? options = null, CancellationToken ct = default);

    /// <summary>Presses a mouse button at the current position.</summary>
    Task DownAsync(Page.MouseDownOptions? options = null, CancellationToken ct = default);

    /// <summary>Releases a mouse button at the current position.</summary>
    Task UpAsync(Page.MouseUpOptions? options = null, CancellationToken ct = default);

    /// <summary>Moves the mouse to the specified screen coordinates.</summary>
    Task MoveAsync(double x, double y, Page.MouseMoveOptions? options = null, CancellationToken ct = default);

    /// <summary>Dispatches a mouse wheel event.</summary>
    Task WheelAsync(double deltaX, double deltaY, CancellationToken ct = default);
}

/// <summary>
/// Global keyboard operations.
/// Mirrors Playwright's <c>Keyboard</c> class.
/// Obtain via <see cref="IFlawrightPage.Keyboard"/>.
/// </summary>
public interface IFlawrightKeyboard
{
    /// <summary>Holds down a key.</summary>
    Task DownAsync(string key, CancellationToken ct = default);

    /// <summary>Releases a key.</summary>
    Task UpAsync(string key, CancellationToken ct = default);

    /// <summary>Presses a key or chord (e.g. "Ctrl+S"). Press+release.</summary>
    Task PressAsync(string key, Page.KeyboardPressOptions? options = null, CancellationToken ct = default);

    /// <summary>Types text character-by-character with optional delay.</summary>
    Task TypeAsync(string text, Page.KeyboardTypeOptions? options = null, CancellationToken ct = default);

    /// <summary>Inserts text directly without key-by-key simulation.</summary>
    Task InsertTextAsync(string text, CancellationToken ct = default);
}

/// <summary>
/// Assertion helpers for a page, providing Playwright-style
/// <c>expect(page).toHaveTitle()</c> semantics with auto-waiting.
/// Obtain via <see cref="AssertionsStatic.Expect(IFlawrightPage)"/>.
/// </summary>
public interface IFlawrightPageAssertions
{
    /// <summary>
    /// Gets the negated assertions object.
    /// </summary>
#pragma warning disable CA1716
    IFlawrightPageAssertions Not { get; }
#pragma warning restore CA1716

    /// <summary>Asserts that the page title equals <paramref name="expected"/>.</summary>
    Task ToHaveTitleAsync(string expected, Assertions.PageAssertionsToHaveTitleOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the page title matches <paramref name="expected"/>.</summary>
    Task ToHaveTitleAsync(System.Text.RegularExpressions.Regex expected, Assertions.PageAssertionsToHaveTitleOptions? options = null, CancellationToken ct = default);
}

/// <summary>
/// A lazy reference to one or more UI elements, resolved at action time with
/// auto-waiting.  Mirrors Playwright's <c>Locator</c> concept.
///
/// <para>
/// Breaking change from v0.1.x: <c>FirstAsync</c> and <c>NthAsync</c> have been
/// removed.  Use the sync properties <see cref="First"/>, <see cref="Last"/>, and
/// the sync method <see cref="Nth"/> to obtain sub-locators, then call action
/// methods on those.
/// </para>
/// </summary>
public interface IFlawrightLocator
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Gets the raw selector string used by this locator.</summary>
    string Selector { get; }

    // ── Sync chaining (Playwright contract: these are properties/methods returning Locator) ──

    /// <summary>Returns a locator that resolves to the first matching element.</summary>
    IFlawrightLocator First { get; }

    /// <summary>Returns a locator that resolves to the last matching element.</summary>
    IFlawrightLocator Last { get; }

    /// <summary>Returns a locator that resolves to the element at <paramref name="index"/> (0-based).</summary>
    /// <param name="index">Zero-based position in the matched result set.</param>
    IFlawrightLocator Nth(int index);

    // ── Scoped chaining ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns a new locator scoped to descendants matching <paramref name="selector"/>
    /// within the elements matched by this locator.
    /// </summary>
    IFlawrightLocator Locator(string selector);

    /// <summary>
    /// Returns a new locator scoped to descendants matched by <paramref name="inner"/>
    /// within the elements matched by this locator.
    /// </summary>
    IFlawrightLocator Locator(IFlawrightLocator inner);

    // ── Filtering ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a new locator that further filters the results of this locator
    /// using the provided options.
    /// </summary>
    IFlawrightLocator Filter(Locator.LocatorFilterOptions options);

    // ── Composition ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a locator that matches elements satisfying both this locator and
    /// <paramref name="other"/> (intersection).
    /// </summary>
#pragma warning disable CA1716 // 'And'/'Or' intentionally match the Playwright API convention
    IFlawrightLocator And(IFlawrightLocator other);

    /// <summary>
    /// Returns a locator that matches elements satisfying either this locator or
    /// <paramref name="other"/> (union).
    /// </summary>
    IFlawrightLocator Or(IFlawrightLocator other);
#pragma warning restore CA1716

    // ── Query helpers (sync, return new Locator) ──────────────────────────────

    /// <summary>Returns a locator for elements with the given ARIA role.</summary>
    IFlawrightLocator GetByRole(Selectors.AriaRole role, Locator.LocatorGetByRoleOptions? options = null);

    /// <summary>Returns a locator for elements with a label matching <paramref name="text"/>.</summary>
    IFlawrightLocator GetByLabel(string text, Locator.LocatorGetByLabelOptions? options = null);

    /// <summary>Returns a locator for elements whose visible text matches <paramref name="text"/>.</summary>
    IFlawrightLocator GetByText(string text, Locator.LocatorGetByTextOptions? options = null);

    /// <summary>Returns a locator for elements with a matching test ID (AutomationId).</summary>
    IFlawrightLocator GetByTestId(string testId);

    /// <summary>Returns a locator for elements with a placeholder matching <paramref name="text"/>.</summary>
    IFlawrightLocator GetByPlaceholder(string text, Locator.LocatorGetByPlaceholderOptions? options = null);

    /// <summary>Returns a locator for elements with a title matching <paramref name="text"/>.</summary>
    IFlawrightLocator GetByTitle(string text, Locator.LocatorGetByTitleOptions? options = null);

    // ── Async resolution ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current count of matching elements without auto-waiting.
    /// Returns 0 immediately if no elements are found.
    /// </summary>
    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>
    /// Auto-waits for at least one matching element, then returns all currently
    /// matching elements as <see cref="IFlawrightElement"/> handles.
    /// </summary>
    Task<IReadOnlyList<IFlawrightElement>> AllAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Returns the inner text of all currently matching elements without auto-waiting.
    /// </summary>
    Task<IReadOnlyList<string>> AllInnerTextsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the text content of all currently matching elements without auto-waiting.
    /// </summary>
    Task<IReadOnlyList<string>> AllTextContentsAsync(CancellationToken ct = default);

    // ── Async actions (options-based) ─────────────────────────────────────────

    /// <summary>Clicks the first matching element (auto-waited).</summary>
    Task ClickAsync(Locator.LocatorClickOptions? options = null, CancellationToken ct = default);

    /// <summary>Right-clicks the first matching element (auto-waited).</summary>
    /// <remarks>
    /// In virtual input mode this always throws
    /// <see cref="System.NotSupportedException"/> because UIA <c>InvokePattern</c>
    /// has no concept of which mouse button triggered the action.
    /// Use real input mode (the default) for right-click + context-menu scenarios.
    /// </remarks>
    Task RightClickAsync(Locator.LocatorClickOptions? options = null, CancellationToken ct = default);

    /// <summary>Double-clicks the first matching element (auto-waited).</summary>
    Task DoubleClickAsync(Locator.LocatorDoubleClickOptions? options = null, CancellationToken ct = default);

    /// <summary>Fills the first matching element with <paramref name="text"/> via ValuePattern (auto-waited).</summary>
    Task FillAsync(string text, Locator.LocatorFillOptions? options = null, CancellationToken ct = default);

    /// <summary>Clears the value of the first matching element (auto-waited).</summary>
    Task ClearAsync(Locator.LocatorClearOptions? options = null, CancellationToken ct = default);

    /// <summary>Types <paramref name="text"/> character-by-character into the first matching element (auto-waited).</summary>
    Task TypeAsync(string text, Locator.LocatorTypeOptions? options = null, CancellationToken ct = default);

    /// <summary>Types <paramref name="text"/> sequentially into the first matching element (auto-waited).</summary>
    Task PressSequentiallyAsync(string text, Locator.LocatorPressSequentiallyOptions? options = null, CancellationToken ct = default);

    /// <summary>Presses a key or chord on the first matching element (auto-waited).</summary>
    Task PressAsync(string key, Locator.LocatorPressOptions? options = null, CancellationToken ct = default);

    /// <summary>Checks the first matching toggle element (auto-waited).</summary>
    Task CheckAsync(Locator.LocatorCheckOptions? options = null, CancellationToken ct = default);

    /// <summary>Unchecks the first matching toggle element (auto-waited).</summary>
    Task UncheckAsync(Locator.LocatorUncheckOptions? options = null, CancellationToken ct = default);

    /// <summary>Sets the checked state of the first matching element (auto-waited).</summary>
#pragma warning disable CA1716
    Task SetCheckedAsync(bool @checked, Locator.LocatorSetCheckedOptions? options = null, CancellationToken ct = default);
#pragma warning restore CA1716

    /// <summary>Selects an option by string value in the first matching element (auto-waited).</summary>
    Task SelectOptionAsync(string value, Locator.LocatorSelectOptionOptions? options = null, CancellationToken ct = default);

    /// <summary>Selects an option by <see cref="Locator.SelectOptionValue"/> in the first matching element (auto-waited).</summary>
    Task SelectOptionAsync(Locator.SelectOptionValue value, Locator.LocatorSelectOptionOptions? options = null, CancellationToken ct = default);

    /// <summary>Hovers over the first matching element (auto-waited).</summary>
    Task HoverAsync(Locator.LocatorHoverOptions? options = null, CancellationToken ct = default);

    /// <summary>Focuses the first matching element (auto-waited).</summary>
    Task FocusAsync(CancellationToken ct = default);

    /// <summary>Removes focus from the first matching element (auto-waited).</summary>
    Task BlurAsync(CancellationToken ct = default);

    /// <summary>Drags the first matching element to <paramref name="target"/> (auto-waited).</summary>
    Task DragToAsync(IFlawrightLocator target, Locator.LocatorDragToOptions? options = null, CancellationToken ct = default);

    /// <summary>Scrolls the first matching element into view if needed (auto-waited).</summary>
    Task ScrollIntoViewIfNeededAsync(CancellationToken ct = default);

    /// <summary>
    /// Captures a screenshot of the first matching element.
    /// <para>
    /// <b>Note:</b> This method is a stub in Wave C. Screenshot capture requires
    /// <c>IElementBackend.Capture()</c> which will be added in Wave D.
    /// Currently returns an empty byte array.
    /// </para>
    /// </summary>
    Task<byte[]> ScreenshotAsync(Locator.LocatorScreenshotOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Captures a screenshot of the first matching element and saves it to
    /// <paramref name="path"/>.  Convenience overload — equivalent to passing
    /// <c>new LocatorScreenshotOptions { Path = path }</c>.
    /// </summary>
    /// <param name="path">File path where the PNG screenshot will be saved.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PNG image data as a byte array.</returns>
    Task<byte[]> ScreenshotAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Highlights the first matching element.
    /// <para><b>Note:</b> Stub in Wave C — Wave D will implement visual highlighting.</para>
    /// </summary>
    Task HighlightAsync(CancellationToken ct = default);

    // ── Read methods ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> if the first matching element is currently
    /// visible (present in the UIA tree and not off-screen).
    /// This is an instant probe with no auto-wait — returns <see langword="false"/>
    /// immediately if the element is not found.  For auto-waiting visibility checks
    /// use <c>Expect().ToBeVisibleAsync()</c> or
    /// <c>WaitForAsync(WaitForState.Visible)</c>.
    /// </summary>
    Task<bool> IsVisibleAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns <see langword="true"/> if the first matching element is hidden or absent.
    /// This is an instant probe with no auto-wait — returns <see langword="true"/>
    /// immediately if the element is not in the UIA tree (missing elements are
    /// treated as hidden, consistent with Playwright semantics).
    /// </summary>
    Task<bool> IsHiddenAsync(CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if the first matching element is enabled (auto-waited).</summary>
    Task<bool> IsEnabledAsync(CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if the first matching element is disabled (auto-waited).</summary>
    Task<bool> IsDisabledAsync(CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if the first matching element is checked (auto-waited).</summary>
    Task<bool> IsCheckedAsync(CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if the first matching element is editable (auto-waited).</summary>
    Task<bool> IsEditableAsync(CancellationToken ct = default);

    /// <summary>Returns the inner text of the first matching element (auto-waited).</summary>
    Task<string> InnerTextAsync(CancellationToken ct = default);

    /// <summary>Returns the text content of the first matching element (auto-waited), or <see langword="null"/>.</summary>
    Task<string?> TextContentAsync(CancellationToken ct = default);

    /// <summary>Returns the input value of the first matching element (auto-waited), or <see langword="null"/>.</summary>
    Task<string?> InputValueAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the display text of the currently selected item in a selection
    /// container (e.g. WPF <c>ComboBox</c>, <c>ListBox</c>) via
    /// <c>SelectionPattern.GetSelection()</c>, falling back to
    /// <c>ValuePattern.Value</c> for editable combo boxes.
    /// Returns <see langword="null"/> when no item is selected or the element
    /// does not support either pattern.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<string?> SelectedTextAsync(CancellationToken ct = default);

    /// <summary>Returns the value of a named attribute on the first matching element (auto-waited).</summary>
    Task<string?> GetAttributeAsync(string name, CancellationToken ct = default);

    /// <summary>Returns the bounding box of the first matching element (auto-waited), or <see langword="null"/>.</summary>
    Task<Locator.BoundingBox?> BoundingBoxAsync(CancellationToken ct = default);

    // ── Range value (Slider / Spinner) ────────────────────────────────────────

    /// <summary>
    /// Returns the current numeric value of a range control (e.g. WPF <c>Slider</c> or
    /// <c>NumericUpDown</c>) via <c>RangeValuePattern</c> (auto-waited).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current value.</returns>
    /// <exception cref="System.NotSupportedException">
    /// Thrown when the element does not support <c>RangeValuePattern</c>.
    /// </exception>
    Task<double> GetValueAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the numeric value of a range control (e.g. WPF <c>Slider</c> or
    /// <c>NumericUpDown</c>) via <c>RangeValuePattern</c> (auto-waited).
    /// </summary>
    /// <param name="value">The value to set.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="System.NotSupportedException">
    /// Thrown when the element does not support <c>RangeValuePattern</c>.
    /// </exception>
    Task SetValueAsync(double value, CancellationToken ct = default);

    // ── Wait for state ────────────────────────────────────────────────────────

    /// <summary>Waits for the first matching element to reach the specified state.</summary>
    Task WaitForAsync(Locator.LocatorWaitForOptions? options = null, CancellationToken ct = default);

    // ── Element handle ────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves this locator to a concrete element handle (auto-waited).
    /// Prefer locator-based actions over element handles.
    /// </summary>
    [Obsolete("Prefer locator-based actions; ElementHandle exists for advanced introspection only.")]
    Task<IFlawrightElement> ElementHandleAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    // ── Assertions ────────────────────────────────────────────────────────────

    /// <summary>Returns assertion helpers for this locator.</summary>
    IFlawrightAssertions Expect();
}

/// <summary>
/// A resolved UI element with async action methods.  Mirrors Playwright's
/// <c>ElementHandle</c> concept.
///
/// <para>
/// The interface exposes two surfaces:
/// <list type="bullet">
///   <item><description>
///     <b>Legacy surface</b> — parameterless or <see cref="CancellationToken"/>-only
///     overloads carried over from v0.1.x.  These are kept for build compatibility
///     while <c>FlawrightLocator</c> and <c>FlawrightAssertions</c> still reference
///     them. They will be <b>removed in Wave C</b> once callers are updated.
///     Methods in this group are marked <c>// REMOVE in Wave C</c>.
///   </description></item>
///   <item><description>
///     <b>New surface (Wave B.3)</b> — options-based overloads matching Playwright's
///     <c>Locator</c> action methods, plus new read-only properties and query methods
///     (<see cref="AutomationId"/>, <see cref="BoundingBoxAsync"/>, etc.).
///   </description></item>
/// </list>
/// </para>
/// </summary>
public interface IFlawrightElement
{
    // ── Legacy surface — REMOVE in Wave C ────────────────────────────────────

    /// <summary>Gets the locator that produced this element.</summary>
    /// <remarks>REMOVE in Wave C — callers should use the backend-native constructor.</remarks>
    IFlawrightLocator Locator { get; }

    /// <summary>
    /// Returns the element's visible text content.  Uses <c>ValuePattern</c>
    /// first (edit controls), then <c>TextPattern</c> (document/text
    /// controls), and falls back to the <c>Name</c> property.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>REMOVE in Wave C — superseded by <see cref="InnerTextAsync"/> / <see cref="TextContentAsync"/>.</remarks>
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

    // ── New surface (Wave B.3) — Identity / read ──────────────────────────────

    /// <summary>Gets the UIA AutomationId of the element, or <see langword="null"/>.</summary>
    string? AutomationId { get; }

    /// <summary>Gets the UIA Name of the element, or <see langword="null"/>.</summary>
    string? Name { get; }

    /// <summary>Gets the UIA ClassName of the element, or <see langword="null"/>.</summary>
    string? ClassName { get; }

    /// <summary>Gets the string name of the UIA ControlType (e.g. "Button", "Edit").</summary>
    string ControlTypeName { get; }

    /// <summary>Returns the bounding box of the element in screen coordinates, or <see langword="null"/> if the element has no screen presence.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<Locator.BoundingBox?> BoundingBoxAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the element's inner text.  Resolution order: <c>ValuePattern</c> →
    /// <c>TextPattern</c> → <c>Name</c>.  Returns <see cref="string.Empty"/> if
    /// all sources are null.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<string> InnerTextAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the element's text content, or <see langword="null"/> if all
    /// text sources are null.  Resolution order: <c>ValuePattern</c> →
    /// <c>TextPattern</c> → <c>Name</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<string?> TextContentAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the current value of an input element via <c>ValuePattern</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The value string.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the element does not support <c>ValuePattern</c> or
    /// <c>TextPattern</c> (i.e., it is not a text input).
    /// </exception>
    Task<string?> InputValueAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the display text of the currently selected item in a selection
    /// container (e.g. WPF <c>ComboBox</c>, <c>ListBox</c>).
    /// Resolution order: <c>SelectionPattern.GetSelection()</c> →
    /// <c>ValuePattern.Value</c> (editable combo).
    /// Returns <see langword="null"/> when no item is selected or neither pattern
    /// is supported.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<string?> SelectedTextAsync(CancellationToken ct = default);

    // ── New surface (Wave B.3) — State ────────────────────────────────────────

    /// <summary>Returns <see langword="true"/> if the element is hidden (off-screen).</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> IsHiddenAsync(CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if the element is disabled.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> IsDisabledAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns <see langword="true"/> if the element supports value input and is
    /// currently enabled.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> IsEditableAsync(CancellationToken ct = default);

    // ── New surface (Wave B.3) — Actions ──────────────────────────────────────

    /// <summary>Clicks the element with options.</summary>
    /// <param name="options">Click options (position, modifiers, etc.). <see langword="null"/> uses defaults.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ClickAsync(Locator.LocatorClickOptions? options = null, CancellationToken ct = default);

    /// <summary>Double-clicks the element with options.</summary>
    /// <param name="options">Double-click options. <see langword="null"/> uses defaults.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DoubleClickAsync(Locator.LocatorDoubleClickOptions? options = null, CancellationToken ct = default);

    /// <summary>Fills the element with <paramref name="text"/> via <c>ValuePattern</c>.</summary>
    /// <param name="text">Text to fill.</param>
    /// <param name="options">Fill options. <see langword="null"/> uses defaults.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the element does not support the ValuePattern.
    /// </exception>
    Task FillAsync(string text, Locator.LocatorFillOptions? options = null, CancellationToken ct = default);

    /// <summary>Clears the element's value.</summary>
    /// <param name="options">Clear options. <see langword="null"/> uses defaults.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ClearAsync(Locator.LocatorClearOptions? options = null, CancellationToken ct = default);

    /// <summary>Moves the mouse over the element.</summary>
    /// <param name="options">Hover options (position offset, modifiers, etc.). <see langword="null"/> uses defaults.</param>
    /// <param name="ct">Cancellation token.</param>
    Task HoverAsync(Locator.LocatorHoverOptions? options = null, CancellationToken ct = default);

    // ScrollIntoViewIfNeededAsync(CancellationToken) is already on the legacy surface above — no new overload needed.

    /// <summary>Checks the element (sets toggle state to <c>On</c>).</summary>
    /// <param name="options">Check options. <see langword="null"/> uses defaults.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the element does not support the TogglePattern.
    /// </exception>
    Task CheckAsync(Locator.LocatorCheckOptions? options = null, CancellationToken ct = default);

    /// <summary>Unchecks the element (sets toggle state to <c>Off</c>).</summary>
    /// <param name="options">Uncheck options. <see langword="null"/> uses defaults.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the element does not support the TogglePattern.
    /// </exception>
    Task UncheckAsync(Locator.LocatorUncheckOptions? options = null, CancellationToken ct = default);

    /// <summary>Sets the element's checked state.</summary>
    /// <param name="checked">The desired state: <see langword="true"/> to check, <see langword="false"/> to uncheck.</param>
    /// <param name="options">Options. <see langword="null"/> uses defaults.</param>
    /// <param name="ct">Cancellation token.</param>
#pragma warning disable CA1716 // Parameter name matches Playwright API convention
    Task SetCheckedAsync(bool @checked, Locator.LocatorSetCheckedOptions? options = null, CancellationToken ct = default);
#pragma warning restore CA1716

    /// <summary>Selects an item by value in a combo-box or list-box.</summary>
    /// <param name="value">The name or AutomationId of the item to select.</param>
    /// <param name="options">Options. <see langword="null"/> uses defaults.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the element does not support the SelectionPattern.
    /// </exception>
    Task SelectOptionAsync(string value, Locator.LocatorSelectOptionOptions? options = null, CancellationToken ct = default);
}

/// <summary>
/// Assertion helpers for a locator, providing Playwright-style
/// <c>expect(locator).toBeVisible()</c> semantics with auto-waiting.
/// Obtain via <see cref="IFlawrightLocator.Expect"/> or the static
/// <c>Assertions.Expect(locator)</c> entry point.
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

    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>Asserts that the element is visible (exists and is not off-screen).</summary>
    Task ToBeVisibleAsync(Assertions.AssertionsToBeVisibleOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is hidden (off-screen or absent).</summary>
    Task ToBeHiddenAsync(Assertions.AssertionsToBeHiddenOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is enabled.</summary>
    Task ToBeEnabledAsync(Assertions.AssertionsToBeEnabledOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is disabled.</summary>
    Task ToBeDisabledAsync(Assertions.AssertionsToBeDisabledOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is checked (toggle state is <c>On</c>).</summary>
    Task ToBeCheckedAsync(Assertions.AssertionsToBeCheckedOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element has keyboard focus.</summary>
    /// <remarks>
    /// Not yet supported by the active backend.  Full support requires
    /// <c>IElementBackend.HasKeyboardFocus</c>, planned for Wave D.
    /// This method currently throws <see cref="NotSupportedException"/>.
    /// </remarks>
    /// <exception cref="System.NotSupportedException">
    /// Thrown until UIA HasKeyboardFocus is wired.
    /// </exception>
    Task ToBeFocusedAsync(Assertions.AssertionsToBeFocusedOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is editable (supports value input and is enabled).</summary>
    Task ToBeEditableAsync(Assertions.AssertionsToBeEditableOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is empty (no value and no inner text).</summary>
    Task ToBeEmptyAsync(Assertions.AssertionsToBeEmptyOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is attached to the UI tree (count > 0).</summary>
    Task ToBeAttachedAsync(Assertions.AssertionsToBeAttachedOptions? options = null, CancellationToken ct = default);

    // ── Text ──────────────────────────────────────────────────────────────────

    /// <summary>Asserts that the element's inner text equals <paramref name="expected"/>.</summary>
    Task ToHaveTextAsync(string expected, Assertions.AssertionsToHaveTextOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's inner text matches <paramref name="expected"/>.</summary>
    Task ToHaveTextAsync(System.Text.RegularExpressions.Regex expected, Assertions.AssertionsToHaveTextOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's inner text contains <paramref name="expected"/>.</summary>
    Task ToContainTextAsync(string expected, Assertions.AssertionsToContainTextOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's inner text contains a match for <paramref name="expected"/>.</summary>
    Task ToContainTextAsync(System.Text.RegularExpressions.Regex expected, Assertions.AssertionsToContainTextOptions? options = null, CancellationToken ct = default);

    // ── Value ─────────────────────────────────────────────────────────────────

    /// <summary>Asserts that the element's value (via <c>ValuePattern</c>) equals <paramref name="expected"/>.</summary>
    Task ToHaveValueAsync(string expected, Assertions.AssertionsToHaveValueOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's value matches <paramref name="expected"/>.</summary>
    Task ToHaveValueAsync(System.Text.RegularExpressions.Regex expected, Assertions.AssertionsToHaveValueOptions? options = null, CancellationToken ct = default);

    // ── Count ─────────────────────────────────────────────────────────────────

    /// <summary>Asserts that the locator matches exactly <paramref name="expected"/> elements (auto-waited).</summary>
    Task ToHaveCountAsync(int expected, Assertions.AssertionsToHaveCountOptions? options = null, CancellationToken ct = default);

    // ── Attributes / identity ─────────────────────────────────────────────────

    /// <summary>Asserts that the named attribute equals <paramref name="expected"/>.</summary>
    Task ToHaveAttributeAsync(string name, string expected, Assertions.AssertionsToHaveAttributeOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the named attribute matches <paramref name="expected"/>.</summary>
    Task ToHaveAttributeAsync(string name, System.Text.RegularExpressions.Regex expected, Assertions.AssertionsToHaveAttributeOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's AutomationId equals <paramref name="expected"/>.</summary>
    Task ToHaveIdAsync(string expected, Assertions.AssertionsToHaveIdOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's AutomationId matches <paramref name="expected"/>.</summary>
    Task ToHaveIdAsync(System.Text.RegularExpressions.Regex expected, Assertions.AssertionsToHaveIdOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's ClassName equals <paramref name="expected"/>.</summary>
    Task ToHaveClassAsync(string expected, Assertions.AssertionsToHaveClassOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's ClassName matches <paramref name="expected"/>.</summary>
    Task ToHaveClassAsync(System.Text.RegularExpressions.Regex expected, Assertions.AssertionsToHaveClassOptions? options = null, CancellationToken ct = default);

    // ── Role / accessibility ──────────────────────────────────────────────────

    /// <summary>Asserts that the element's control type maps to <paramref name="expected"/> ARIA role.</summary>
    Task ToHaveRoleAsync(Selectors.AriaRole expected, Assertions.AssertionsToHaveRoleOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's accessible name (UIA Name) equals <paramref name="expected"/>.</summary>
    Task ToHaveAccessibleNameAsync(string expected, Assertions.AssertionsToHaveAccessibleNameOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's accessible name (UIA Name) matches <paramref name="expected"/>.</summary>
    Task ToHaveAccessibleNameAsync(System.Text.RegularExpressions.Regex expected, Assertions.AssertionsToHaveAccessibleNameOptions? options = null, CancellationToken ct = default);
}

/// <summary>
/// Negated counterpart of <see cref="IFlawrightAssertions"/>.  Each method
/// asserts the opposite condition.  Obtain via <see cref="IFlawrightAssertions.Not"/>.
/// </summary>
public interface IFlawrightNotAssertions
{
    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>Asserts that the element is <em>not</em> visible.</summary>
    Task ToBeVisibleAsync(Assertions.AssertionsToBeVisibleOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is <em>not</em> hidden.</summary>
    Task ToBeHiddenAsync(Assertions.AssertionsToBeHiddenOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is <em>not</em> enabled.</summary>
    Task ToBeEnabledAsync(Assertions.AssertionsToBeEnabledOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is <em>not</em> disabled.</summary>
    Task ToBeDisabledAsync(Assertions.AssertionsToBeDisabledOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is <em>not</em> checked.</summary>
    Task ToBeCheckedAsync(Assertions.AssertionsToBeCheckedOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element does <em>not</em> have keyboard focus.</summary>
    Task ToBeFocusedAsync(Assertions.AssertionsToBeFocusedOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is <em>not</em> editable.</summary>
    Task ToBeEditableAsync(Assertions.AssertionsToBeEditableOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is <em>not</em> empty.</summary>
    Task ToBeEmptyAsync(Assertions.AssertionsToBeEmptyOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element is <em>not</em> attached (count == 0).</summary>
    Task ToBeAttachedAsync(Assertions.AssertionsToBeAttachedOptions? options = null, CancellationToken ct = default);

    // ── Text ──────────────────────────────────────────────────────────────────

    /// <summary>Asserts that the element's inner text does <em>not</em> equal <paramref name="expected"/>.</summary>
    Task ToHaveTextAsync(string expected, Assertions.AssertionsToHaveTextOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's inner text does <em>not</em> match <paramref name="expected"/>.</summary>
    Task ToHaveTextAsync(System.Text.RegularExpressions.Regex expected, Assertions.AssertionsToHaveTextOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's inner text does <em>not</em> contain <paramref name="expected"/>.</summary>
    Task ToContainTextAsync(string expected, Assertions.AssertionsToContainTextOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's inner text does <em>not</em> contain a match for <paramref name="expected"/>.</summary>
    Task ToContainTextAsync(System.Text.RegularExpressions.Regex expected, Assertions.AssertionsToContainTextOptions? options = null, CancellationToken ct = default);

    // ── Value ─────────────────────────────────────────────────────────────────

    /// <summary>Asserts that the element's value does <em>not</em> equal <paramref name="expected"/>.</summary>
    Task ToHaveValueAsync(string expected, Assertions.AssertionsToHaveValueOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's value does <em>not</em> match <paramref name="expected"/>.</summary>
    Task ToHaveValueAsync(System.Text.RegularExpressions.Regex expected, Assertions.AssertionsToHaveValueOptions? options = null, CancellationToken ct = default);

    // ── Count ─────────────────────────────────────────────────────────────────

    /// <summary>Asserts that the locator does <em>not</em> match exactly <paramref name="expected"/> elements.</summary>
    Task ToHaveCountAsync(int expected, Assertions.AssertionsToHaveCountOptions? options = null, CancellationToken ct = default);

    // ── Attributes / identity ─────────────────────────────────────────────────

    /// <summary>Asserts that the named attribute does <em>not</em> equal <paramref name="expected"/>.</summary>
    Task ToHaveAttributeAsync(string name, string expected, Assertions.AssertionsToHaveAttributeOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the named attribute does <em>not</em> match <paramref name="expected"/>.</summary>
    Task ToHaveAttributeAsync(string name, System.Text.RegularExpressions.Regex expected, Assertions.AssertionsToHaveAttributeOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's AutomationId does <em>not</em> equal <paramref name="expected"/>.</summary>
    Task ToHaveIdAsync(string expected, Assertions.AssertionsToHaveIdOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's AutomationId does <em>not</em> match <paramref name="expected"/>.</summary>
    Task ToHaveIdAsync(System.Text.RegularExpressions.Regex expected, Assertions.AssertionsToHaveIdOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's ClassName does <em>not</em> equal <paramref name="expected"/>.</summary>
    Task ToHaveClassAsync(string expected, Assertions.AssertionsToHaveClassOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's ClassName does <em>not</em> match <paramref name="expected"/>.</summary>
    Task ToHaveClassAsync(System.Text.RegularExpressions.Regex expected, Assertions.AssertionsToHaveClassOptions? options = null, CancellationToken ct = default);

    // ── Role / accessibility ──────────────────────────────────────────────────

    /// <summary>Asserts that the element's control type does <em>not</em> map to <paramref name="expected"/> ARIA role.</summary>
    Task ToHaveRoleAsync(Selectors.AriaRole expected, Assertions.AssertionsToHaveRoleOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's accessible name does <em>not</em> equal <paramref name="expected"/>.</summary>
    Task ToHaveAccessibleNameAsync(string expected, Assertions.AssertionsToHaveAccessibleNameOptions? options = null, CancellationToken ct = default);

    /// <summary>Asserts that the element's accessible name does <em>not</em> match <paramref name="expected"/>.</summary>
    Task ToHaveAccessibleNameAsync(System.Text.RegularExpressions.Regex expected, Assertions.AssertionsToHaveAccessibleNameOptions? options = null, CancellationToken ct = default);
}
