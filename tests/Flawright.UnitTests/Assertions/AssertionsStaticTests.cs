using NSubstitute;
using Xunit;

namespace Flawright.UnitTests.Assertions;

/// <summary>
/// Tests for the static <see cref="AssertionsStatic"/> entry point.
/// </summary>
public sealed class AssertionsStaticTests
{
    private static IFlawrightLocator Locator(string selector = "test")
    {
        var loc = Substitute.For<IFlawrightLocator>();
        loc.Selector.Returns(selector);
        loc.Expect().Returns(Substitute.For<IFlawrightAssertions>());
        return loc;
    }

    [Fact]
    public void Expect_ReturnsNonNull()
    {
        var loc = Locator();
        var result = AssertionsStatic.Expect(loc);
        Assert.NotNull(result);
    }

    [Fact]
    public void Expect_DelegatesToLocator_Expect()
    {
        var loc = Locator();
        _ = AssertionsStatic.Expect(loc);
        loc.Received(1).Expect();
    }

    [Fact]
    public void Expect_ThrowsArgumentNullException_WhenLocatorIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => AssertionsStatic.Expect((IFlawrightLocator)null!));
    }

    [Fact]
    public void Expect_ReturnsIFlawrightAssertions()
    {
        var loc = Locator();
        var result = AssertionsStatic.Expect(loc);
        Assert.IsAssignableFrom<IFlawrightAssertions>(result);
    }

    [Fact]
    public async Task Expect_ChainToNotWorks()
    {
        // Arrange: set up a real locator mock and a real FlawrightAssertions instance.
        var opts = new FlawrightOptions
        {
            DefaultTimeout = TimeSpan.FromMilliseconds(50),
            DefaultRetryInterval = TimeSpan.FromMilliseconds(10),
        };

        var loc = Substitute.For<IFlawrightLocator>();
        loc.Selector.Returns("selector");
        loc.Expect().Returns(new FlawrightAssertions(loc, opts));
        loc.IsVisibleAsync(Arg.Any<CancellationToken>()).Returns(false);

        // Act: Expect -> Not -> ToBeVisible should pass (since element is NOT visible)
        var assertions = AssertionsStatic.Expect(loc);
        await assertions.Not.ToBeVisibleAsync();
    }
}
