namespace JerrettDavis.Flawright;

/// <summary>
/// Assertion helpers that implement Playwright-style
/// <c>expect(locator).toBeVisible()</c> semantics with auto-waiting.
/// Obtain via <see cref="IFlawrightLocator.Expect"/>.
/// </summary>
/// <example>
/// <code>
/// await page.Locator("#save").Expect().ToBeVisibleAsync();
/// await page.Locator("#save").Expect().Not.ToBeDisabledAsync();
/// </code>
/// </example>
public sealed class FlawrightAssertions : IFlawrightAssertions
{
    private readonly FlawrightLocator _locator;
    private readonly FlawrightOptions _options;

    internal FlawrightAssertions(FlawrightLocator locator, FlawrightOptions options)
    {
        _locator = locator;
        _options = options;
    }

    /// <inheritdoc/>
    public IFlawrightNotAssertions Not => new FlawrightNotAssertions(_locator, _options);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private TimeSpan ResolveTimeout(TimeSpan? perCall) => perCall ?? _options.DefaultTimeout;

    // ── IFlawrightAssertions ─────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task ToBeVisibleAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);
        await AutoWait.UntilTrueAsync(
            () =>
            {
                var el = TryFindFirst();
                if (el == null) return false;
                return el.IsVisibleAsync().GetAwaiter().GetResult();
            },
            $"Expected element '{_locator.Selector}' to be visible",
            to,
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ToBeHiddenAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);
        await AutoWait.UntilTrueAsync(
            () =>
            {
                var el = TryFindFirst();
                // Hidden means either absent or off-screen
                if (el == null) return true;
                return !el.IsVisibleAsync().GetAwaiter().GetResult();
            },
            $"Expected element '{_locator.Selector}' to be hidden",
            to,
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ToBeEnabledAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);
        await AutoWait.UntilTrueAsync(
            () =>
            {
                var el = TryFindFirst();
                if (el == null) return false;
                return el.IsEnabledAsync().GetAwaiter().GetResult();
            },
            $"Expected element '{_locator.Selector}' to be enabled",
            to,
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ToBeDisabledAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);
        await AutoWait.UntilTrueAsync(
            () =>
            {
                var el = TryFindFirst();
                if (el == null) return false;
                return !el.IsEnabledAsync().GetAwaiter().GetResult();
            },
            $"Expected element '{_locator.Selector}' to be disabled",
            to,
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ToHaveTextAsync(
        string expectedText,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);
        await AutoWait.UntilTrueAsync(
            () =>
            {
                var el = TryFindFirst();
                if (el == null) return false;
                var actual = el.TextAsync().GetAwaiter().GetResult();
                return string.Equals(actual, expectedText, StringComparison.Ordinal);
            },
            $"Expected element '{_locator.Selector}' to have text '{expectedText}'",
            to,
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ToHaveCountAsync(int expectedCount, CancellationToken ct = default)
    {
        var actualCount = await _locator.CountAsync(ct).ConfigureAwait(false);
        if (actualCount != expectedCount)
            throw new AssertionException(
                $"Expected {expectedCount} elements for '{_locator.Selector}' but found {actualCount}.");
    }

    /// <inheritdoc/>
    public async Task ToHaveValueAsync(
        string expected,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);
        await AutoWait.UntilTrueAsync(
            () =>
            {
                if (TryFindFirst() is not FlawrightElement el) return false;
                var vp = el.AutomationElement.Patterns.Value;
                if (!vp.IsSupported) return false;
                return string.Equals(vp.Pattern.Value.Value, expected, StringComparison.Ordinal);
            },
            $"Expected element '{_locator.Selector}' to have value '{expected}'",
            to,
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ToBeCheckedAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);
        await AutoWait.UntilTrueAsync(
            () =>
            {
                var el = TryFindFirst();
                if (el == null) return false;
                return el.IsCheckedAsync().GetAwaiter().GetResult();
            },
            $"Expected element '{_locator.Selector}' to be checked",
            to,
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private IFlawrightElement? TryFindFirst()
    {
#pragma warning disable CA1031 // Return null when element is not yet available; polling loop handles retry
        try { return _locator.FindFirst(); }
        catch (Exception) { return null; }
#pragma warning restore CA1031
    }
}

/// <summary>
/// Negated counterpart of <see cref="FlawrightAssertions"/>.  Each assertion
/// method inverts the condition.  Obtain via
/// <see cref="IFlawrightAssertions.Not"/>.
/// </summary>
/// <example>
/// <code>
/// await page.Locator("#spinner").Expect().Not.ToBeVisibleAsync();
/// </code>
/// </example>
public sealed class FlawrightNotAssertions : IFlawrightNotAssertions
{
    private readonly FlawrightLocator _locator;
    private readonly FlawrightOptions _options;

    internal FlawrightNotAssertions(FlawrightLocator locator, FlawrightOptions options)
    {
        _locator = locator;
        _options = options;
    }

    private TimeSpan ResolveTimeout(TimeSpan? perCall) => perCall ?? _options.DefaultTimeout;

    /// <inheritdoc/>
    public async Task ToBeVisibleAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);
        await AutoWait.UntilTrueAsync(
            () =>
            {
                var el = TryFindFirst();
                if (el == null) return true;
                return !el.IsVisibleAsync().GetAwaiter().GetResult();
            },
            $"Expected element '{_locator.Selector}' NOT to be visible",
            to,
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ToBeHiddenAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);
        await AutoWait.UntilTrueAsync(
            () =>
            {
                var el = TryFindFirst();
                if (el == null) return false;
                return el.IsVisibleAsync().GetAwaiter().GetResult();
            },
            $"Expected element '{_locator.Selector}' NOT to be hidden",
            to,
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ToBeEnabledAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);
        await AutoWait.UntilTrueAsync(
            () =>
            {
                var el = TryFindFirst();
                if (el == null) return false;
                return !el.IsEnabledAsync().GetAwaiter().GetResult();
            },
            $"Expected element '{_locator.Selector}' NOT to be enabled",
            to,
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ToBeDisabledAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);
        await AutoWait.UntilTrueAsync(
            () =>
            {
                var el = TryFindFirst();
                if (el == null) return true;
                return el.IsEnabledAsync().GetAwaiter().GetResult();
            },
            $"Expected element '{_locator.Selector}' NOT to be disabled",
            to,
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ToHaveTextAsync(
        string expectedText,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);
        await AutoWait.UntilTrueAsync(
            () =>
            {
                var el = TryFindFirst();
                if (el == null) return true;
                var actual = el.TextAsync().GetAwaiter().GetResult();
                return !string.Equals(actual, expectedText, StringComparison.Ordinal);
            },
            $"Expected element '{_locator.Selector}' NOT to have text '{expectedText}'",
            to,
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ToHaveValueAsync(
        string expected,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);
        await AutoWait.UntilTrueAsync(
            () =>
            {
                if (TryFindFirst() is not FlawrightElement el) return true;
                var vp = el.AutomationElement.Patterns.Value;
                if (!vp.IsSupported) return true;
                return !string.Equals(vp.Pattern.Value.Value, expected, StringComparison.Ordinal);
            },
            $"Expected element '{_locator.Selector}' NOT to have value '{expected}'",
            to,
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ToBeCheckedAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);
        await AutoWait.UntilTrueAsync(
            () =>
            {
                var el = TryFindFirst();
                if (el == null) return true;
                return !el.IsCheckedAsync().GetAwaiter().GetResult();
            },
            $"Expected element '{_locator.Selector}' NOT to be checked",
            to,
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    private IFlawrightElement? TryFindFirst()
    {
#pragma warning disable CA1031 // Return null when element is not yet available; polling loop handles retry
        try { return _locator.FindFirst(); }
        catch (Exception) { return null; }
#pragma warning restore CA1031
    }
}
