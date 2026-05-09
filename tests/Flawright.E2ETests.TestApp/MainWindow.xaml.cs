using System.Windows;
using System.Windows.Input;

namespace Flawright.E2ETests.TestApp;

/// <summary>
/// Deterministic WPF test target for Flawright E2E tests.
/// </summary>
/// <remarks>
/// <para>
/// Every interactive control on this window has both a predictable
/// <c>x:Name</c> and an explicit <c>AutomationProperties.AutomationId</c>
/// so Flawright can locate it by automation ID, by name, or by control type
/// without relying on system apps that may not be present on CI runners.
/// </para>
/// <para>
/// The <c>btnShowDialog</c> button opens a modal <see cref="SaveChangesDialog"/>
/// that replicates the "Save changes?" pattern used by Notepad, so
/// <see cref="Flawright.CloseBehaviors.DismissDialogCloseBehavior"/> tests do
/// not depend on Notepad's dirty-buffer state.
/// </para>
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>Initialises the main window.</summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    // ── btnClick ──────────────────────────────────────────────────────────────

    private void BtnClick_Click(object sender, RoutedEventArgs e)
    {
        lblOutput.Text = "Clicked";
    }

    // ── btnDoubleClick ────────────────────────────────────────────────────────

    private void BtnDoubleClick_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        lblOutput.Text = "DoubleClicked";
    }

    // ── btnShowDialog ─────────────────────────────────────────────────────────

    private void BtnShowDialog_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveChangesDialog { Owner = this };
        dialog.ShowDialog();
    }

    // ── btnSpawnWindow ────────────────────────────────────────────────────────

    /// <summary>
    /// Opens a second top-level <see cref="SpawnedWindow"/> in the same process
    /// so multi-window tests can exercise <c>GetAllPagesAsync</c> and
    /// <c>WaitForPageAsync(title)</c>.
    /// </summary>
    /// <remarks>
    /// The spawned window has a deterministic <see cref="Window.Title"/>
    /// (<c>"Flawright Spawned Window"</c>) and is shown without an owner so it
    /// appears as a separate top-level window in the UIA tree.
    /// </remarks>
    private void BtnSpawnWindow_Click(object sender, RoutedEventArgs e)
    {
        var window = new SpawnedWindow();
        window.Show();
    }

    // ── btnExit ───────────────────────────────────────────────────────────────

    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
