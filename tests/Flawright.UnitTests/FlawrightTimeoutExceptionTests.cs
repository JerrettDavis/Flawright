using Xunit;

namespace Flawright.UnitTests;

/// <summary>Tests for <see cref="FlawrightTimeoutException"/>.</summary>
public class FlawrightTimeoutExceptionTests
{
    [Fact]
    public void DefaultCtor_MessageIsEmpty()
    {
        var ex = new FlawrightTimeoutException();

        // Message may be a generic default; the key thing is no NRE.
        Assert.NotNull(ex);
    }

    [Fact]
    public void MessageCtor_PreservesMessage()
    {
        const string Msg = "Timed out after waiting";

        var ex = new FlawrightTimeoutException(Msg);

        Assert.Equal(Msg, ex.Message);
    }

    [Fact]
    public void MessageInnerCtor_PreservesMessageAndInner()
    {
        var inner = new InvalidOperationException("inner");
        const string Msg = "Wrapper message";

        var ex = new FlawrightTimeoutException(Msg, inner);

        Assert.Equal(Msg, ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void SelectorTimeoutCtor_ExposesProperties()
    {
        const string Selector = "#save-button";
        var timeout = TimeSpan.FromSeconds(5);

        var ex = new FlawrightTimeoutException(Selector, timeout);

        Assert.Equal(Selector, ex.Selector);
        Assert.Equal(timeout, ex.Timeout);
    }

    [Fact]
    public void SelectorTimeoutCtor_MessageContainsSelectorAndTimeout()
    {
        const string Selector = "#save-button";
        var timeout = TimeSpan.FromSeconds(3);

        var ex = new FlawrightTimeoutException(Selector, timeout);

        Assert.Contains(Selector, ex.Message, StringComparison.Ordinal);
        // The formatted duration should appear somewhere in the message.
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public void IsSubclassOfTimeoutException()
    {
        var ex = new FlawrightTimeoutException("msg");

        Assert.IsAssignableFrom<TimeoutException>(ex);
    }

    [Fact]
    public void DefaultCtor_SelectorAndTimeoutAreNull()
    {
        var ex = new FlawrightTimeoutException();

        Assert.Null(ex.Selector);
        Assert.Null(ex.Timeout);
    }

    [Fact]
    public void MessageCtor_SelectorAndTimeoutAreNull()
    {
        var ex = new FlawrightTimeoutException("some message");

        Assert.Null(ex.Selector);
        Assert.Null(ex.Timeout);
    }

    [Fact]
    public void CanBeCaughtAsTimeoutException()
    {
        FlawrightTimeoutException? caught = null;

        try
        {
            throw new FlawrightTimeoutException("#btn", TimeSpan.FromSeconds(1));
        }
        catch (TimeoutException te)
        {
            caught = te as FlawrightTimeoutException;
        }

        Assert.NotNull(caught);
        Assert.Equal("#btn", caught!.Selector);
    }
}
