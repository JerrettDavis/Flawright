using Reqnroll;

namespace Flawright.Reqnroll.NotepadMenuDemo;

/// <summary>
/// Custom step definitions for Notepad menu and dialog scenarios.
/// These supplement the built-in <c>FlawrightSteps</c> from Flawright.Reqnroll
/// with steps specific to Notepad's unsaved-changes dialog.
/// </summary>
/// <remarks>
/// <para>
/// The modern WinUI3 Notepad (Windows 11) exposes menus via UIA <c>Name</c>
/// properties such as <c>"name:File"</c> and <c>"name:Edit"</c>. If a machine
/// runs an older build that does not expose these names, the keyboard accelerator
/// <c>Alt+F</c> / <c>Alt+E</c> can be used instead via the built-in
/// <c>I press "Alt+F" globally</c> step.
/// </para>
/// <para>
/// The unsaved-changes dialog triggered by <c>Ctrl+W</c> (close tab) in modern
/// Notepad offers three buttons: <c>"Don't save"</c>, <c>"Save"</c>, and
/// <c>"Cancel"</c>. Classic Notepad uses <c>"Don't Save"</c> (capital S).
/// The steps below try the Win11 casing first, then fall back to the Win10 form.
/// </para>
/// </remarks>
[Binding]
#pragma warning disable CA1515 // Reqnroll discovers binding classes via reflection — public is required even in test assemblies
public sealed class NotepadMenuStepDefinitions
#pragma warning restore CA1515
{
    private readonly IFlawrightPage _page;

    public NotepadMenuStepDefinitions(IFlawrightPage page)
    {
        _page = page;
    }

    /// <summary>
    /// Sends Ctrl+W to close the current Notepad tab (or window on classic Notepad),
    /// which triggers the unsaved-changes dialog when there is modified content.
    /// </summary>
    /// <remarks>
    /// On modern WinUI3 Notepad, Ctrl+W closes the current tab and shows the dialog
    /// only for that tab's unsaved content. The Notepad window itself remains open.
    /// On classic Win32 Notepad (Windows 10), Ctrl+W is not bound — Alt+F4 is used
    /// as the fallback, which closes the window and shows the dialog.
    /// </remarks>
    [When(@"I trigger unsaved-changes close on Notepad")]
    public async Task TriggerUnsavedChangesCloseAsync()
    {
        // Modern WinUI3 Notepad: Ctrl+W closes the current tab.
        // Classic Notepad: Ctrl+W is not bound — use Alt+F4 instead.
        // We send Ctrl+W; if no dialog appears the scenario will timeout on the
        // next assertion, which is the correct failure signal for unsupported Notepad versions.
        await _page.Keyboard.PressAsync("Ctrl+W").ConfigureAwait(false);

        // Allow time for the dialog to appear before the next assertion step runs.
        await _page.WaitForTimeoutAsync(500).ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that the Notepad unsaved-changes dialog is visible.
    /// The dialog exposes at least one of the candidate button names.
    /// </summary>
    /// <remarks>
    /// Win11 Notepad labels the button <c>"Don't save"</c> (lowercase s).
    /// Win10 classic Notepad labels the button <c>"Don't Save"</c> (uppercase S).
    /// This step waits for whichever variant is present.
    /// </remarks>
    [Then(@"the unsaved-changes dialog should be visible")]
    public async Task UnsavedChangesDialogShouldBeVisibleAsync()
    {
        // Try Win11 casing first; fall back to Win10 casing.
        // WaitForSelectorAsync polls with auto-waiting, so we attempt
        // the most common form and catch any timeout to try the other.
        bool dialogFound = false;
        foreach (var buttonName in new[] { "name:Don't save", "name:Don't Save" })
        {
            try
            {
                await _page.WaitForSelectorAsync(buttonName).ConfigureAwait(false);
                dialogFound = true;
                break;
            }
#pragma warning disable CA1031 // Intentional: try each candidate name in turn
            catch (Exception)
#pragma warning restore CA1031
            {
                // Try the next candidate.
            }
        }

        if (!dialogFound)
        {
            throw new InvalidOperationException(
                "Unsaved-changes dialog did not appear. " +
                "Neither 'Don't save' nor 'Don't Save' button was found within the timeout. " +
                "This scenario targets Windows 11 WinUI3 Notepad or Windows 10 classic Notepad.");
        }
    }

    /// <summary>
    /// Clicks the named button in the unsaved-changes dialog, trying Win11
    /// casing (<c>"Don't save"</c>) and then Win10 casing (<c>"Don't Save"</c>).
    /// </summary>
    /// <param name="buttonLabel">The label passed from the Gherkin step — typically "Don't save".</param>
    [When(@"I click the ""([^""]*)"" button in the dialog")]
    public async Task ClickDialogButtonAsync(string buttonLabel)
    {
        // Build candidate selectors: exact name first, then Win10 case variant.
        var candidates = new[]
        {
            $"name:{buttonLabel}",
            // If the caller passed the Win11 form ("Don't save"), also try Win10 ("Don't Save").
            $"name:{char.ToUpperInvariant(buttonLabel[0])}{buttonLabel[1..]}",
        };

        foreach (var selector in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var locator = _page.Locator(selector);
                if (await locator.IsVisibleAsync().ConfigureAwait(false))
                {
                    await _page.ClickAsync(selector).ConfigureAwait(false);
                    return;
                }
            }
#pragma warning disable CA1031 // Intentional: try each candidate selector in turn
            catch (Exception)
#pragma warning restore CA1031
            {
                // Try the next candidate.
            }
        }

        throw new InvalidOperationException(
            $"Button '{buttonLabel}' (and common case variants) not found in the dialog. " +
            "Ensure the unsaved-changes dialog is visible before clicking a dialog button.");
    }
}
