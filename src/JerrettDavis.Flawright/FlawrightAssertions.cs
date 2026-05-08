using System.Text.RegularExpressions;
using JerrettDavis.Flawright.Assertions;
using JerrettDavis.Flawright.Selectors;

namespace JerrettDavis.Flawright;

/// <summary>
/// Assertion helpers that implement Playwright-style
/// <c>expect(locator).toBeVisible()</c> semantics with auto-waiting.
/// Obtain via <see cref="IFlawrightLocator.Expect"/> or
/// <see cref="AssertionsStatic.Expect(IFlawrightLocator)"/>.
/// </summary>
/// <example>
/// <code>
/// await Assertions.Expect(page.Locator("#save")).ToBeVisibleAsync();
/// await page.Locator("#save").Expect().Not.ToBeDisabledAsync();
/// </code>
/// </example>
internal sealed partial class FlawrightAssertions : IFlawrightAssertions
{
    private readonly IFlawrightLocator _locator;
    private readonly FlawrightOptions _opts;
    private readonly bool _negated;

    internal FlawrightAssertions(IFlawrightLocator locator, FlawrightOptions opts, bool negated = false)
    {
        _locator = locator;
        _opts = opts;
        _negated = negated;
    }

    /// <inheritdoc/>
#pragma warning disable CA1716
    public IFlawrightNotAssertions Not => new FlawrightNotAssertions(_locator, _opts);
#pragma warning restore CA1716

    // ── Helpers ───────────────────────────────────────────────────────────────

    private TimeSpan ResolveTimeout(TimeSpan? perCall) => perCall ?? _opts.DefaultTimeout;

    /// <summary>
    /// Polls <paramref name="predicate"/> with auto-wait until it returns the
    /// expected boolean (true for positive assertions, false for negated).
    /// On timeout, throws <see cref="AssertionException"/> with <paramref name="message"/>.
    /// </summary>
    private async Task AssertAsync(
        Func<CancellationToken, Task<bool>> predicate,
        TimeSpan timeout,
        string message,
        CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            bool result = false;
            try
            {
                result = await predicate(ct).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Swallow transient failures; timeout surfaces the real error
            catch (Exception) when (!ct.IsCancellationRequested)
#pragma warning restore CA1031
            {
                // element not ready — keep polling
            }

            // For positive assertions: success when result is true.
            // For negated assertions: success when result is false.
            bool success = _negated ? !result : result;

            if (success)
                return;

            if (DateTime.UtcNow >= deadline)
                throw new AssertionException(message);

            await Task.Delay(_opts.DefaultRetryInterval, ct).ConfigureAwait(false);
        }
    }

    private string Pos(string readable) =>
        _negated
            ? $"Expected locator '{_locator.Selector}' NOT {readable}"
            : $"Expected locator '{_locator.Selector}' {readable}";

