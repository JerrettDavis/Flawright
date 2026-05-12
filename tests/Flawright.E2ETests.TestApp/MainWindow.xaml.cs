using System;
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

    // ── btnShowDialogNoOwner ──────────────────────────────────────────────────

    private void BtnShowDialogNoOwner_Click(object sender, RoutedEventArgs e)
    {
        // Use a plain inline Window so the Title is unambiguous — SaveChangesDialog's
        // XAML hard-codes Title="Save changes?" and AutomationProperties.Name="Save changes?",
        // which can shadow the programmatic Title override before the window is shown.
        var d = new System.Windows.Window
        {
            Title = "Ownerless Dialog",
            Width = 300,
            Height = 150,
            Content = new System.Windows.Controls.TextBlock
            {
                Text = "Ownerless dialog body",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
            },
        };
        // Intentionally no Owner assignment — this is the test of an ownerless dialog.
        d.ShowDialog();
    }

    // ── btnShowModelessOwned ──────────────────────────────────────────────────

    private void BtnShowModelessOwned_Click(object sender, RoutedEventArgs e)
    {
        var w = new Window
        {
            Owner = this,
            Title = "Modeless Owned Window",
            Width = 300,
            Height = 120,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new System.Windows.Controls.TextBlock
            {
                Text = "Modeless owned window",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
            },
        };
        w.Show();
    }

    // ── btnShowToolWindow ─────────────────────────────────────────────────────

    private void BtnShowToolWindow_Click(object sender, RoutedEventArgs e)
    {
        var w = new Window
        {
            Owner = this,
            Title = "Tool Window",
            WindowStyle = WindowStyle.ToolWindow,
            Width = 280,
            Height = 100,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new System.Windows.Controls.TextBlock
            {
                Text = "Tool window",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
            },
        };
        w.Show();
    }

    // ── btnShowNestedDialog ───────────────────────────────────────────────────

    private void BtnShowNestedDialog_Click(object sender, RoutedEventArgs e)
    {
        var outer = new OuterDialogWindow { Owner = this };
        outer.ShowDialog();
    }

    // ── btnShowWinFormsModal ──────────────────────────────────────────────────

#pragma warning disable CA1303 // WinForms Text properties are UI labels; localisation not required for test fixtures
    private void BtnShowWinFormsModal_Click(object sender, RoutedEventArgs e)
    {
        var form = new System.Windows.Forms.Form
        {
            Text = "WinForms Modal",
            Width = 300,
            Height = 150,
        };
        var btn = new System.Windows.Forms.Button
        {
            Text = "Close",
            Dock = System.Windows.Forms.DockStyle.Bottom,
        };
        btn.Click += (_, _) => form.Close();
        form.Controls.Add(btn);
        form.ShowDialog(new Win32WindowAdapter(this));
    }
#pragma warning restore CA1303

    // ── btnShowWinFormsModeless ───────────────────────────────────────────────

#pragma warning disable CA1303 // WinForms Text properties are UI labels; localisation not required for test fixtures
    private void BtnShowWinFormsModeless_Click(object sender, RoutedEventArgs e)
    {
        var form = new System.Windows.Forms.Form
        {
            Text = "WinForms Modeless",
            Width = 300,
            Height = 150,
        };
        var btn = new System.Windows.Forms.Button
        {
            Text = "Close",
            Dock = System.Windows.Forms.DockStyle.Bottom,
        };
        btn.Click += (_, _) => form.Close();
        form.Controls.Add(btn);
        form.Show();
    }
#pragma warning restore CA1303

    // ── btnShowMessageBox ─────────────────────────────────────────────────────

    private void BtnShowMessageBox_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("Test message", "Test MessageBox", MessageBoxButton.OKCancel);
    }

    // ── btnShowOpenFileDialog ─────────────────────────────────────────────────
    // Note: System.Windows.Forms.OpenFileDialog is used here instead of
    // Microsoft.Win32.OpenFileDialog because Windows 11 / Server 2025 ships a
    // new XAML-based file picker (PickerHost.exe) for the Win32 variant that
    // runs out-of-process and therefore does not appear in GetOwnedWindowsAsync.
    // The WinForms picker uses the legacy in-process comdlg32 path reliably.
#pragma warning disable CA1303 // WinForms Title properties are UI labels; localisation not required for test fixtures
    private void BtnShowOpenFileDialog_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new System.Windows.Forms.OpenFileDialog { Title = "Open" };
        dlg.ShowDialog(new Win32WindowAdapter(this));
    }
#pragma warning restore CA1303

    // ── btnExit ───────────────────────────────────────────────────────────────

    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }
}

/// <summary>
/// Adapts a WPF <see cref="Window"/> as a <see cref="System.Windows.Forms.IWin32Window"/>
/// owner handle for WinForms dialogs shown from a WPF app.
/// </summary>
internal sealed class Win32WindowAdapter : System.Windows.Forms.IWin32Window
{
    /// <inheritdoc/>
    public IntPtr Handle { get; }

    /// <summary>Initialises the adapter from a WPF owner window.</summary>
    public Win32WindowAdapter(Window owner)
        => Handle = new System.Windows.Interop.WindowInteropHelper(owner).Handle;
}
