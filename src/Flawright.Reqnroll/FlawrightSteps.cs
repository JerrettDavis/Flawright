using Flawright.Backends;
using Reqnroll;

namespace Flawright.Reqnroll;

/// <summary>
/// Built-in Reqnroll step bindings for Flawright Windows desktop UI automation.
/// </summary>
/// <remarks>
/// <para>
/// All steps are grouped by category:
/// <list type="bullet">
///   <item><description>Window focus</description></item>
///   <item><description>Mouse actions (click, double-click, right-click)</description></item>
///   <item><description>Text input (fill, type, clear)</description></item>
///   <item><description>Keyboard (global press/type, element press)</description></item>
///   <item><description>Element state (focus, hover)</description></item>
///   <item><description>Toggle/checkbox (check, uncheck)</description></item>
///   <item><description>Selection</description></item>
///   <item><description>Wait</description></item>
///   <item><description>Dialog interaction</description></item>
///   <item><description>Drag and drop</description></item>
///   <item><description>Assertions</description></item>
/// </list>
/// </para>
/// <para>
/// <see cref="IFlawrightPage"/> is injected via BoDi — it is registered by
/// <see cref="FlawrightReqnrollHooks.InitializeAsync"/> before any step runs.
/// </para>
/// <para>
/// To override a built-in step in your own bindings file, define a step with the
/// same (or a more specific) regex. Reqnroll resolves the most specific match.
/// </para>
/// </remarks>
[Binding]
public sealed class FlawrightSteps
{
    private readonly IFlawrightPage _page;
    /// <summary>
    /// Persists the dialog page returned by "I wait for dialog" until overwritten by the next invocation.
    /// Per-scenario (BoDi creates a new instance per scenario), so no cross-scenario leak.
    /// </summary>
    private IFlawrightPage? _dialogPage;

    /// <summary>
    /// Initialises a new instance of <see cref="FlawrightSteps"/> with the current
    /// scenario's page, injected by BoDi from
    /// <see cref="FlawrightReqnrollHooks.InitializeAsync"/>.
    /// </summary>
    /// <param name="page">The page (window) for the current scenario.</param>
    public FlawrightSteps(IFlawrightPage page)
    {
        _page = page;
    }

    // ── Window focus ──────────────────────────────────────────────────────────

    /// <summary>
    /// Brings the application window to the front (activates it).
    /// </summary>
    /// <example>
    /// <code>Given I have the application in focus</code>
    /// </example>
    [Given(@"I have the application in focus")]
    [When(@"I bring the application to the front")]
    public async Task BringToFrontAsync()
    {
        await _page.BringToFrontAsync().ConfigureAwait(false);
    }

    // ── Mouse actions ─────────────────────────────────────────────────────────

    /// <summary>
    /// Clicks the first element matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>When I click "name:OK"</code>
    /// </example>
    [When(@"I click ""([^""]*)""")]
    public async Task ClickAsync(string selector)
    {
        await _page.ClickAsync(selector).ConfigureAwait(false);
    }

    /// <summary>
    /// Double-clicks the first element matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>When I double-click "name:My Item"</code>
    /// </example>
    [When(@"I double-click ""([^""]*)""")]
    public async Task DoubleClickAsync(string selector)
    {
        await _page.DoubleClickAsync(selector).ConfigureAwait(false);
    }

    /// <summary>
    /// Right-clicks the first element matching <paramref name="selector"/>.
    /// Right-click is simulated by clicking with the right mouse button; Flawright
    /// routes this through the element's click action with a right-button modifier.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>When I right-click "name:My File"</code>
    /// </example>
    [When(@"I right-click ""([^""]*)""")]
    public async Task RightClickAsync(string selector)
    {
        await _page.ClickAsync(
            selector,
            new Locator.LocatorClickOptions { Button = MouseButton.Right })
            .ConfigureAwait(false);
    }

    // ── Text input ────────────────────────────────────────────────────────────

