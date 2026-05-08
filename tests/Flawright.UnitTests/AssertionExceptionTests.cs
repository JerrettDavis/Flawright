using Xunit;

namespace Flawright.UnitTests;

/// <summary>Tests for <see cref="AssertionException"/>.</summary>
public class AssertionExceptionTests
{
    [Fact]
    public void DefaultCtor_CreatesInstance()
    {
        var ex = new AssertionException();
        Assert.NotNull(ex);
    }

    [Fact]
    public void MessageCtor_PreservesMessage()
    {
        const string Msg = "Expected element to be visible";

        var ex = new AssertionException(Msg);

        Assert.Equal(Msg, ex.Message);
    }

    [Fact]
    public void MessageInnerCtor_PreservesMessageAndInner()
    {
        var inner = new InvalidOperationException("inner cause");
        const string Msg = "Assertion failed";

        var ex = new AssertionException(Msg, inner);

        Assert.Equal(Msg, ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void IsSubclassOfException()
    {
        var ex = new AssertionException("test");
        Assert.IsAssignableFrom<Exception>(ex);
    }

    [Fact]
    public void CanBeCaughtAsException()
    {
        Exception? caught = null;

        try
        {
            throw new AssertionException("caught me");
        }
        catch (Exception e)
        {
            caught = e;
        }

        Assert.NotNull(caught);
        Assert.Equal("caught me", caught!.Message);
    }
}
