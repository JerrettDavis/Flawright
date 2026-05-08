using Flawright.Backends;
using Flawright.Input;

namespace Flawright.Page;

/// <summary>
/// Implements <see cref="IFlawrightKeyboard"/> by delegating to <see cref="IInputBackend"/>.
/// Exposes global keyboard operations mirroring Playwright's <c>Keyboard</c> class.
/// </summary>
internal sealed class FlawrightKeyboard : IFlawrightKeyboard
{
    private readonly IInputBackend _input;

    internal FlawrightKeyboard(IInputBackend input)
    {
        _input = input;
    }

    /// <inheritdoc/>
    public Task DownAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(key);
        var vk = KeyParser.ParseKey(key.Trim());
        _input.KeyboardPress(vk);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UpAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(key);
        var vk = KeyParser.ParseKey(key.Trim());
        _input.KeyboardRelease(vk);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task PressAsync(string key, KeyboardPressOptions? options = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(key);

        // Parse chord: "Ctrl+S" → modifiers=[CONTROL], mainKey=KEY_S
        var parts = key.Split('+');
        var modifierParts = parts[..^1];
        var mainKeyName = parts[^1].Trim();

        var modifierVks = modifierParts
            .Select(m => KeyParser.ParseModifier(m.Trim()))
            .ToArray();

        var mainVk = KeyParser.ParseKey(mainKeyName);

        // Press modifiers
        foreach (var mod in modifierVks)
            _input.KeyboardPress(mod);

        // Press and release main key
        _input.KeyboardPress(mainVk);

        if (options?.Delay is { } delay)
            await Task.Delay(delay, ct).ConfigureAwait(false);

        _input.KeyboardRelease(mainVk);

        // Release modifiers in reverse order
        foreach (var mod in modifierVks.Reverse())
            _input.KeyboardRelease(mod);
    }

    /// <inheritdoc/>
    public async Task TypeAsync(string text, KeyboardTypeOptions? options = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(text);

        if (options?.Delay is { } delay)
        {
            foreach (var ch in text)
            {
                ct.ThrowIfCancellationRequested();
                _input.KeyboardType(ch.ToString());
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
        else
        {
            _input.KeyboardType(text);
        }
    }

    /// <inheritdoc/>
    public Task InsertTextAsync(string text, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(text);
        _input.KeyboardType(text);
        return Task.CompletedTask;
    }
}
