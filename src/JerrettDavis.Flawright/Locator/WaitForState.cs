namespace JerrettDavis.Flawright.Locator;

/// <summary>
/// Describes what element state to wait for in
/// <c>IFlawrightLocator.WaitForAsync</c>.
/// </summary>
public enum WaitForState
{
    /// <summary>Wait until the element is visible (exists and not off-screen).</summary>
    Visible,

    /// <summary>Wait until the element is hidden (off-screen or absent).</summary>
    Hidden,

    /// <summary>Wait until the element exists in the UIA tree (regardless of visibility).</summary>
    Attached,

    /// <summary>Wait until the element is removed from the UIA tree.</summary>
    Detached
}
