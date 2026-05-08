#pragma warning disable CA1031 // intentional broad catch in IsVisible/IsHidden fast-path
#pragma warning disable MA0009 // Regex in GetByRole uses Regex.Escape — safe from ReDoS

using System.Text.RegularExpressions;
using JerrettDavis.Flawright.Backends;
using JerrettDavis.Flawright.Input;
using JerrettDavis.Flawright.Internals;
using JerrettDavis.Flawright.Locator;
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
internal sealed class FlawrightLocator : IFlawrightLocator
{
    private readonly LocatorContext _ctx;

    // ── Construction ──────────────────────────────────────────────────────────

    internal FlawrightLocator(LocatorContext ctx)
    {
        _ctx = ctx;
    }

    // ── IFlawrightLocator: Identity ───────────────────────────────────────────

    /// <inheritdoc/>
    public string Selector => _ctx.Selector;

    // ── IFlawrightLocator: Sync chaining ──────────────────────────────────────

    /// <inheritdoc/>
    public IFlawrightLocator First =>
        new FlawrightLocator(_ctx with { IndexKind = LocatorIndex.First, NthIndex = 0 });

    /// <inheritdoc/>
    public IFlawrightLocator Last =>
        new FlawrightLocator(_ctx with { IndexKind = LocatorIndex.Last, NthIndex = 0 });

    /// <inheritdoc/>
    public IFlawrightLocator Nth(int index) =>
        new FlawrightLocator(_ctx with { IndexKind = LocatorIndex.Nth, NthIndex = index });

    // ── IFlawrightLocator: Scoped chaining ────────────────────────────────────

    /// <inheritdoc/>
    public IFlawrightLocator Locator(string selector)
    {
        ArgumentException.ThrowIfNullOrEmpty(selector);
        var ast = SelectorParser.Parse(selector);
        var innerPipeline = _ctx.Translator.Translate(ast);

        // Combine: first resolve _ctx.Pipeline, then for each result, run innerPipeline.
        // We represent this by wrapping in a new context with the inner pipeline and
        // restricting the root search to the parent locator's results.
        // The combined selector string reflects both levels.
        var combinedSelector = $"{_ctx.Selector} >> {selector}";

        // Build a composite pipeline: the outer steps + inner steps.
        var combinedSteps = _ctx.Pipeline.Steps.Concat(innerPipeline.Steps).ToList().AsReadOnly();
        var combinedPipeline = new SelectorPipeline(combinedSteps);

        return new FlawrightLocator(_ctx with
        {
            Selector = combinedSelector,
            Pipeline = combinedPipeline,
            // Reset index/filters that were scoped to the parent — the inner locator is fresh.
            IndexKind = LocatorIndex.Any,
            NthIndex = 0,
            Filters = [],
            AndWith = null,
            OrWith = null,
        });
    }

    /// <inheritdoc/>
    public IFlawrightLocator Locator(IFlawrightLocator inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        // Delegate to the string overload using the inner locator's selector.
        return Locator(inner.Selector);
    }

    // ── IFlawrightLocator: Filtering ──────────────────────────────────────────

    /// <inheritdoc/>
    public IFlawrightLocator Filter(LocatorFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var newFilters = _ctx.Filters.Append(options).ToList().AsReadOnly();
        return new FlawrightLocator(_ctx with { Filters = newFilters });
    }

    // ── IFlawrightLocator: Composition ────────────────────────────────────────

    /// <inheritdoc/>
    public IFlawrightLocator And(IFlawrightLocator other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new FlawrightLocator(_ctx with { AndWith = other });
    }

    /// <inheritdoc/>
    public IFlawrightLocator Or(IFlawrightLocator other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new FlawrightLocator(_ctx with { OrWith = other });
    }

    // ── IFlawrightLocator: Query helpers ─────────────────────────────────────