    /// <summary>
    /// Fills the element matching <paramref name="selector"/> with <paramref name="value"/>
    /// via the UIA <c>ValuePattern</c> (fast, single-shot assignment).
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <param name="value">Text to set.</param>
    /// <example>
    /// <code>When I fill "#RichEditBox" with "Hello from Flawright!"</code>
    /// </example>
    [When(@"I fill ""([^""]*)"" with ""([^""]*)""")]
    public async Task FillAsync(string selector, string value)
    {
        await _page.FillAsync(selector, value).ConfigureAwait(false);
    }

    /// <summary>
    /// Types <paramref name="text"/> character-by-character into the element matching
    /// <paramref name="selector"/> (simulates realistic key events).
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <param name="text">Text to type.</param>
    /// <example>
    /// <code>When I type "hello world" into "#RichEditBox"</code>
    /// </example>
    [When(@"I type ""([^""]*)"" into ""([^""]*)""")]
    public async Task TypeIntoAsync(string text, string selector)
    {
        await _page.TypeAsync(selector, text).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears the value of the element matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>When I clear "#RichEditBox"</code>
    /// </example>
    [When(@"I clear ""([^""]*)""")]
    public async Task ClearAsync(string selector)
    {
        await _page.Locator(selector).ClearAsync().ConfigureAwait(false);
    }

    // ── Keyboard ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a global key press (not focused on any particular element).
    /// Useful for shortcuts like <c>Ctrl+S</c> or <c>Alt+F4</c>.
    /// </summary>
    /// <param name="key">Playwright-style key string, e.g. <c>Ctrl+S</c>.</param>
    /// <example>
    /// <code>When I press "Ctrl+S" globally</code>
    /// </example>
    [When(@"I press ""([^""]*)"" globally")]
    public async Task PressGlobalAsync(string key)
    {
        await _page.Keyboard.PressAsync(key).ConfigureAwait(false);
    }

    /// <summary>
    /// Focuses the element matching <paramref name="selector"/> and sends a key press.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <param name="key">Playwright-style key string, e.g. <c>Enter</c> or <c>Ctrl+A</c>.</param>
    /// <example>
    /// <code>When I press "Enter" on "#SubmitButton"</code>
    /// </example>
    [When(@"I press ""([^""]*)"" on ""([^""]*)""")]
    public async Task PressOnAsync(string key, string selector)
    {
        await _page.PressAsync(selector, key).ConfigureAwait(false);
    }

    /// <summary>
    /// Types <paramref name="text"/> globally using the keyboard (not focused on an element).
    /// </summary>
    /// <param name="text">Text to type.</param>
    /// <example>
    /// <code>When I type "Hello World" globally</code>
    /// </example>
    [When(@"I type ""([^""]*)"" globally")]
    public async Task TypeGlobalAsync(string text)
    {
        await _page.Keyboard.TypeAsync(text).ConfigureAwait(false);
    }

    // ── Element state ─────────────────────────────────────────────────────────

    /// <summary>
    /// Gives keyboard focus to the element matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>When I focus "#SearchBox"</code>
    /// </example>
    [When(@"I focus ""([^""]*)""")]
    public async Task FocusAsync(string selector)
    {
        await _page.FocusAsync(selector).ConfigureAwait(false);
    }

    /// <summary>
    /// Moves the mouse pointer over the element matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>When I hover over "name:Help"</code>
    /// </example>
    [When(@"I hover over ""([^""]*)""")]
    public async Task HoverAsync(string selector)
    {
        await _page.HoverAsync(selector).ConfigureAwait(false);
    }

    // ── Toggle / checkbox ─────────────────────────────────────────────────────

    /// <summary>
    /// Checks (sets to <c>On</c>) the toggle element matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>When I check "name:Enable Dark Mode"</code>
    /// </example>
    [When(@"I check ""([^""]*)""")]
    public async Task CheckAsync(string selector)
    {
        await _page.CheckAsync(selector).ConfigureAwait(false);
    }

    /// <summary>
    /// Unchecks (sets to <c>Off</c>) the toggle element matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>When I uncheck "name:Show Notifications"</code>
    /// </example>
    [When(@"I uncheck ""([^""]*)""")]
    public async Task UncheckAsync(string selector)
    {
        await _page.UncheckAsync(selector).ConfigureAwait(false);
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Selects the option identified by <paramref name="value"/> in the combo-box or
    /// list-box matching <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <param name="value">Value string of the item to select.</param>
    /// <example>
    /// <code>When I select "Dark" from "name:Theme"</code>
    /// </example>
    [When(@"I select ""([^""]*)"" from ""([^""]*)""")]
    public async Task SelectOptionAsync(string value, string selector)
    {
        await _page.SelectOptionAsync(selector, value).ConfigureAwait(false);
    }

    // ── Wait ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Waits for <paramref name="milliseconds"/> milliseconds before continuing.
    /// </summary>
    /// <param name="milliseconds">Duration in milliseconds.</param>
    /// <example>
    /// <code>When I wait for 500 milliseconds</code>
    /// </example>
    [When(@"I wait for (\d+) milliseconds?")]
    public async Task WaitAsync(int milliseconds)
    {
        await _page.WaitForTimeoutAsync(milliseconds).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for an element matching <paramref name="selector"/> to appear in the UI tree.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>When I wait for selector "name:Loading Complete"</code>
    /// </example>
    [When(@"I wait for selector ""([^""]*)""")]
    public async Task WaitForSelectorAsync(string selector)
    {
        await _page.WaitForSelectorAsync(selector).ConfigureAwait(false);
    }

    // ── Dialog interaction ────────────────────────────────────────────────────

    /// <summary>
    /// Waits for a dialog window owned by the current page whose title contains
    /// <paramref name="titlePattern"/>, then stores it in scenario state for
    /// subsequent dialog steps.
    /// </summary>
    /// <param name="titlePattern">Substring to match against the dialog title (case-insensitive).</param>
    /// <example>
    /// <code>When I wait for dialog "Save changes?"</code>
    /// </example>
    [When(@"I wait for dialog ""([^""]*)""")]
    public async Task WaitForDialogWithTitleAsync(string titlePattern)
    {
        _dialogPage = await _page.WaitForDialogAsync(titlePattern).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for any dialog window owned by the current page and stores it in
    /// scenario state for subsequent dialog steps.
    /// </summary>
    /// <example>
    /// <code>When I wait for dialog</code>
    /// </example>
    [When(@"I wait for dialog")]
    public async Task WaitForDialogAsync()
    {
        _dialogPage = await _page.WaitForDialogAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that a dialog page was captured by the most recent
    /// <c>I wait for dialog</c> step.
    /// </summary>
    /// <example>
    /// <code>Then a dialog should be visible</code>
    /// </example>
    [Then(@"a dialog should be visible")]
    public Task DialogShouldBeVisibleAsync()
    {
        if (_dialogPage == null)
        {
            throw new AssertionException(
                "No dialog has been captured. " +
                "Use 'When I wait for dialog' before asserting dialog visibility.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clicks the first element matching <paramref name="selector"/> inside the
    /// dialog captured by the most recent <c>I wait for dialog</c> step.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>When I click "name:Cancel" in dialog</code>
    /// </example>
    [When(@"I click ""([^""]*)"" in dialog")]
    public async Task ClickInDialogAsync(string selector)
    {
        EnsureDialogPage();
        await _dialogPage!.ClickAsync(selector).ConfigureAwait(false);
    }

    /// <summary>
    /// Fills the element matching <paramref name="selector"/> inside the dialog
    /// captured by the most recent <c>I wait for dialog</c> step with <paramref name="value"/>.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <param name="value">Text to fill.</param>
    /// <example>
    /// <code>When I fill "name:FileName" in dialog with "report.txt"</code>
    /// </example>
    [When(@"I fill ""([^""]*)"" in dialog with ""([^""]*)""")]
    public async Task FillInDialogAsync(string selector, string value)
    {
        EnsureDialogPage();
        await _dialogPage!.FillAsync(selector, value).ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that the element matching <paramref name="selector"/> is visible
    /// inside the dialog captured by the most recent <c>I wait for dialog</c> step.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>Then "name:Cancel" should be visible in dialog</code>
    /// </example>
    [Then(@"""([^""]*)"" should be visible in dialog")]
    public async Task ShouldBeVisibleInDialogAsync(string selector)
    {
        EnsureDialogPage();
        await _dialogPage!.Locator(selector).Expect().ToBeVisibleAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that the element matching <paramref name="selector"/> inside the
    /// dialog contains the text <paramref name="expected"/> (substring match).
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <param name="expected">Expected substring.</param>
    /// <example>
    /// <code>Then "name:Message" should contain "unsaved" in dialog</code>
    /// </example>
    [Then(@"""([^""]*)"" should contain ""([^""]*)"" in dialog")]
    public async Task ShouldContainTextInDialogAsync(string selector, string expected)
    {
        EnsureDialogPage();
        await _dialogPage!.Locator(selector).Expect().ToContainTextAsync(expected).ConfigureAwait(false);
    }

    // ── Drag and drop ─────────────────────────────────────────────────────────

    /// <summary>
    /// Drags the element matching <paramref name="source"/> and drops it onto the element
    /// matching <paramref name="target"/>.
    /// </summary>
    /// <param name="source">Selector for the element to drag.</param>
    /// <param name="target">Selector for the drop target.</param>
    /// <example>
    /// <code>When I drag "name:Item A" to "name:Folder B"</code>
    /// </example>
    [When(@"I drag ""([^""]*)"" to ""([^""]*)""")]
    public async Task DragAndDropAsync(string source, string target)
    {
        await _page.DragAndDropAsync(source, target).ConfigureAwait(false);
    }

    // ── Assertions ────────────────────────────────────────────────────────────

    /// <summary>
    /// Asserts that the element matching <paramref name="selector"/> is visible.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>Then "name:Save" should be visible</code>
    /// </example>
    [Then(@"""([^""]*)"" should be visible")]
    public async Task ShouldBeVisibleAsync(string selector)
    {
        await _page.Locator(selector).Expect().ToBeVisibleAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that the element matching <paramref name="selector"/> is hidden.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>Then "name:Spinner" should be hidden</code>
    /// </example>
    [Then(@"""([^""]*)"" should be hidden")]
    public async Task ShouldBeHiddenAsync(string selector)
    {
        await _page.Locator(selector).Expect().ToBeHiddenAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that the element matching <paramref name="selector"/> is enabled.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>Then "name:Submit" should be enabled</code>
    /// </example>
    [Then(@"""([^""]*)"" should be enabled")]
    public async Task ShouldBeEnabledAsync(string selector)
    {
        await _page.Locator(selector).Expect().ToBeEnabledAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that the element matching <paramref name="selector"/> is disabled.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>Then "name:Submit" should be disabled</code>
    /// </example>
    [Then(@"""([^""]*)"" should be disabled")]
    public async Task ShouldBeDisabledAsync(string selector)
    {
        await _page.Locator(selector).Expect().ToBeDisabledAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that the toggle element matching <paramref name="selector"/> is checked.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>Then "name:Remember Me" should be checked</code>
    /// </example>
    [Then(@"""([^""]*)"" should be checked")]
    public async Task ShouldBeCheckedAsync(string selector)
    {
        await _page.Locator(selector).Expect().ToBeCheckedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that the element matching <paramref name="selector"/> contains the text
    /// <paramref name="expected"/> (substring match).
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <param name="expected">Expected substring.</param>
    /// <example>
    /// <code>Then "#RichEditBox" should contain "Hello"</code>
    /// </example>
    [Then(@"""([^""]*)"" should contain ""([^""]*)""")]
    public async Task ShouldContainTextAsync(string selector, string expected)
    {
        await _page.Locator(selector).Expect().ToContainTextAsync(expected).ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that the element matching <paramref name="selector"/> has exactly the text
    /// <paramref name="expected"/> (full-text equality).
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <param name="expected">Expected full text.</param>
    /// <example>
    /// <code>Then "name:Status" should have text "Ready"</code>
    /// </example>
    [Then(@"""([^""]*)"" should have text ""([^""]*)""")]
    public async Task ShouldHaveTextAsync(string selector, string expected)
    {
        await _page.Locator(selector).Expect().ToHaveTextAsync(expected).ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that the input element matching <paramref name="selector"/> has the value
    /// <paramref name="expected"/> via the UIA <c>ValuePattern</c>.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <param name="expected">Expected value string.</param>
    /// <example>
    /// <code>Then "#AmountBox" should have value "42"</code>
    /// </example>
    [Then(@"""([^""]*)"" should have value ""([^""]*)""")]
    public async Task ShouldHaveValueAsync(string selector, string expected)
    {
        await _page.Locator(selector).Expect().ToHaveValueAsync(expected).ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that the element matching <paramref name="selector"/> is empty (no value
    /// and no inner text).
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <example>
    /// <code>Then "#SearchBox" should be empty</code>
    /// </example>
    [Then(@"""([^""]*)"" should be empty")]
    public async Task ShouldBeEmptyAsync(string selector)
    {
        await _page.Locator(selector).Expect().ToBeEmptyAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that exactly <paramref name="expected"/> elements match <paramref name="selector"/>.
    /// </summary>
    /// <param name="selector">Flawright selector string.</param>
    /// <param name="expected">Expected element count.</param>
    /// <example>
    /// <code>Then "controltype:ListItem" should have count 3</code>
    /// </example>
    [Then(@"""([^""]*)"" should have count (\d+)")]
    public async Task ShouldHaveCountAsync(string selector, int expected)
    {
        await _page.Locator(selector).Expect().ToHaveCountAsync(expected).ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that the window title is exactly <paramref name="expected"/>.
    /// </summary>
    /// <param name="expected">Expected window title.</param>
    /// <example>
    /// <code>Then the window title should be "Notepad"</code>
    /// </example>
    [Then(@"the window title should be ""([^""]*)""")]
    public async Task WindowTitleShouldBeAsync(string expected)
    {
        var title = await _page.TitleAsync().ConfigureAwait(false);
        if (!string.Equals(title, expected, StringComparison.Ordinal))
        {
            throw new AssertionException(
                $"Expected window title to be \"{expected}\" but found \"{title}\".");
        }
    }

    /// <summary>
    /// Asserts that the window title contains <paramref name="expected"/> as a substring.
    /// </summary>
    /// <param name="expected">Expected substring of the window title.</param>
    /// <example>
    /// <code>Then the window title should contain "Notepad"</code>
    /// </example>
    [Then(@"the window title should contain ""([^""]*)""")]
    public async Task WindowTitleShouldContainAsync(string expected)
    {
        var title = await _page.TitleAsync().ConfigureAwait(false);
        if (!title.Contains(expected, StringComparison.Ordinal))
        {
            throw new AssertionException(
                $"Expected window title to contain \"{expected}\" but found \"{title}\".");
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Throws <see cref="AssertionException"/> when no dialog page has been captured
    /// by a preceding <c>I wait for dialog</c> step.
    /// </summary>
    private void EnsureDialogPage()
    {
        if (_dialogPage == null)
        {
            throw new AssertionException(
                "No dialog has been captured. " +
                "Use 'When I wait for dialog' or 'When I wait for dialog \"title\"' " +
                "before interacting with dialog elements.");
        }
    }
}
