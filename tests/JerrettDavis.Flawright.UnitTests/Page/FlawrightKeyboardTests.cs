using FlaUI.Core.WindowsAPI;
using JerrettDavis.Flawright.Page;
using JerrettDavis.Flawright.UnitTests.Fakes;
using Xunit;

namespace JerrettDavis.Flawright.UnitTests.Page;

/// <summary>
/// Unit tests for <see cref="FlawrightKeyboard"/>.
/// </summary>
public sealed class FlawrightKeyboardTests
{
    private static (FlawrightKeyboard Keyboard, FakeInputBackend Input) Make()
    {
        var input = new FakeInputBackend();
        return (new FlawrightKeyboard(input), input);
    }

    // ── DownAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DownAsync_PressesKey()
    {
        var (kb, input) = Make();
        await kb.DownAsync("Enter");
        Assert.Contains(VirtualKeyShort.ENTER, input.KeyPresses);
    }

    [Fact]
    public async Task DownAsync_ParsesEscapeKey()
    {
        var (kb, input) = Make();
        await kb.DownAsync("Escape");
        Assert.Contains(VirtualKeyShort.ESCAPE, input.KeyPresses);
    }

    [Fact]
    public async Task DownAsync_ThrowsOnEmptyKey()
    {
        var (kb, _) = Make();
        await Assert.ThrowsAsync<ArgumentException>(() => kb.DownAsync(""));
    }

    [Fact]
    public async Task DownAsync_ThrowsOnNullKey()
    {
        var (kb, _) = Make();
        await Assert.ThrowsAsync<ArgumentNullException>(() => kb.DownAsync(null!));
    }

