using System.Windows;

namespace Flawright.E2ETests.TestApp;

/// <summary>
/// Modal "Save changes?" dialog used to exercise
/// <see cref="Flawright.CloseBehaviors.DismissDialogCloseBehavior"/>
/// without depending on Notepad's dirty-buffer state.
/// </summary>
/// <remarks>
/// Clicking <b>Don't Save</b> closes both this dialog and its owner
/// <see cref="MainWindow"/> — replicating the Notepad pattern where
/// dismissing the save prompt causes the application to exit.
/// </remarks>
public partial class SaveChangesDialog : Window
{
    /// <summary>Initialises the dialog.</summary>
    public SaveChangesDialog()
    {
        InitializeComponent();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BtnDontSave_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();

        // Close the owner window so the application exits — this is the
        // pattern DismissDialogCloseBehavior is designed to handle.
        Owner?.Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = null;
        Close();
    }
}
