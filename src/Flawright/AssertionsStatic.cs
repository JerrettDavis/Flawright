using Flawright.Page;

namespace Flawright;

/// <summary>
/// Static entry points for Flawright assertions, mirroring Playwright's
/// <c>Assertions</c> class.
///
/// <para>
/// This class is the canonical entry point for assertion creation.
/// It is named <c>AssertionsStatic</c> to avoid ambiguity with the
/// <c>Flawright.Assertions</c> namespace which holds options records.
/// </para>
/// </summary>
/// <example>
/// <code>
/// using static Flawright.AssertionsStatic;
///
/// await Expect(page.Locator("#save")).ToBeVisibleAsync();
/// await Expect(page).ToHaveTitleAsync("My App");
/// </code>
/// </example>
public static class AssertionsStatic
{
    /// <summary>
    /// Creates an assertions object for the given locator.
    /// </summary>
    /// <param name="locator">The locator to assert against.</param>
    /// <returns>An <see cref="IFlawrightAssertions"/> scoped to <paramref name="locator"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="locator"/> is <see langword="null"/>.
    /// </exception>
    public static IFlawrightAssertions Expect(IFlawrightLocator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        return locator.Expect();
    }

    /// <summary>
    /// Creates a page assertions object for the given page.
    /// </summary>
    /// <param name="page">The page to assert against.</param>
    /// <returns>An <see cref="IFlawrightPageAssertions"/> scoped to <paramref name="page"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="page"/> is <see langword="null"/>.
    /// </exception>
    public static IFlawrightPageAssertions Expect(IFlawrightPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return new FlawrightPageAssertions(page, page.Options);
    }
}
