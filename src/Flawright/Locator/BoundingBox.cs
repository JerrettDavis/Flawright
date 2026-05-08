namespace Flawright.Locator;

/// <summary>
/// Represents the bounding box of an element in screen coordinates.
/// </summary>
/// <param name="X">Left edge of the bounding box.</param>
/// <param name="Y">Top edge of the bounding box.</param>
/// <param name="Width">Width of the bounding box.</param>
/// <param name="Height">Height of the bounding box.</param>
public sealed record BoundingBox(double X, double Y, double Width, double Height);