    // ── State assertions ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task ToBeVisibleAsync(AssertionsToBeVisibleOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok => await _locator.IsVisibleAsync(tok).ConfigureAwait(false),
            ResolveTimeout(options?.Timeout),
            Pos("to be visible"),
            ct);

    /// <inheritdoc/>
    public Task ToBeHiddenAsync(AssertionsToBeHiddenOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok => await _locator.IsHiddenAsync(tok).ConfigureAwait(false),
            ResolveTimeout(options?.Timeout),
            Pos("to be hidden"),
            ct);

    /// <inheritdoc/>
    public Task ToBeEnabledAsync(AssertionsToBeEnabledOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok => await _locator.IsEnabledAsync(tok).ConfigureAwait(false),
            ResolveTimeout(options?.Timeout),
            Pos("to be enabled"),
            ct);

    /// <inheritdoc/>
    public Task ToBeDisabledAsync(AssertionsToBeDisabledOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok => await _locator.IsDisabledAsync(tok).ConfigureAwait(false),
            ResolveTimeout(options?.Timeout),
            Pos("to be disabled"),
            ct);

    /// <inheritdoc/>
    public Task ToBeCheckedAsync(AssertionsToBeCheckedOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok => await _locator.IsCheckedAsync(tok).ConfigureAwait(false),
            ResolveTimeout(options?.Timeout),
            Pos("to be checked"),
            ct);

    /// <inheritdoc/>
    /// <remarks>
    /// Not yet supported by the active backend.  Full implementation requires
    /// <c>IElementBackend.HasKeyboardFocus</c>, flagged for Wave D backend support.
    /// </remarks>
    public Task ToBeFocusedAsync(AssertionsToBeFocusedOptions? options = null, CancellationToken ct = default)
        => throw new NotSupportedException(
            "ToBeFocusedAsync is not yet supported by the active backend; track Wave D backend support.");

    /// <inheritdoc/>
    public Task ToBeEditableAsync(AssertionsToBeEditableOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok => await _locator.IsEditableAsync(tok).ConfigureAwait(false),
            ResolveTimeout(options?.Timeout),
            Pos("to be editable"),
            ct);

    /// <inheritdoc/>
    public Task ToBeEmptyAsync(AssertionsToBeEmptyOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                // Empty = input value is null/empty AND inner text is null/empty.
                var inputVal = await _locator.InputValueAsync(tok).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(inputVal))
                    return false;

                var innerText = await _locator.InnerTextAsync(tok).ConfigureAwait(false);
                return string.IsNullOrEmpty(innerText);
            },
            ResolveTimeout(options?.Timeout),
            Pos("to be empty"),
            ct);

    /// <inheritdoc/>
    public Task ToBeAttachedAsync(AssertionsToBeAttachedOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok => (await _locator.CountAsync(tok).ConfigureAwait(false)) > 0,
            ResolveTimeout(options?.Timeout),
            Pos("to be attached"),
            ct);

    // ── Text assertions ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task ToHaveTextAsync(string expected, AssertionsToHaveTextOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                var actual = await _locator.InnerTextAsync(tok).ConfigureAwait(false);
                return CompareText(actual, expected, options?.IgnoreCase ?? false, options?.Normalized ?? false);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to have text '{expected}'"),
            ct);

    /// <inheritdoc/>
    public Task ToHaveTextAsync(Regex expected, AssertionsToHaveTextOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                var actual = await _locator.InnerTextAsync(tok).ConfigureAwait(false);
                var normalized = options?.Normalized == true ? NormalizeWhitespace(actual) : actual;
                return expected.IsMatch(normalized);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to have text matching '{expected}'"),
            ct);

    /// <inheritdoc/>
    public Task ToContainTextAsync(string expected, AssertionsToContainTextOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                var actual = await _locator.InnerTextAsync(tok).ConfigureAwait(false);
                var comparison = (options?.IgnoreCase ?? false)
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;
                var normalizedActual = options?.Normalized == true ? NormalizeWhitespace(actual) : actual;
                var normalizedExpected = options?.Normalized == true ? NormalizeWhitespace(expected) : expected;
                return normalizedActual.Contains(normalizedExpected, comparison);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to contain text '{expected}'"),
            ct);

    /// <inheritdoc/>
    public Task ToContainTextAsync(Regex expected, AssertionsToContainTextOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                var actual = await _locator.InnerTextAsync(tok).ConfigureAwait(false);
                var normalized = options?.Normalized == true ? NormalizeWhitespace(actual) : actual;
                return expected.IsMatch(normalized);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to contain text matching '{expected}'"),
            ct);

    // ── Value assertions ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task ToHaveValueAsync(string expected, AssertionsToHaveValueOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                var actual = await _locator.InputValueAsync(tok).ConfigureAwait(false);
                return string.Equals(actual, expected, StringComparison.Ordinal);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to have value '{expected}'"),
            ct);

    /// <inheritdoc/>
    public Task ToHaveValueAsync(Regex expected, AssertionsToHaveValueOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                var actual = await _locator.InputValueAsync(tok).ConfigureAwait(false);
                return actual != null && expected.IsMatch(actual);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to have value matching '{expected}'"),
            ct);

    // ── Count assertion ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task ToHaveCountAsync(int expected, AssertionsToHaveCountOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok => (await _locator.CountAsync(tok).ConfigureAwait(false)) == expected,
            ResolveTimeout(options?.Timeout),
            Pos($"to have count {expected}"),
            ct);

    // ── Attribute / identity assertions ───────────────────────────────────────

    /// <inheritdoc/>
    public Task ToHaveAttributeAsync(string name, string expected, AssertionsToHaveAttributeOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                var actual = await _locator.GetAttributeAsync(name, tok).ConfigureAwait(false);
                var comparison = (options?.IgnoreCase ?? false)
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;
                return string.Equals(actual, expected, comparison);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to have attribute '{name}' = '{expected}'"),
            ct);

    /// <inheritdoc/>
    public Task ToHaveAttributeAsync(string name, Regex expected, AssertionsToHaveAttributeOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                var actual = await _locator.GetAttributeAsync(name, tok).ConfigureAwait(false);
                return actual != null && expected.IsMatch(actual);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to have attribute '{name}' matching '{expected}'"),
            ct);

    /// <inheritdoc/>
    public Task ToHaveIdAsync(string expected, AssertionsToHaveIdOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                var actual = await _locator.GetAttributeAsync("id", tok).ConfigureAwait(false);
                return string.Equals(actual, expected, StringComparison.Ordinal);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to have id '{expected}'"),
            ct);

    /// <inheritdoc/>
    public Task ToHaveIdAsync(Regex expected, AssertionsToHaveIdOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                var actual = await _locator.GetAttributeAsync("id", tok).ConfigureAwait(false);
                return actual != null && expected.IsMatch(actual);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to have id matching '{expected}'"),
            ct);

    /// <inheritdoc/>
    public Task ToHaveClassAsync(string expected, AssertionsToHaveClassOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                var actual = await _locator.GetAttributeAsync("class", tok).ConfigureAwait(false);
                var comparison = (options?.IgnoreCase ?? false)
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;
                return string.Equals(actual, expected, comparison);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to have class '{expected}'"),
            ct);

    /// <inheritdoc/>
    public Task ToHaveClassAsync(Regex expected, AssertionsToHaveClassOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                var actual = await _locator.GetAttributeAsync("class", tok).ConfigureAwait(false);
                return actual != null && expected.IsMatch(actual);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to have class matching '{expected}'"),
            ct);

    // ── Role / accessibility assertions ───────────────────────────────────────

    /// <inheritdoc/>
    public Task ToHaveRoleAsync(AriaRole expected, AssertionsToHaveRoleOptions? options = null, CancellationToken ct = default)
    {
        // Map before entering the polling loop so that NotSupportedException (web-only roles)
        // propagates immediately rather than being swallowed and retried.
        var controlType = AriaRoleMapper.Map(expected); // throws NotSupportedException for web-only roles
        var expectedControlTypeName = controlType.ToString();

        return AssertAsync(
            async tok =>
            {
                var actual = await _locator.GetAttributeAsync("controltype", tok).ConfigureAwait(false);
                return string.Equals(actual, expectedControlTypeName, StringComparison.OrdinalIgnoreCase);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to have role '{expected}'"),
            ct);
    }

    /// <inheritdoc/>
    public Task ToHaveAccessibleNameAsync(string expected, AssertionsToHaveAccessibleNameOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                var actual = await _locator.GetAttributeAsync("name", tok).ConfigureAwait(false);
                var comparison = (options?.IgnoreCase ?? false)
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;
                return string.Equals(actual, expected, comparison);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to have accessible name '{expected}'"),
            ct);

    /// <inheritdoc/>
    public Task ToHaveAccessibleNameAsync(Regex expected, AssertionsToHaveAccessibleNameOptions? options = null, CancellationToken ct = default)
        => AssertAsync(
            async tok =>
            {
                var actual = await _locator.GetAttributeAsync("name", tok).ConfigureAwait(false);
                return actual != null && expected.IsMatch(actual);
            },
            ResolveTimeout(options?.Timeout),
            Pos($"to have accessible name matching '{expected}'"),
            ct);

    // ── Static text helpers ───────────────────────────────────────────────────

    private static bool CompareText(string actual, string expected, bool ignoreCase, bool normalized)
    {
        var a = normalized ? NormalizeWhitespace(actual) : actual;
        var e = normalized ? NormalizeWhitespace(expected) : expected;
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(a, e, comparison);
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\s+", System.Text.RegularExpressions.RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial System.Text.RegularExpressions.Regex WhitespacePattern();

    private static string NormalizeWhitespace(string s)
        => string.IsNullOrEmpty(s)
            ? s
            : WhitespacePattern().Replace(s.Trim(), " ");
}

/// <summary>
/// Negated counterpart of <see cref="FlawrightAssertions"/>.  Delegates to a
/// <see cref="FlawrightAssertions"/> instance with <c>negated = true</c>.
/// Obtain via <see cref="IFlawrightAssertions.Not"/>.
/// </summary>
/// <example>
/// <code>
/// await page.Locator("#spinner").Expect().Not.ToBeVisibleAsync();
/// </code>
/// </example>
internal sealed class FlawrightNotAssertions : IFlawrightNotAssertions
{
    private readonly FlawrightAssertions _inner;

    internal FlawrightNotAssertions(IFlawrightLocator locator, FlawrightOptions opts)
    {
        _inner = new FlawrightAssertions(locator, opts, negated: true);
    }

    /// <inheritdoc/>
    public Task ToBeVisibleAsync(AssertionsToBeVisibleOptions? options = null, CancellationToken ct = default)
        => _inner.ToBeVisibleAsync(options, ct);

    /// <inheritdoc/>
    public Task ToBeHiddenAsync(AssertionsToBeHiddenOptions? options = null, CancellationToken ct = default)
        => _inner.ToBeHiddenAsync(options, ct);

    /// <inheritdoc/>
    public Task ToBeEnabledAsync(AssertionsToBeEnabledOptions? options = null, CancellationToken ct = default)
        => _inner.ToBeEnabledAsync(options, ct);

    /// <inheritdoc/>
    public Task ToBeDisabledAsync(AssertionsToBeDisabledOptions? options = null, CancellationToken ct = default)
        => _inner.ToBeDisabledAsync(options, ct);

    /// <inheritdoc/>
    public Task ToBeCheckedAsync(AssertionsToBeCheckedOptions? options = null, CancellationToken ct = default)
        => _inner.ToBeCheckedAsync(options, ct);

    /// <inheritdoc/>
    public Task ToBeFocusedAsync(AssertionsToBeFocusedOptions? options = null, CancellationToken ct = default)
        => _inner.ToBeFocusedAsync(options, ct);

    /// <inheritdoc/>
    public Task ToBeEditableAsync(AssertionsToBeEditableOptions? options = null, CancellationToken ct = default)
        => _inner.ToBeEditableAsync(options, ct);

    /// <inheritdoc/>
    public Task ToBeEmptyAsync(AssertionsToBeEmptyOptions? options = null, CancellationToken ct = default)
        => _inner.ToBeEmptyAsync(options, ct);

    /// <inheritdoc/>
    public Task ToBeAttachedAsync(AssertionsToBeAttachedOptions? options = null, CancellationToken ct = default)
        => _inner.ToBeAttachedAsync(options, ct);

    /// <inheritdoc/>
    public Task ToHaveTextAsync(string expected, AssertionsToHaveTextOptions? options = null, CancellationToken ct = default)
        => _inner.ToHaveTextAsync(expected, options, ct);

    /// <inheritdoc/>
    public Task ToHaveTextAsync(Regex expected, AssertionsToHaveTextOptions? options = null, CancellationToken ct = default)
        => _inner.ToHaveTextAsync(expected, options, ct);

    /// <inheritdoc/>
    public Task ToContainTextAsync(string expected, AssertionsToContainTextOptions? options = null, CancellationToken ct = default)
        => _inner.ToContainTextAsync(expected, options, ct);

    /// <inheritdoc/>
    public Task ToContainTextAsync(Regex expected, AssertionsToContainTextOptions? options = null, CancellationToken ct = default)
        => _inner.ToContainTextAsync(expected, options, ct);

    /// <inheritdoc/>
    public Task ToHaveValueAsync(string expected, AssertionsToHaveValueOptions? options = null, CancellationToken ct = default)
        => _inner.ToHaveValueAsync(expected, options, ct);

    /// <inheritdoc/>
    public Task ToHaveValueAsync(Regex expected, AssertionsToHaveValueOptions? options = null, CancellationToken ct = default)
        => _inner.ToHaveValueAsync(expected, options, ct);

    /// <inheritdoc/>
    public Task ToHaveCountAsync(int expected, AssertionsToHaveCountOptions? options = null, CancellationToken ct = default)
        => _inner.ToHaveCountAsync(expected, options, ct);

    /// <inheritdoc/>
    public Task ToHaveAttributeAsync(string name, string expected, AssertionsToHaveAttributeOptions? options = null, CancellationToken ct = default)
        => _inner.ToHaveAttributeAsync(name, expected, options, ct);

    /// <inheritdoc/>
    public Task ToHaveAttributeAsync(string name, Regex expected, AssertionsToHaveAttributeOptions? options = null, CancellationToken ct = default)
        => _inner.ToHaveAttributeAsync(name, expected, options, ct);

    /// <inheritdoc/>
    public Task ToHaveIdAsync(string expected, AssertionsToHaveIdOptions? options = null, CancellationToken ct = default)
        => _inner.ToHaveIdAsync(expected, options, ct);

    /// <inheritdoc/>
    public Task ToHaveIdAsync(Regex expected, AssertionsToHaveIdOptions? options = null, CancellationToken ct = default)
        => _inner.ToHaveIdAsync(expected, options, ct);

    /// <inheritdoc/>
    public Task ToHaveClassAsync(string expected, AssertionsToHaveClassOptions? options = null, CancellationToken ct = default)
        => _inner.ToHaveClassAsync(expected, options, ct);

    /// <inheritdoc/>
    public Task ToHaveClassAsync(Regex expected, AssertionsToHaveClassOptions? options = null, CancellationToken ct = default)
        => _inner.ToHaveClassAsync(expected, options, ct);

    /// <inheritdoc/>
    public Task ToHaveRoleAsync(AriaRole expected, AssertionsToHaveRoleOptions? options = null, CancellationToken ct = default)
        => _inner.ToHaveRoleAsync(expected, options, ct);

    /// <inheritdoc/>
    public Task ToHaveAccessibleNameAsync(string expected, AssertionsToHaveAccessibleNameOptions? options = null, CancellationToken ct = default)
        => _inner.ToHaveAccessibleNameAsync(expected, options, ct);

    /// <inheritdoc/>
    public Task ToHaveAccessibleNameAsync(Regex expected, AssertionsToHaveAccessibleNameOptions? options = null, CancellationToken ct = default)
        => _inner.ToHaveAccessibleNameAsync(expected, options, ct);
}
