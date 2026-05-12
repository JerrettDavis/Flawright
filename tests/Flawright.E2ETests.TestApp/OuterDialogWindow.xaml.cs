using System.Windows;

namespace Flawright.E2ETests.TestApp;

/// <summary>
/// Outer modal dialog used by nested-dialog E2E tests.
/// Contains a button that opens an <see cref="InnerDialogWindow"/> as a child dialog.
/// </summary>
public partial class OuterDialogWindow : Window
{
    /// <summary>Initialises the outer dialog.</summary>
    public OuterDialogWindow()
    {
        InitializeComponent();
    }

    private void BtnOpenInner_Click(object sender, RoutedEventArgs e)
    {
        var inner = new InnerDialogWindow { Owner = this };
        inner.ShowDialog();
    }
}
