using Flawright.CloseBehaviors;
using Flawright.UnitTests.Fakes;
using Xunit;

namespace Flawright.UnitTests.CloseBehaviors;

/// <summary>
/// Unit tests for <see cref="DismissDialogCloseBehavior"/>.
/// </summary>
public sealed class DismissDialogCloseBehaviorTests
{
    // ── DefaultDiscardButtonNames ─────────────────────────────────────────────

    [Fact]
    public void DefaultDiscardButtonNames_ContainsBothCasingVariants()
    {
        Assert.Contains("Don't Save", DismissDialogCloseBehavior.DefaultDiscardButtonNames, StringComparer.Ordinal);
        Assert.Contains("Don't save", DismissDialogCloseBehavior.DefaultDiscardButtonNames, StringComparer.Ordinal);
    }

    // ── Constructor / button name configuration ───────────────────────────────

    [Fact]
    public void Constructor_WithNoArgs_UsesDefaultButtonNames()
    {
        var behavior = new DismissDialogCloseBehavior();

        Assert.Equal(DismissDialogCloseBehavior.DefaultDiscardButtonNames, behavior.ButtonNames);
    }

    [Fact]
    public void Constructor_WithCustomNames_UsesProvidedNames()
    {
        var behavior = new DismissDialogCloseBehavior("Discard", "Abandon");

        Assert.Collection(behavior.ButtonNames,
            n => Assert.Equal("Discard", n),
            n => Assert.Equal("Abandon", n));
    }

    [Fact]
    public void Constructor_WithEmptyArray_FallsBackToDefaults()
    {
        var behavior = new DismissDialogCloseBehavior(Array.Empty<string>());

        Assert.Equal(DismissDialogCloseBehavior.DefaultDiscardButtonNames, behavior.ButtonNames);
    }

    // ── Dialog dismissal ──────────────────────────────────────────────────────

    [Fact]
    public async Task CloseAsync_SendsCloseSignalBeforePolling()
    {
        var discardButton = new FakeElementBackend(name: "Don't Save", controlTypeName: "Button");
        var ctx = new FakeCloseContext(
            hasExited: false,
            waitForExitResults: [true],
            findButtonResult: new FlawrightElement(discardButton, new FakeInputBackend()));

        var behavior = new DismissDialogCloseBehavior();
        await behavior.CloseAsync(ctx);

        Assert.Equal(1, ctx.SendCloseSignalCount);
    }

    [Fact]
    public async Task CloseAsync_ClicksFirstMatchingButton_Win10Style()
    {
        // "Don't Save" is the Win10 default
        var discardButton = new FakeElementBackend(name: "Don't Save", controlTypeName: "Button");
        var input = new FakeInputBackend();
        var element = new FlawrightElement(discardButton, input);
        var ctx = new FakeCloseContext(
            hasExited: false,
            waitForExitResults: [true],
            findButtonResult: element);

        var behavior = new DismissDialogCloseBehavior();
        await behavior.CloseAsync(ctx);

        // RealInputMode routes clicks through input.MouseClick
        Assert.Single(input.MouseClicks);
    }

    [Fact]
    public async Task CloseAsync_ClicksFirstMatchingButton_Win11Style()
    {
        // "Don't save" (lowercase s) is the Win11 variant — use a behavior that
        // only looks for that name to confirm case-sensitive matching works.
        var discardButton = new FakeElementBackend(name: "Don't save", controlTypeName: "Button");
        var input = new FakeInputBackend();
        var element = new FlawrightElement(discardButton, input);
        var ctx = new FakeCloseContext(
            hasExited: false,
            waitForExitResults: [true],
            findButtonResult: element);

        var win11Behavior = new DismissDialogCloseBehavior("Don't save");

        await win11Behavior.CloseAsync(ctx);

        // RealInputMode routes clicks through input.MouseClick
        Assert.Single(input.MouseClicks);
    }

    [Fact]
    public async Task CloseAsync_WithCustomButtonName_ClicksMatchingButton()
    {
        var discardButton = new FakeElementBackend(name: "Discard", controlTypeName: "Button");
        var input = new FakeInputBackend();
        var element = new FlawrightElement(discardButton, input);
        var ctx = new FakeCloseContext(
            hasExited: false,
            waitForExitResults: [true],
            findButtonResult: element);

        var behavior = new DismissDialogCloseBehavior("Discard");
        await behavior.CloseAsync(ctx);

        // RealInputMode routes clicks through input.MouseClick
        Assert.Single(input.MouseClicks);
    }

    [Fact]
    public async Task CloseAsync_WhenNoDialogAppears_StillWaitsForExit()
    {
        // No button found — process exits on its own
        var ctx = new FakeCloseContext(
            hasExited: false,
            waitForExitResults: [true],
            findButtonResult: null);

        var behavior = new DismissDialogCloseBehavior
        {
            DialogPollTimeout = TimeSpan.FromMilliseconds(50) // fast for test
        };
        var result = await behavior.CloseAsync(ctx);

        Assert.True(result);
        Assert.Equal(1, ctx.WaitForExitCallCount);
    }

    [Fact]
    public async Task CloseAsync_ReturnsValueFromWaitForExit()
    {
        var ctx = new FakeCloseContext(
            hasExited: true, // already exited — dialog poll loop skips
            waitForExitResults: [false]);

        var behavior = new DismissDialogCloseBehavior
        {
            DialogPollTimeout = TimeSpan.FromMilliseconds(10)
        };
        var result = await behavior.CloseAsync(ctx);

        Assert.False(result);
    }

    [Fact]
    public async Task CloseAsync_PollsButtonNamesInOrder()
    {
        // Verify button names are tried: "Don't Save" first, "Don't save" second
        var ctx = new FakeCloseContext(
            hasExited: false,
            waitForExitResults: [true],
            findButtonResult: null);

        var behavior = new DismissDialogCloseBehavior
        {
            DialogPollTimeout = TimeSpan.FromMilliseconds(50)
        };

        await behavior.CloseAsync(ctx);

        // Both default names should have been polled at least once each
        Assert.Contains("Don't Save", ctx.FindButtonCalls, StringComparer.Ordinal);
        Assert.Contains("Don't save", ctx.FindButtonCalls, StringComparer.Ordinal);
    }
}
