using Reqnroll;

namespace Flawright.Reqnroll.NotepadMenuDemo;

/// <summary>
/// Custom step definitions for Notepad menu and dialog scenarios.
/// These supplement the built-in <c>FlawrightSteps</c> from Flawright.Reqnroll
/// with steps specific to Notepad's unsaved-changes trigger.
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
/// Dialog interaction (wait, assert, click, fill) is handled by the built-in
/// dialog steps in <c>FlawrightSteps</c> from Flawright.Reqnroll.
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
}
