using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;
using JerrettDavis.Flawright.Selectors;

namespace JerrettDavis.Flawright;

/// <summary>
/// A lazy reference to one or more UI elements, resolved at action time with
/// auto-waiting.  Create instances via <see cref="IFlawrightPage.Locator"/>;
/// do not instantiate directly.
/// </summary>
/// <remarks>
/// Supported selector syntax is documented on <see cref="SelectorParser"/>.
/// </remarks>
/// <example>
/// <code>
/// var locator = page.Locator("#myButton");
/// await locator.ClickAsync();
///
/// var count = await page.Locator("role:Button").CountAsync();
///
/// var filtered = page.Locator("role:ListItem")
///     .Filter(el => el.TextAsync().GetAwaiter().GetResult().Contains("Save"));
/// </code>
/// </example>
public sealed class FlawrightLocator : IFlawrightLocator
{
    private readonly AutomationElement _root;
    private readonly UIA3Automation _automation;
    private readonly FlawrightOptions _options;
    private readonly Func<IFlawrightElement, bool>? _filter;

    internal FlawrightLocator(
        string selector,
        AutomationElement root,
        UIA3Automation automation,
        FlawrightOptions options,
        Func<IFlawrightElement, bool>? filter = null)
    {
        Selector = selector;
        _root = root;
        _automation = automation;
        _options = options;
        _filter = filter;
    }

    /// <inheritdoc/>
    public string Selector { get; }

    // ── Element resolution helpers ──────────────────────────────────────────

    private ConditionBase GetCondition()
    {
        var cf = new ConditionFactory(_automation.PropertyLibrary);
        return SelectorParser.Parse(Selector, cf);
    }

    private IFlawrightElement[] FindAll()
    {
        var condition = GetCondition();
        var raw = _root.FindAllDescendants(condition);

        IEnumerable<IFlawrightElement> elements =
            raw.Select(e => (IFlawrightElement)new FlawrightElement(e, this));

        if (_filter != null)
            elements = elements.Where(_filter);

        return elements.ToArray();
    }

    internal IFlawrightElement? FindFirst()
    {
        var all = FindAll();
        return all.Length > 0 ? all[0] : null;
    }

    private IFlawrightElement? FindNth(int index)
    {
        var all = FindAll();
        return all.Length > index ? all[index] : null;
    }

    private TimeSpan ResolveTimeout(TimeSpan? perCall) => perCall ?? _options.DefaultTimeout;

    // ── IFlawrightLocator ───────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IFlawrightElement> FirstAsync(
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        return await AutoWait.UntilAsync(
            FindFirst,
            Selector,
            ResolveTimeout(timeout),
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IFlawrightElement> NthAsync(
        int index,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        return await AutoWait.UntilAsync(
            () => FindNth(index),
            Selector,
            ResolveTimeout(timeout),
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<int> CountAsync(CancellationToken ct = default)
        => Task.Run(() => FindAll().Length, ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IFlawrightElement>> AllAsync(
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        // Auto-wait for at least one element to exist
        await AutoWait.UntilAsync(
            FindFirst,
            Selector,
            ResolveTimeout(timeout),
            _options.DefaultRetryInterval,
            ct).ConfigureAwait(false);

        return await Task.Run(() => (IReadOnlyList<IFlawrightElement>)FindAll().ToList().AsReadOnly(), ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public IFlawrightLocator Filter(Func<IFlawrightElement, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        Func<IFlawrightElement, bool> combined =
            _filter != null
                ? e => _filter(e) && predicate(e)
                : predicate;

        return new FlawrightLocator(Selector, _root, _automation, _options, combined);
    }

    /// <inheritdoc/>
    public async Task ClickAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var element = await FirstAsync(timeout, ct).ConfigureAwait(false);
        await element.ClickAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task FillAsync(string text, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var element = await FirstAsync(timeout, ct).ConfigureAwait(false);
        await element.FillAsync(text, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public IFlawrightAssertions Expect() => new FlawrightAssertions(this, _options);

    // ── Internals ───────────────────────────────────────────────────────────

    internal AutomationElement Root => _root;
    internal UIA3Automation Automation => _automation;
    internal FlawrightOptions Options => _options;
}