    /// <inheritdoc/>
    public IFlawrightLocator GetByRole(AriaRole role, LocatorGetByRoleOptions? options = null)
    {
        var controlTypeName = AriaRoleMapper.Map(role); // throws NotSupportedException for web-only roles
        // Build a selector for the ControlType only (name filtering applied as a Filter below)
        var roleSelector = BuildRoleSelector(controlTypeName.ToString());
        var locator = CreateChild(roleSelector);

        // If a Name filter is requested, apply it as a LocatorFilterOptions so that
        // the filter tests the element's own Name property rather than searching descendants.
        // This mirrors Playwright's {name:} accessible-name matching.
        if (options?.Name is { } name && !string.IsNullOrEmpty(name))
        {
            LocatorFilterOptions filterOptions;
            if (options.Exact)
            {
                // Exact match: use a regex anchored to start and end.
                filterOptions = new LocatorFilterOptions
                {
                    HasTextRegex = new Regex(
                        "^" + Regex.Escape(name) + "$",
                        RegexOptions.IgnoreCase,
                        TimeSpan.FromSeconds(1))
                };
            }
            else
            {
                // Partial/case-insensitive match via HasText.
                filterOptions = new LocatorFilterOptions { HasText = name };
            }

            locator = (FlawrightLocator)locator.Filter(filterOptions);

            // Update the selector string to reflect the name for diagnostics.
            var quotedName = QuoteSelector(name);
            var namePart = options.Exact
                ? $"[name={quotedName}]"
                : $"[name*={quotedName}]";
            var combinedSelector = $"{locator.Selector} >> {namePart}";
            locator = new FlawrightLocator(locator._ctx with { Selector = combinedSelector });
        }

        return locator;
    }

    /// <inheritdoc/>
    public IFlawrightLocator GetByLabel(string text, LocatorGetByLabelOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        // Labels in UIA are expressed as the Name property of a nearby label control
        // or the Name of the target element itself. We match by Name (exact or contains).
        var selector = (options?.Exact == true)
            ? $"[name={QuoteSelector(text)}]"
            : $"[name*={QuoteSelector(text)}]";
        return CreateChild(selector);
    }

    /// <inheritdoc/>
    public IFlawrightLocator GetByText(string text, LocatorGetByTextOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var selector = (options?.Exact == true)
            ? $"[name={QuoteSelector(text)}]"
            : $"[name*={QuoteSelector(text)}]";
        return CreateChild(selector);
    }

    /// <inheritdoc/>
    public IFlawrightLocator GetByTestId(string testId)
    {
        ArgumentNullException.ThrowIfNull(testId);
        return CreateChild($"#{testId}");
    }

    /// <inheritdoc/>
    public IFlawrightLocator GetByPlaceholder(string text, LocatorGetByPlaceholderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        // Placeholder text in desktop UIA typically appears as the Name when no value is set.
        var selector = (options?.Exact == true)
            ? $"[name={QuoteSelector(text)}]"
            : $"[name*={QuoteSelector(text)}]";
        return CreateChild(selector);
    }

    /// <inheritdoc/>
    public IFlawrightLocator GetByTitle(string text, LocatorGetByTitleOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        // Title maps to Name in UIA.
        var selector = (options?.Exact == true)
            ? $"[name={QuoteSelector(text)}]"
            : $"[name*={QuoteSelector(text)}]";
        return CreateChild(selector);
    }

    // ── IFlawrightLocator: Async resolution ───────────────────────────────────

