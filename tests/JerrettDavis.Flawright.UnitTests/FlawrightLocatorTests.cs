using JerrettDavis.Flawright;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests;

public class BasicTypeTests
{
    [Fact]
    public void AssertionException_HasCorrectMessage()
    {
        var ex = new AssertionException("Expected element to be visible");
        Assert.Equal("Expected element to be visible", ex.Message);
    }

    [Fact]
    public void FlawrightElement_TypeExists()
    {
        var elementType = typeof(FlawrightElement);
        Assert.NotNull(elementType);
    }

    [Fact]
    public void FlawrightLocator_TypeExists()
    {
        var locatorType = typeof(FlawrightLocator);
        Assert.NotNull(locatorType);
    }

    [Fact]
    public void FlawrightAssertions_TypeExists()
    {
        var assertionsType = typeof(FlawrightAssertions);
        Assert.NotNull(assertionsType);
    }

    [Fact]
    public void FlawrightBrowser_TypeExists()
    {
        var browserType = typeof(FlawrightBrowser);
        Assert.NotNull(browserType);
    }

    [Fact]
    public void LaunchOptions_CanBeCreated()
    {
        var options = new LaunchOptions { ApplicationPath = "notepad.exe" };
        Assert.Equal("notepad.exe", options.ApplicationPath);
    }

    [Fact]
    public void AttachOptions_CanBeCreated()
    {
        var options = new AttachOptions { ProcessId = 1234 };
        Assert.Equal(1234, options.ProcessId);
    }

    [Fact]
    public async Task Flawright_CanBeCreated()
    {
        await using var flawright = await Flawright.CreateAsync();
        Assert.NotNull(flawright);
    }

    [Fact]
    public async Task Flawright_LaunchAsync_ReturnsBrowser()
    {
        await using var flawright = await Flawright.CreateAsync();
        var browser = await flawright.LaunchAsync(new LaunchOptions { ApplicationPath = "notepad.exe" });
        Assert.NotNull(browser);
    }
}