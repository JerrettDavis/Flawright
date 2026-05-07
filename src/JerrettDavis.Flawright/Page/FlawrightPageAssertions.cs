using System.Text.RegularExpressions;
using JerrettDavis.Flawright.Assertions;
using JerrettDavis.Flawright.Internals;

namespace JerrettDavis.Flawright.Page;

/// <summary>
/// Implements <see cref="IFlawrightPageAssertions"/> with auto-waiting semantics.
/// Obtain via <see cref="AssertionsStatic.Expect(IFlawrightPage)"/>.
/// </summary>
internal sealed class FlawrightPageAssertions : IFlawrightPageAssertions
{
    private readonly IFlawrightPage _page;
    private readonly FlawrightOptions _opts;
    private readonly bool _negated;

    internal FlawrightPageAssertions(IFlawrightPage page, FlawrightOptions opts, bool negated = false)
    {
        _page = page;
        _opts = opts;
        _negated = negated;
    }

    /// <inheritdoc/>
    public IFlawrightPageAssertions Not => new FlawrightPageAssertions(_page, _opts, !_negated);

    /// <inheritdoc/>
    public async Task ToHaveTitleAsync(
        string expected,
        PageAssertionsToHaveTitleOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(expected);

        var timeout = options?.Timeout ?? _opts.DefaultTimeout;
        var comparison = (options?.IgnoreCase == true)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var description = _negated
            ? $"Expected page title NOT to be '{expected}'"
            : $"Expected page title to be '{expected}'";

        await AutoWait.UntilTrueAsync(
            async _ =>
            {
                var title = await _page.TitleAsync(ct).ConfigureAwait(false);
                var matches = string.Equals(title, expected, comparison);
                return _negated ? !matches : matches;
            },
            description,
            timeout,
            _opts.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ToHaveTitleAsync(
        Regex expected,
        PageAssertionsToHaveTitleOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(expected);

        var timeout = options?.Timeout ?? _opts.DefaultTimeout;

        var description = _negated
            ? $"Expected page title NOT to match /{expected}/"
            : $"Expected page title to match /{expected}/";

        await AutoWait.UntilTrueAsync(
            async _ =>
            {
                var title = await _page.TitleAsync(ct).ConfigureAwait(false);
                var matches = expected.IsMatch(title);
                return _negated ? !matches : matches;
            },
            description,
            timeout,
            _opts.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }
}
