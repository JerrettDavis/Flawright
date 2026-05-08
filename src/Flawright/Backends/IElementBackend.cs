using System.Drawing;

namespace Flawright.Backends;

/// <summary>
/// Backend seam that abstracts FlaUI's <c>AutomationElement</c>.
///
/// All production code (locators, assertions, element actions) should depend
/// only on this interface.  The sole FlaUI implementation is
/// <c>UiaElementBackend</c> in the <c>Backends.Uia</c> namespace.
/// Unit tests use <c>FakeElementBackend</c> from the test project.
/// </summary>
public interface IElementBackend
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Gets the UIA AutomationId of the element, or <see langword="null"/>.</summary>
    string? AutomationId { get; }

    /// <summary>Gets the UIA Name of the element, or <see langword="null"/>.</summary>
    string? Name { get; }

    /// <summary>Gets the UIA ClassName of the element, or <see langword="null"/>.</summary>
    string? ClassName { get; }

    /// <summary>Gets the string name of the UIA ControlType (e.g. "Button", "Edit").</summary>
    string ControlTypeName { get; }

    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>Gets whether the element is currently enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Gets whether the element is off-screen (hidden).</summary>
    bool IsOffscreen { get; }

    /// <summary>Gets the bounding rectangle of the element in screen coordinates.</summary>
    Rectangle BoundingRectangle { get; }

    // ── Actions ───────────────────────────────────────────────────────────────

    /// <summary>Clicks the element (single click, primary button).</summary>
    void Click();

    /// <summary>Double-clicks the element.</summary>
    void DoubleClick();

    /// <summary>Gives keyboard focus to the element.</summary>
    void Focus();

    // ── Pattern operations ────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to set the element's value via <c>ValuePattern</c> or the
    /// <c>AsTextBox()</c> helper.
    /// </summary>
    /// <param name="text">The text to set.</param>
    /// <returns><see langword="true"/> if the value was set; <see langword="false"/> if neither pattern is available.</returns>
    bool TrySetValue(string text);

    /// <summary>
    /// Attempts to retrieve the element's value via <c>ValuePattern</c>.
    /// </summary>
    /// <returns>The current value, or <see langword="null"/> if <c>ValuePattern</c> is not supported.</returns>
    string? TryGetValue();

    /// <summary>
    /// Attempts to retrieve the full document text via <c>TextPattern</c>.
    /// </summary>
    /// <returns>The document text, or <see langword="null"/> if <c>TextPattern</c> is not supported.</returns>
    string? TryGetDocumentText();

    /// <summary>
    /// Attempts to select the element via <c>SelectionItemPattern</c>.
    /// This is used for controls (e.g. WPF <c>RadioButton</c>) that implement
    /// <c>SelectionItemPattern</c> instead of <c>TogglePattern</c>.
    /// </summary>
    /// <returns><see langword="true"/> if the element was selected; <see langword="false"/> if <c>SelectionItemPattern</c> is not supported.</returns>
    bool TrySelect();

    /// <summary>
    /// Attempts to set the toggle state to <c>On</c> using <c>TogglePattern</c>.
    /// Loops up to two iterations if the state does not immediately change.
    /// </summary>
    /// <returns><see langword="true"/> if the element was toggled on (or was already on); <see langword="false"/> if <c>TogglePattern</c> is not supported.</returns>
    bool TryToggleOn();

    /// <summary>
    /// Attempts to set the toggle state to <c>Off</c> using <c>TogglePattern</c>.
    /// Loops up to two iterations if the state does not immediately change.
    /// </summary>
    /// <returns><see langword="true"/> if the element was toggled off (or was already off); <see langword="false"/> if <c>TogglePattern</c> is not supported.</returns>
    bool TryToggleOff();

    /// <summary>
    /// Gets the current toggle state.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> = On, <see langword="false"/> = Off,
    /// <see langword="null"/> = Indeterminate or pattern not supported.
    /// </returns>
    bool? GetToggleState();

    /// <summary>
    /// Attempts to scroll the element into view via <c>ScrollItemPattern</c>.
    /// </summary>
    /// <returns><see langword="true"/> if the pattern was invoked; <see langword="false"/> if not supported.</returns>
    bool TryScrollIntoView();

    /// <summary>
    /// Attempts to expand the element via <c>ExpandCollapsePattern</c>.
    /// Used before searching descendants of collapsible containers (e.g. WPF
    /// <c>ComboBox</c>) to materialise virtualised items in the UIA tree.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the pattern was invoked (the element was expanded or
    /// was already expanded); <see langword="false"/> if <c>ExpandCollapsePattern</c> is
    /// not supported.
    /// </returns>
    bool TryExpand();

    /// <summary>
    /// Finds a descendant by name or automation ID and selects it via
    /// <c>SelectionItemPattern</c>.
    /// </summary>
    /// <param name="nameOrId">The <c>Name</c> or <c>AutomationId</c> of the item to select.</param>
    /// <returns><see langword="true"/> if the item was found and selected; <see langword="false"/> otherwise.</returns>
    bool TrySelectItem(string nameOrId);

    /// <summary>
    /// Attempts to invoke the element's default action via UIA patterns
    /// (<c>InvokePattern</c>, falling back to <c>LegacyIAccessiblePattern.DoDefaultAction</c>).
    /// Returns <see langword="true"/> if a pattern was found and the invocation succeeded;
    /// <see langword="false"/> if no suitable pattern is implemented by the element.
    /// </summary>
    bool TryInvoke();

    // ── Tree traversal ────────────────────────────────────────────────────────

    /// <summary>
    /// Finds all descendants matching the given condition (native query + optional
    /// post-filter).
    /// </summary>
    /// <param name="condition">The condition produced by the selector parser or a <c>FakeElementCondition</c>.</param>
    /// <returns>All matching backends.</returns>
    IEnumerable<IElementBackend> FindAll(IElementCondition condition);

    /// <summary>
    /// Finds the first descendant matching the given condition.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <returns>The first match, or <see langword="null"/> if none.</returns>
    IElementBackend? FindFirst(IElementCondition condition);

    // ── Screenshot ────────────────────────────────────────────────────────────

    /// <summary>
    /// Captures a PNG screenshot of this element's window.
    /// </summary>
    /// <returns>
    /// A byte array containing the PNG image data, or an empty array when
    /// the element has no associated window handle (e.g. off-screen or
    /// zero-size bounding rectangle).
    /// </returns>
    byte[] CaptureScreenshot();
}