    [Fact]
    public async Task DownAsync_RespectsCancellation()
    {
        var (kb, _) = Make();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => kb.DownAsync("A", cts.Token));
    }

    // ── UpAsync ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpAsync_ReleasesKey()
    {
        var (kb, input) = Make();
        await kb.UpAsync("A");
        Assert.Contains(VirtualKeyShort.KEY_A, input.KeyReleases);
    }

    [Fact]
    public async Task UpAsync_ThrowsOnEmptyKey()
    {
        var (kb, _) = Make();
        await Assert.ThrowsAsync<ArgumentException>(() => kb.UpAsync(""));
    }

    [Fact]
    public async Task UpAsync_ThrowsOnNullKey()
    {
        var (kb, _) = Make();
        await Assert.ThrowsAsync<ArgumentNullException>(() => kb.UpAsync(null!));
    }

    [Fact]
    public async Task UpAsync_RespectsCancellation()
    {
        var (kb, _) = Make();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => kb.UpAsync("A", cts.Token));
    }

    // ── PressAsync — single key ───────────────────────────────────────────────

    [Fact]
    public async Task PressAsync_SingleKey_PressesAndReleasesKey()
    {
        var (kb, input) = Make();
        await kb.PressAsync("Enter");
        Assert.Contains(VirtualKeyShort.ENTER, input.KeyPresses);
        Assert.Contains(VirtualKeyShort.ENTER, input.KeyReleases);
    }

    [Fact]
    public async Task PressAsync_SingleKey_NoModifiers()
    {
        var (kb, input) = Make();
        await kb.PressAsync("A");
        // No modifier presses
        Assert.DoesNotContain(VirtualKeyShort.CONTROL, input.KeyPresses);
        Assert.DoesNotContain(VirtualKeyShort.SHIFT, input.KeyPresses);
        Assert.DoesNotContain(VirtualKeyShort.ALT, input.KeyPresses);
    }

    // ── PressAsync — chord ────────────────────────────────────────────────────

    [Fact]
    public async Task PressAsync_CtrlS_PressesCtrlThenS()
    {
        var (kb, input) = Make();
        await kb.PressAsync("Ctrl+S");

        // CONTROL should be pressed before KEY_S
        var ctrlIdx = input.KeyPresses.ToList().IndexOf(VirtualKeyShort.CONTROL);
        var sIdx = input.KeyPresses.ToList().IndexOf(VirtualKeyShort.KEY_S);
        Assert.True(ctrlIdx >= 0, "CONTROL not pressed");
        Assert.True(sIdx >= 0, "S not pressed");
        Assert.True(ctrlIdx < sIdx, "CONTROL should be pressed before S");
    }

    [Fact]
    public async Task PressAsync_CtrlS_ReleasesInReverseOrder()
    {
        var (kb, input) = Make();
        await kb.PressAsync("Ctrl+S");

        var releases = input.KeyReleases.ToList();
        var sReleaseIdx = releases.IndexOf(VirtualKeyShort.KEY_S);
        var ctrlReleaseIdx = releases.LastIndexOf(VirtualKeyShort.CONTROL);

        Assert.True(sReleaseIdx >= 0, "S not released");
        Assert.True(ctrlReleaseIdx >= 0, "CONTROL not released");
        Assert.True(sReleaseIdx < ctrlReleaseIdx, "S should be released before CONTROL");
    }

    [Fact]
    public async Task PressAsync_CtrlShiftZ_PressesAllModifiers()
    {
        var (kb, input) = Make();
        await kb.PressAsync("Ctrl+Shift+Z");

        Assert.Contains(VirtualKeyShort.CONTROL, input.KeyPresses);
        Assert.Contains(VirtualKeyShort.SHIFT, input.KeyPresses);
        Assert.Contains(VirtualKeyShort.KEY_Z, input.KeyPresses);
    }

    [Fact]
    public async Task PressAsync_AltF4_PressesAltAndF4()
    {
        var (kb, input) = Make();
        await kb.PressAsync("Alt+F4");

        Assert.Contains(VirtualKeyShort.ALT, input.KeyPresses);
        Assert.Contains(VirtualKeyShort.F4, input.KeyPresses);
    }

    [Fact]
    public async Task PressAsync_RespectsCancellation()
    {
        var (kb, _) = Make();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => kb.PressAsync("Enter", ct: cts.Token));
    }

    [Fact]
    public async Task PressAsync_ThrowsOnEmptyOrNullKey()
    {
        var (kb, _) = Make();
        await Assert.ThrowsAsync<ArgumentException>(() => kb.PressAsync(""));
        await Assert.ThrowsAsync<ArgumentNullException>(() => kb.PressAsync(null!));
    }

    // ── PressAsync — delay ────────────────────────────────────────────────────

    [Fact]
    public async Task PressAsync_WithDelay_WaitsBeforeRelease()
    {
        var (kb, input) = Make();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await kb.PressAsync("Enter", new KeyboardPressOptions { Delay = TimeSpan.FromMilliseconds(50) });
        sw.Stop();

        // Should have waited the delay
        Assert.True(sw.ElapsedMilliseconds >= 40);
        // Key should still be pressed and released
        Assert.Contains(VirtualKeyShort.ENTER, input.KeyPresses);
        Assert.Contains(VirtualKeyShort.ENTER, input.KeyReleases);
    }

    // ── TypeAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task TypeAsync_TypesTextWithoutDelay()
    {
        var (kb, input) = Make();
        await kb.TypeAsync("hello");
        Assert.Contains("hello", input.TypedTexts);
    }

    [Fact]
    public async Task TypeAsync_TypesTextAsOneChunk_WhenNoDelay()
    {
        var (kb, input) = Make();
        await kb.TypeAsync("abc");
        // With no delay, should be a single KeyboardType call
        Assert.Single(input.TypedTexts);
        Assert.Equal("abc", input.TypedTexts[0]);
    }

    [Fact]
    public async Task TypeAsync_TypesCharByChar_WhenDelaySet()
    {
        var (kb, input) = Make();
        await kb.TypeAsync("abc", new KeyboardTypeOptions { Delay = TimeSpan.FromMilliseconds(10) });
        // With delay, each character is typed individually
        Assert.Equal(3, input.TypedTexts.Count);
        Assert.Equal("a", input.TypedTexts[0]);
        Assert.Equal("b", input.TypedTexts[1]);
        Assert.Equal("c", input.TypedTexts[2]);
    }

    [Fact]
    public async Task TypeAsync_WithDelay_TakesTime()
    {
        var (kb, _) = Make();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await kb.TypeAsync("ab", new KeyboardTypeOptions { Delay = TimeSpan.FromMilliseconds(30) });
        sw.Stop();
        // 2 chars × 30ms = 60ms minimum
        Assert.True(sw.ElapsedMilliseconds >= 40);
    }

    [Fact]
    public async Task TypeAsync_ThrowsOnNullText()
    {
        var (kb, _) = Make();
        await Assert.ThrowsAsync<ArgumentNullException>(() => kb.TypeAsync(null!));
    }

    [Fact]
    public async Task TypeAsync_EmptyString_TypesNothing()
    {
        var (kb, input) = Make();
        await kb.TypeAsync(string.Empty);
        // Empty string: either no call or one empty-string call
        Assert.True(input.TypedTexts.Count == 0 || input.TypedTexts.All(t => string.IsNullOrEmpty(t)));
    }

    [Fact]
    public async Task TypeAsync_RespectsCancellation_WithDelay()
    {
        var (kb, _) = Make();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(20);
        // "abcdef" with 30ms delay should get cancelled mid-way
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => kb.TypeAsync("abcdef", new KeyboardTypeOptions { Delay = TimeSpan.FromMilliseconds(30) }, cts.Token));
    }

    // ── InsertTextAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task InsertTextAsync_InsertsText()
    {
        var (kb, input) = Make();
        await kb.InsertTextAsync("quick text");
        Assert.Contains("quick text", input.TypedTexts);
    }

    [Fact]
    public async Task InsertTextAsync_ThrowsOnNullText()
    {
        var (kb, _) = Make();
        await Assert.ThrowsAsync<ArgumentNullException>(() => kb.InsertTextAsync(null!));
    }

    [Fact]
    public async Task InsertTextAsync_RespectsCancellation()
    {
        var (kb, _) = Make();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => kb.InsertTextAsync("text", cts.Token));
    }
}
