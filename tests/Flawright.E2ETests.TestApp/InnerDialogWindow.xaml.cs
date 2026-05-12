using System.Windows;

namespace Flawright.E2ETests.TestApp;

/// <summary>
/// Inner modal dialog opened from <see cref="OuterDialogWindow"/> to exercise nested-dialog E2E scenarios.
/// </summary>
public partial class InnerDialogWindow : Window
{
    /// <summary>Initialises the inner dialog.</summary>
    public InnerDialogWindow()
    {
        InitializeComponent();
    }

    private void BtnCloseInner_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
