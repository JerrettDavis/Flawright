using System.Windows;

namespace Flawright.E2ETests.TestApp;

/// <summary>
/// A second top-level window opened by the <c>btnSpawnWindow</c> button.
/// Used by multi-window E2E tests to exercise
/// <c>IFlawrightBrowser.GetAllPagesAsync</c> and
/// <c>IFlawrightBrowser.WaitForPageAsync(title)</c>.
/// </summary>
/// <remarks>
/// The window is shown without an owner so it appears as a genuinely
/// separate top-level window in the UIA tree.
/// Its <see cref="Window.Title"/> is fixed at <c>"Flawright Spawned Window"</c>
/// so tests can wait for it deterministically.
/// </remarks>
public partial class SpawnedWindow : Window
{
    /// <summary>Initialises the spawned window.</summary>
    public SpawnedWindow()
    {
        InitializeComponent();
    }
}