    /// <inheritdoc/>
    public Task<int> CountAsync(CancellationToken ct = default)
    {
        // Per Playwright spec: CountAsync never auto-waits — returns 0 immediately.
        try
        {
            var results = RunPipeline();
            var filtered = ApplyFilters(results);
            return Task.FromResult(filtered.Count);
        }
        catch (Exception)
        {
            return Task.FromResult(0);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IFlawrightElement>> AllAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);

        // Auto-wait for at least one element.
        await AutoWait.UntilAsync(
            _ =>
            {
                var results = RunPipeline();
                var filtered = ApplyFilters(results);
                if (filtered.Count == 0) return Task.FromResult<IReadOnlyList<IElementBackend>?>(null);
                return Task.FromResult<IReadOnlyList<IElementBackend>?>(filtered);
            },
            _ctx.Selector,
            to,
            _ctx.Options.DefaultRetryInterval,
            ct).ConfigureAwait(false);

        // Now return all (re-query to get the current snapshot).
        var all = RunPipeline();
        var allFiltered = ApplyFilters(all);
        return allFiltered.Select(b => (IFlawrightElement)new FlawrightElement(b, _ctx.Input)).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> AllInnerTextsAsync(CancellationToken ct = default)
    {
        var backends = RunPipeline();
        var filtered = ApplyFilters(backends);

        var texts = new List<string>(filtered.Count);
        foreach (var b in filtered)
        {
            var el = new FlawrightElement(b, _ctx.Input);
            texts.Add(await el.InnerTextAsync(ct).ConfigureAwait(false));
        }
        return texts.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> AllTextContentsAsync(CancellationToken ct = default)
    {
        var backends = RunPipeline();
        var filtered = ApplyFilters(backends);

        var texts = new List<string>(filtered.Count);
        foreach (var b in filtered)
        {
            var el = new FlawrightElement(b, _ctx.Input);
            texts.Add(await el.TextContentAsync(ct).ConfigureAwait(false) ?? string.Empty);
        }
        return texts.AsReadOnly();
    }

    // ── IFlawrightLocator: Async actions ──────────────────────────────────────

    /// <inheritdoc/>
    public async Task ClickAsync(LocatorClickOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        await el.ClickAsync(options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DoubleClickAsync(LocatorDoubleClickOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        await el.DoubleClickAsync(options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task FillAsync(string text, LocatorFillOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        await el.FillAsync(text, options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ClearAsync(LocatorClearOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        await el.ClearAsync(options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task TypeAsync(string text, LocatorTypeOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        backend.Focus();
        _ctx.Input.KeyboardType(text);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task PressSequentiallyAsync(string text, LocatorPressSequentiallyOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        backend.Focus();
        _ctx.Input.KeyboardType(text);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task PressAsync(string key, LocatorPressOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        backend.Focus();
        // Parse the key and dispatch via the input backend.
        var vk = KeyParser.ParseKey(key.Split('+')[^1].Trim());
        _ctx.Input.KeyboardTap(vk);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CheckAsync(LocatorCheckOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        await el.CheckAsync(options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UncheckAsync(LocatorUncheckOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        await el.UncheckAsync(options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetCheckedAsync(bool @checked, LocatorSetCheckedOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        await el.SetCheckedAsync(@checked, options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SelectOptionAsync(string value, LocatorSelectOptionOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        await el.SelectOptionAsync(value, options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SelectOptionAsync(SelectOptionValue value, LocatorSelectOptionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);

        // Resolve the SelectOptionValue to a string identifier.
        var identifier = value.Label ?? value.Value;
        if (identifier != null)
        {
            await el.SelectOptionAsync(identifier, options, ct).ConfigureAwait(false);
        }
        else if (value.Index.HasValue)
        {
            // For index-based selection, find the Nth child item.
            var children = backend.FindAll(new IndexBasedCondition()).ToList();
            if (value.Index.Value < 0 || value.Index.Value >= children.Count)
                throw new InvalidOperationException(
                    $"Index {value.Index.Value} is out of range. The container has {children.Count} items.");

            var target = children[value.Index.Value];
            if (!target.TrySelectItem(target.Name ?? target.AutomationId ?? string.Empty))
                throw new InvalidOperationException(
                    $"Could not select item at index {value.Index.Value}.");
        }
        else
        {
            throw new ArgumentException(
                "SelectOptionValue must have at least one of Label, Value, or Index set.", nameof(value));
        }
    }

    /// <inheritdoc/>
    public async Task HoverAsync(LocatorHoverOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        await el.HoverAsync(options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task FocusAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        backend.Focus();
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task BlurAsync(CancellationToken ct = default)
    {
        // UIA has no direct "blur" API — focus another element or press Tab.
        // We resolve to ensure the element exists, then do nothing further.
        // Wave D can wire this to a proper blur mechanism if available.
        _ = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DragToAsync(IFlawrightLocator target, LocatorDragToOptions? options = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        var sourceBackend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var targetBackend = await ResolveLocatorSingleAsync(target, options?.Timeout, ct).ConfigureAwait(false);

        var sourceRect = sourceBackend.BoundingRectangle;
        var targetRect = targetBackend.BoundingRectangle;

        int srcX, srcY;
        if (options?.SourcePosition is { } srcPos)
        {
            srcX = sourceRect.X + (int)srcPos.X;
            srcY = sourceRect.Y + (int)srcPos.Y;
        }
        else
        {
            srcX = sourceRect.X + sourceRect.Width / 2;
            srcY = sourceRect.Y + sourceRect.Height / 2;
        }

        int tgtX, tgtY;
        if (options?.TargetPosition is { } tgtPos)
        {
            tgtX = targetRect.X + (int)tgtPos.X;
            tgtY = targetRect.Y + (int)tgtPos.Y;
        }
        else
        {
            tgtX = targetRect.X + targetRect.Width / 2;
            tgtY = targetRect.Y + targetRect.Height / 2;
        }

        _ctx.Input.MouseMove(srcX, srcY, steps: 0);
        _ctx.Input.MouseDown(MouseButton.Left);
        _ctx.Input.MouseMove(tgtX, tgtY, steps: 10);
        _ctx.Input.MouseUp(MouseButton.Left);

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ScrollIntoViewIfNeededAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        await el.ScrollIntoViewIfNeededAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<byte[]> ScreenshotAsync(LocatorScreenshotOptions? options = null, CancellationToken ct = default)
    {
        // Wave C stub: Wave D will add IElementBackend.Capture().
        // Return empty byte array; if a Path is specified, create an empty file as a placeholder.
        if (options?.Path != null)
        {
            System.IO.File.WriteAllBytes(options.Path, []);
        }
        return Task.FromResult(Array.Empty<byte>());
    }

    /// <inheritdoc/>
    public Task HighlightAsync(CancellationToken ct = default)
    {
        // Wave C stub: Wave D will implement visual element highlighting.
        return Task.CompletedTask;
    }

    // ── IFlawrightLocator: Read methods ───────────────────────────────────────

    /// <inheritdoc/>
    public async Task<bool> IsVisibleAsync(CancellationToken ct = default)
    {
        // Short timeout (1s): return false rather than throw if not found.
        var shortTimeout = TimeSpan.FromSeconds(1);
        try
        {
            var backend = await ResolveSingleAsync(shortTimeout, ct).ConfigureAwait(false);
            return !backend.IsOffscreen;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsHiddenAsync(CancellationToken ct = default)
    {
        // Short timeout (1s): return true (hidden) if not found.
        var shortTimeout = TimeSpan.FromSeconds(1);
        try
        {
            var backend = await ResolveSingleAsync(shortTimeout, ct).ConfigureAwait(false);
            return backend.IsOffscreen;
        }
        catch (Exception)
        {
            return true;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsEnabledAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        return backend.IsEnabled;
    }

    /// <inheritdoc/>
    public async Task<bool> IsDisabledAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        return !backend.IsEnabled;
    }

    /// <inheritdoc/>
    public async Task<bool> IsCheckedAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        return backend.GetToggleState() == true;
    }

    /// <inheritdoc/>
    public async Task<bool> IsEditableAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        return backend.TryGetValue() != null && backend.IsEnabled;
    }

    /// <inheritdoc/>
    public async Task<string> InnerTextAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        return await el.InnerTextAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string?> TextContentAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        return await el.TextContentAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string?> InputValueAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        return await el.InputValueAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string?> GetAttributeAsync(string name, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        return await el.GetAttributeAsync(name, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<BoundingBox?> BoundingBoxAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input);
        return await el.BoundingBoxAsync(ct).ConfigureAwait(false);
    }

    // ── IFlawrightLocator: Wait for state ─────────────────────────────────────

    /// <inheritdoc/>
    public Task WaitForAsync(LocatorWaitForOptions? options = null, CancellationToken ct = default)
    {
        var state = options?.State ?? WaitForState.Visible;
        var timeout = ResolveTimeout(options?.Timeout);

        return state switch
        {
            WaitForState.Visible => AutoWait.UntilTrueAsync(
                _ =>
                {
                    try
                    {
                        var backends = RunPipeline();
                        var filtered = ApplyFilters(backends);
                        if (filtered.Count == 0) return Task.FromResult(false);
                        var picked = PickIndex(filtered);
                        return Task.FromResult(picked != null && !picked.IsOffscreen);
                    }
                    catch (Exception) { return Task.FromResult(false); }
                },
                $"Waiting for '{_ctx.Selector}' to be visible",
                timeout,
                _ctx.Options.DefaultRetryInterval,
                ct),

            WaitForState.Hidden => AutoWait.UntilTrueAsync(
                _ =>
                {
                    try
                    {
                        var backends = RunPipeline();
                        var filtered = ApplyFilters(backends);
                        if (filtered.Count == 0) return Task.FromResult(true);
                        var picked = PickIndex(filtered);
                        return Task.FromResult(picked == null || picked.IsOffscreen);
                    }
                    catch (Exception) { return Task.FromResult(true); }
                },
                $"Waiting for '{_ctx.Selector}' to be hidden",
                timeout,
                _ctx.Options.DefaultRetryInterval,
                ct),

            WaitForState.Attached => AutoWait.UntilTrueAsync(
                _ =>
                {
                    try
                    {
                        var backends = RunPipeline();
                        var filtered = ApplyFilters(backends);
                        return Task.FromResult(filtered.Count > 0 && PickIndex(filtered) != null);
                    }
                    catch (Exception) { return Task.FromResult(false); }
                },
                $"Waiting for '{_ctx.Selector}' to be attached",
                timeout,
                _ctx.Options.DefaultRetryInterval,
                ct),

            WaitForState.Detached => AutoWait.UntilTrueAsync(
                _ =>
                {
                    try
                    {
                        var backends = RunPipeline();
                        var filtered = ApplyFilters(backends);
                        return Task.FromResult(filtered.Count == 0 || PickIndex(filtered) == null);
                    }
                    catch (Exception) { return Task.FromResult(true); }
                },
                $"Waiting for '{_ctx.Selector}' to be detached",
                timeout,
                _ctx.Options.DefaultRetryInterval,
                ct),

            _ => throw new ArgumentOutOfRangeException(nameof(options), $"Unknown WaitForState: {state}")
        };
    }

    // ── IFlawrightLocator: Element handle ─────────────────────────────────────

    /// <inheritdoc/>
    [Obsolete("Prefer locator-based actions; ElementHandle exists for advanced introspection only.")]
    public async Task<IFlawrightElement> ElementHandleAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(timeout, ct).ConfigureAwait(false);
        return new FlawrightElement(backend, _ctx.Input);
    }

    // ── IFlawrightLocator: Assertions ─────────────────────────────────────────

    /// <inheritdoc/>
    public IFlawrightAssertions Expect() => new FlawrightAssertions(this, _ctx.Options);

    // ── Internal helpers (for FlawrightAssertions / FlawrightPage) ────────────

    /// <summary>
    /// Tries to find the first matching element synchronously without auto-wait.
    /// Returns <see langword="null"/> if nothing matches.  Used by assertions.
    /// </summary>
    internal IFlawrightElement? TryFindFirst()
    {
        try
        {
            var backends = RunPipeline();
            var filtered = ApplyFilters(backends);
            var picked = PickIndex(filtered);
            return picked != null ? new FlawrightElement(picked, _ctx.Input) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // ── Resolution algorithm ──────────────────────────────────────────────────

    /// <summary>
    /// Auto-waits until at least one element matches the full context (pipeline +
    /// filters + index + composition), then returns it.
    /// </summary>
    private async Task<IElementBackend> ResolveSingleAsync(TimeSpan? timeout, CancellationToken ct)
    {
        var to = ResolveTimeout(timeout);

        return await AutoWait.UntilAsync<IElementBackend>(
            _ =>
            {
                try
                {
                    var backends = RunPipeline();
                    var filtered = ApplyFilters(backends);
                    var picked = PickIndex(filtered);
                    return Task.FromResult<IElementBackend?>(picked);
                }
                catch (Exception)
                {
                    return Task.FromResult<IElementBackend?>(null);
                }
            },
            _ctx.Selector,
            to,
            _ctx.Options.DefaultRetryInterval,
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves another <see cref="IFlawrightLocator"/> to a single backend element,
    /// used for drag-to targets.
    /// </summary>
    private async Task<IElementBackend> ResolveLocatorSingleAsync(
        IFlawrightLocator locator, TimeSpan? timeout, CancellationToken ct)
    {
        if (locator is FlawrightLocator fl)
            return await fl.ResolveSingleAsync(timeout, ct).ConfigureAwait(false);

        // Fallback for non-internal implementations: use ElementHandleAsync.
#pragma warning disable CS0618 // Type or member is obsolete
        var element = await locator.ElementHandleAsync(timeout, ct).ConfigureAwait(false);
#pragma warning restore CS0618
        if (element is FlawrightElement fe)
        {
            // Access backend via reflection-free approach — FlawrightElement exposes it internally
            return GetBackendFromElement(fe);
        }

        throw new InvalidOperationException(
            "DragToAsync target locator must produce a FlawrightElement backed by IElementBackend.");
    }

    /// <summary>
    /// Executes the selector pipeline against <see cref="LocatorContext.Root"/>,
    /// applying AND / OR composition if configured.
    /// </summary>
    private List<IElementBackend> RunPipeline()
    {
        var results = ExecutePipeline(_ctx.Root, _ctx.Pipeline);

        // AND composition: intersect with the other locator's results.
        if (_ctx.AndWith is FlawrightLocator andLocator)
        {
            var andResults = andLocator.RunPipeline();
            var andFiltered = andLocator.ApplyFilters(andResults);
            results = results.Where(r => andFiltered.Any(a => ReferenceEquals(a, r))).ToList();
        }

        // OR composition: union with the other locator's results.
        if (_ctx.OrWith is FlawrightLocator orLocator)
        {
            var orResults = orLocator.RunPipeline();
            var orFiltered = orLocator.ApplyFilters(orResults);
            // Union: add items from orFiltered not already in results.
            foreach (var item in orFiltered)
            {
                if (!results.Any(r => ReferenceEquals(r, item)))
                    results.Add(item);
            }
        }

        return results;
    }

    /// <summary>
    /// Executes a pipeline by iterating steps: at each step, search descendants
    /// of each previous-step match for elements matching the current step's condition.
    /// </summary>
    private static List<IElementBackend> ExecutePipeline(IElementBackend root, SelectorPipeline pipeline)
    {
        var current = new List<IElementBackend> { root };

        foreach (var step in pipeline.Steps)
        {
            var next = new List<IElementBackend>();
            foreach (var parent in current)
            {
                next.AddRange(step.FindAllFrom(parent));
            }
            current = next;
        }

        return current;
    }

    /// <summary>
    /// Applies all accumulated <see cref="LocatorFilterOptions"/> to a candidate set.
    /// </summary>
    private List<IElementBackend> ApplyFilters(List<IElementBackend> candidates)
    {
        var result = new List<IElementBackend>(candidates);

        foreach (var filter in _ctx.Filters)
        {
            // Visible filter
            if (filter.Visible.HasValue)
            {
                result = filter.Visible.Value
                    ? result.Where(b => !b.IsOffscreen).ToList()
                    : result.Where(b => b.IsOffscreen).ToList();
            }

            // HasText filter (substring, case-insensitive)
            if (filter.HasText != null)
            {
                var text = filter.HasText;
                result = result.Where(b => GetElementText(b).Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // HasTextRegex filter
            if (filter.HasTextRegex != null)
            {
                var regex = filter.HasTextRegex;
                result = result.Where(b => regex.IsMatch(GetElementText(b))).ToList();
            }

            // HasNotText filter
            if (filter.HasNotText != null)
            {
                var text = filter.HasNotText;
                result = result.Where(b => !GetElementText(b).Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // HasNotTextRegex filter
            if (filter.HasNotTextRegex != null)
            {
                var regex = filter.HasNotTextRegex;
                result = result.Where(b => !regex.IsMatch(GetElementText(b))).ToList();
            }

            // Has(innerLocator) filter
            if (filter.Has is FlawrightLocator hasLocator)
            {
                result = result.Where(b =>
                {
                    // Create a scoped locator rooted at this element and count results.
                    var scopedCtx = hasLocator._ctx with { Root = b };
                    var scoped = new FlawrightLocator(scopedCtx);
                    var scopedResults = scoped.RunPipeline();
                    var scopedFiltered = scoped.ApplyFilters(scopedResults);
                    return scopedFiltered.Count > 0;
                }).ToList();
            }
            else if (filter.Has != null)
            {
                // Non-FlawrightLocator: use CountAsync (sync wrapper)
                result = result.Where(b =>
                {
                    try { return filter.Has.CountAsync().GetAwaiter().GetResult() > 0; }
                    catch { return false; }
                }).ToList();
            }

            // HasNot(innerLocator) filter
            if (filter.HasNot is FlawrightLocator hasNotLocator)
            {
                result = result.Where(b =>
                {
                    var scopedCtx = hasNotLocator._ctx with { Root = b };
                    var scoped = new FlawrightLocator(scopedCtx);
                    var scopedResults = scoped.RunPipeline();
                    var scopedFiltered = scoped.ApplyFilters(scopedResults);
                    return scopedFiltered.Count == 0;
                }).ToList();
            }
            else if (filter.HasNot != null)
            {
                result = result.Where(b =>
                {
                    try { return filter.HasNot.CountAsync().GetAwaiter().GetResult() == 0; }
                    catch { return true; }
                }).ToList();
            }
        }

        return result;
    }

    /// <summary>
    /// Picks the target element from the filtered result set based on
    /// <see cref="LocatorContext.IndexKind"/>.
    /// Returns <see langword="null"/> if the set is empty or the index is out of range.
    /// </summary>
    private IElementBackend? PickIndex(List<IElementBackend> filtered)
    {
        return _ctx.IndexKind switch
        {
            LocatorIndex.Any => filtered.Count > 0 ? filtered[0] : null,
            LocatorIndex.First => filtered.Count > 0 ? filtered[0] : null,
            LocatorIndex.Last => filtered.Count > 0 ? filtered[^1] : null,
            LocatorIndex.Nth => _ctx.NthIndex >= 0 && _ctx.NthIndex < filtered.Count
                ? filtered[_ctx.NthIndex]
                : null,
            _ => null,
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private TimeSpan ResolveTimeout(TimeSpan? perCall) => perCall ?? _ctx.Options.DefaultTimeout;

    private FlawrightLocator CreateChild(string selector)
    {
        // Parse the new selector and append to this locator's pipeline.
        var ast = SelectorParser.Parse(selector);
        var innerPipeline = _ctx.Translator.Translate(ast);
        var combinedSelector = $"{_ctx.Selector} >> {selector}";
        var combinedSteps = _ctx.Pipeline.Steps.Concat(innerPipeline.Steps).ToList().AsReadOnly();
        var combinedPipeline = new SelectorPipeline(combinedSteps);

        return new FlawrightLocator(_ctx with
        {
            Selector = combinedSelector,
            Pipeline = combinedPipeline,
            IndexKind = LocatorIndex.Any,
            NthIndex = 0,
            Filters = [],
            AndWith = null,
            OrWith = null,
        });
    }

    private static string GetElementText(IElementBackend backend)
    {
        return backend.TryGetValue()
            ?? backend.TryGetDocumentText()
            ?? backend.Name
            ?? string.Empty;
    }

    private static string QuoteSelector(string value)
    {
        // Use double-quoted attribute value in selector syntax.
        var escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }

    private static string BuildRoleSelector(string controlTypeName)
    {
        // Build selector for the ControlType only. Name filtering is handled
        // separately via Filter() to avoid incorrect descendant-search semantics.
        var sb = new System.Text.StringBuilder();
        sb.Append('[').Append("role=").Append(controlTypeName).Append(']');
        return sb.ToString();
    }

    private static IElementBackend GetBackendFromElement(FlawrightElement element)
    {
        // FlawrightElement exposes its backend via InternalBackend (see below).
        return element.InternalBackend;
    }
}

/// <summary>
/// A trivial <see cref="IElementCondition"/> that matches all descendants.
/// Used for index-based SelectOptionAsync when iterating children.
/// </summary>
file sealed class IndexBasedCondition : IElementCondition
{
    public IEnumerable<IElementBackend> FindAllFrom(IElementBackend root)
        => root.FindAll(this);
}
