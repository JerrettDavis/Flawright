#pragma warning disable CA1031 // intentional broad catch in IsVisible/IsHidden fast-path
#pragma warning disable MA0009 // Regex in GetByRole uses Regex.Escape — safe from ReDoS

using System.Text.RegularExpressions;
using Flawright.Backends;
using Flawright.Input;
using Flawright.InputModes;
using Flawright.Internals;
using Flawright.Locator;
using Flawright.Selectors;

namespace Flawright;

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

        // Name filtering: prefer NameRegex over Name when both are supplied
        // (regex is the more specific contract). This mirrors Playwright's
        // {name: /regex/} taking precedence over {name: 'text'}. Both forms
        // narrow by the element's own UIA Name property — NOT by GetElementText,
        // which falls back to Value and DocumentText for Edit/Document controls.
        if (options?.NameRegex is { } nameRegex)
        {
            var filterOptions = new LocatorFilterOptions { HasNameRegex = nameRegex };
            locator = (FlawrightLocator)locator.Filter(filterOptions);

            // Update the selector string to reflect the name regex for diagnostics.
            var combinedSelector = $"{locator.Selector} >> [name=~/{nameRegex.ToString()}/]";
            locator = new FlawrightLocator(locator._ctx with { Selector = combinedSelector });
        }
        else if (options?.Name is { } name && !string.IsNullOrEmpty(name))
        {
            LocatorFilterOptions filterOptions;
            if (options.Exact)
            {
                // Exact match: anchor the regex to start and end, matching Name directly.
                filterOptions = new LocatorFilterOptions
                {
                    HasNameRegex = new Regex(
                        "^" + Regex.Escape(name) + "$",
                        RegexOptions.IgnoreCase,
                        TimeSpan.FromSeconds(1))
                };
            }
            else
            {
                // Partial/case-insensitive match against Name directly.
                filterOptions = new LocatorFilterOptions { HasName = name };
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
        return CreateChild(BuildNameSelector(text, options?.Exact == true));
    }

    /// <inheritdoc/>
    public IFlawrightLocator GetByText(string text, LocatorGetByTextOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return CreateChild(BuildNameSelector(text, options?.Exact == true));
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
        return CreateChild(BuildNameSelector(text, options?.Exact == true));
    }

    /// <inheritdoc/>
    public IFlawrightLocator GetByTitle(string text, LocatorGetByTitleOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        // Title maps to Name in UIA.
        return CreateChild(BuildNameSelector(text, options?.Exact == true));
    }

    // ── IFlawrightLocator: Async resolution ───────────────────────────────────

    /// <inheritdoc/>
    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        // Per Playwright spec: CountAsync never auto-waits — returns 0 immediately.
        try
        {
            var results = await RunPipelineAsync().ConfigureAwait(false);
            var filtered = await ApplyFilters(results).ConfigureAwait(false);
            return filtered.Count;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IFlawrightElement>> AllAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var to = ResolveTimeout(timeout);

        // Auto-wait for at least one element.
        await AutoWait.UntilAsync(
            async _ =>
            {
                var results = await RunPipelineAsync().ConfigureAwait(false);
                var filtered = await ApplyFilters(results).ConfigureAwait(false);
                if (filtered.Count == 0) return null;
                return (IReadOnlyList<IElementBackend>?)filtered;
            },
            _ctx.Selector,
            to,
            _ctx.Options.DefaultRetryInterval,
            ct).ConfigureAwait(false);

        // Now return all (re-query to get the current snapshot).
        var all = await RunPipelineAsync().ConfigureAwait(false);
        var allFiltered = await ApplyFilters(all).ConfigureAwait(false);
        return allFiltered.Select(b => (IFlawrightElement)new FlawrightElement(b, _ctx.Input, _ctx.InputMode)).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> AllInnerTextsAsync(CancellationToken ct = default)
    {
        var backends = await RunPipelineAsync().ConfigureAwait(false);
        var filtered = await ApplyFilters(backends).ConfigureAwait(false);

        var texts = new List<string>(filtered.Count);
        foreach (var b in filtered)
        {
            var el = new FlawrightElement(b, _ctx.Input, _ctx.InputMode);
            texts.Add(await el.InnerTextAsync(ct).ConfigureAwait(false));
        }
        return texts.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> AllTextContentsAsync(CancellationToken ct = default)
    {
        var backends = await RunPipelineAsync().ConfigureAwait(false);
        var filtered = await ApplyFilters(backends).ConfigureAwait(false);

        var texts = new List<string>(filtered.Count);
        foreach (var b in filtered)
        {
            var el = new FlawrightElement(b, _ctx.Input, _ctx.InputMode);
            texts.Add(await el.TextContentAsync(ct).ConfigureAwait(false) ?? string.Empty);
        }
        return texts.AsReadOnly();
    }

    // ── IFlawrightLocator: Async actions ──────────────────────────────────────

    /// <inheritdoc/>
    public async Task ClickAsync(LocatorClickOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
        await el.ClickAsync(options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DoubleClickAsync(LocatorDoubleClickOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
        await el.DoubleClickAsync(options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task FillAsync(string text, LocatorFillOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
        await el.FillAsync(text, options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ClearAsync(LocatorClearOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
        await el.ClearAsync(options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task TypeAsync(string text, LocatorTypeOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        _ctx.InputMode.Type(backend, text, _ctx.Input);
    }

    /// <inheritdoc/>
    public Task PressSequentiallyAsync(string text, LocatorPressSequentiallyOptions? options = null, CancellationToken ct = default)
        => TypeAsync(text, options is null ? null : new LocatorTypeOptions { Delay = options.Delay, NoWaitAfter = options.NoWaitAfter, Timeout = options.Timeout }, ct);

    /// <inheritdoc/>
    public async Task PressAsync(string key, LocatorPressOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        _ctx.InputMode.Press(backend, key, _ctx.Input);
    }

    /// <inheritdoc/>
    public async Task CheckAsync(LocatorCheckOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
        await el.CheckAsync(options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UncheckAsync(LocatorUncheckOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
        await el.UncheckAsync(options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetCheckedAsync(bool @checked, LocatorSetCheckedOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
        await el.SetCheckedAsync(@checked, options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SelectOptionAsync(string value, LocatorSelectOptionOptions? options = null, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
        await el.SelectOptionAsync(value, options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SelectOptionAsync(SelectOptionValue value, LocatorSelectOptionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        var backend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);

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
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
        await el.HoverAsync(options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task FocusAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        backend.Focus();
    }

    /// <inheritdoc/>
    public async Task BlurAsync(CancellationToken ct = default)
    {
        // UIA has no direct "blur" API — focus another element or press Tab.
        // We resolve to ensure the element exists, then do nothing further.
        // Wave D can wire this to a proper blur mechanism if available.
        _ = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DragToAsync(IFlawrightLocator target, LocatorDragToOptions? options = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        var sourceBackend = await ResolveSingleAsync(options?.Timeout, ct).ConfigureAwait(false);
        var targetBackend = await ResolveLocatorSingleAsync(target, options?.Timeout, ct).ConfigureAwait(false);

        // When position overrides are specified, wrap the backends so that
        // IInputMode.DragTo sees single-point bounding rectangles at the desired
        // coordinates.  VirtualInputMode will throw before reading them anyway.
        IElementBackend effectiveSource = options?.SourcePosition is { } srcPos
            ? new PointElementBackend(sourceBackend, sourceBackend.BoundingRectangle.X + (int)srcPos.X, sourceBackend.BoundingRectangle.Y + (int)srcPos.Y)
            : sourceBackend;

        IElementBackend effectiveTarget = options?.TargetPosition is { } tgtPos
            ? new PointElementBackend(targetBackend, targetBackend.BoundingRectangle.X + (int)tgtPos.X, targetBackend.BoundingRectangle.Y + (int)tgtPos.Y)
            : targetBackend;

        _ctx.InputMode.DragTo(effectiveSource, effectiveTarget, _ctx.Input);
    }

    /// <inheritdoc/>
    public async Task ScrollIntoViewIfNeededAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
        await el.ScrollIntoViewIfNeededAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<byte[]> ScreenshotAsync(LocatorScreenshotOptions? options = null, CancellationToken ct = default)
    {
        // Wave C stub: Wave D will add real screenshot capture via IElementBackend.Capture().
        // For now, return empty bytes but still write to disk if a path is resolved.
        var bytes = Array.Empty<byte>();
        var path = FlawrightPage.ResolveScreenshotPath(
            options?.Path,
            _ctx.Options.ScreenshotDirectory,
            options?.Type ?? ScreenshotType.Png);
        if (path != null)
        {
            var directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                System.IO.Directory.CreateDirectory(directory);
            await System.IO.File.WriteAllBytesAsync(path, bytes, ct).ConfigureAwait(false);
        }
        return bytes;
    }

    /// <inheritdoc/>
    public Task<byte[]> ScreenshotAsync(string path, CancellationToken ct = default)
        => ScreenshotAsync(new LocatorScreenshotOptions { Path = path }, ct);

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
        // Instant probe — no auto-wait.  Matches Playwright semantics: IsVisible()
        // returns immediately; if the element is not in the tree, returns false.
        // Callers that need auto-waiting should use Expect().ToBeVisibleAsync() or
        // WaitForAsync(WaitForState.Visible), which retry this probe on an interval.
        var element = await TryFindFirstAsync().ConfigureAwait(false);
        if (element == null)
            return false;
        return await element.IsVisibleAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> IsHiddenAsync(CancellationToken ct = default)
    {
        // Instant probe — no auto-wait.  Matches Playwright semantics: IsHidden()
        // returns immediately; if the element is not in the tree, returns true
        // (because a missing element is treated as hidden).
        var element = await TryFindFirstAsync().ConfigureAwait(false);
        if (element == null)
            return true;
        return await element.IsHiddenAsync(ct).ConfigureAwait(false);
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

        // Try TogglePattern first (CheckBox); fall back to SelectionItemPattern (RadioButton).
        var toggle = backend.GetToggleState();
        if (toggle.HasValue)
            return toggle.Value;

        var selected = backend.GetSelectionState();
        if (selected.HasValue)
            return selected.Value;

        // Neither pattern supported — treat as unchecked.
        return false;
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
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
        return await el.InnerTextAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string?> TextContentAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
        return await el.TextContentAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string?> InputValueAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
        return await el.InputValueAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string?> SelectedTextAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        return backend.GetSelectedText();
    }

    /// <inheritdoc/>
    public async Task<string?> GetAttributeAsync(string name, CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
        return await el.GetAttributeAsync(name, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<BoundingBox?> BoundingBoxAsync(CancellationToken ct = default)
    {
        var backend = await ResolveSingleAsync(null, ct).ConfigureAwait(false);
        var el = new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
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
                async _ =>
                {
                    try
                    {
                        var backends = await RunPipelineAsync().ConfigureAwait(false);
                        var filtered = await ApplyFilters(backends).ConfigureAwait(false);
                        if (filtered.Count == 0) return false;
                        var picked = PickIndex(filtered);
                        return picked != null && !picked.IsOffscreen;
                    }
                    catch (Exception) { return false; }
                },
                $"Waiting for '{_ctx.Selector}' to be visible",
                timeout,
                _ctx.Options.DefaultRetryInterval,
                ct),

            WaitForState.Hidden => AutoWait.UntilTrueAsync(
                async _ =>
                {
                    try
                    {
                        var backends = await RunPipelineAsync().ConfigureAwait(false);
                        var filtered = await ApplyFilters(backends).ConfigureAwait(false);
                        if (filtered.Count == 0) return true;
                        var picked = PickIndex(filtered);
                        return picked == null || picked.IsOffscreen;
                    }
                    catch (Exception) { return true; }
                },
                $"Waiting for '{_ctx.Selector}' to be hidden",
                timeout,
                _ctx.Options.DefaultRetryInterval,
                ct),

            WaitForState.Attached => AutoWait.UntilTrueAsync(
                async _ =>
                {
                    try
                    {
                        var backends = await RunPipelineAsync().ConfigureAwait(false);
                        var filtered = await ApplyFilters(backends).ConfigureAwait(false);
                        return filtered.Count > 0 && PickIndex(filtered) != null;
                    }
                    catch (Exception) { return false; }
                },
                $"Waiting for '{_ctx.Selector}' to be attached",
                timeout,
                _ctx.Options.DefaultRetryInterval,
                ct),

            WaitForState.Detached => AutoWait.UntilTrueAsync(
                async _ =>
                {
                    try
                    {
                        var backends = await RunPipelineAsync().ConfigureAwait(false);
                        var filtered = await ApplyFilters(backends).ConfigureAwait(false);
                        return filtered.Count == 0 || PickIndex(filtered) == null;
                    }
                    catch (Exception) { return true; }
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
        return new FlawrightElement(backend, _ctx.Input, _ctx.InputMode);
    }

    // ── IFlawrightLocator: Assertions ─────────────────────────────────────────

    /// <inheritdoc/>
    public IFlawrightAssertions Expect() => new FlawrightAssertions(this, _ctx.Options);

    // ── Internal helpers (for FlawrightAssertions / FlawrightPage) ────────────

    /// <summary>
    /// Tries to find the first matching element without auto-wait.
    /// Returns <see langword="null"/> if nothing matches.  Used by assertions.
    /// </summary>
    internal async Task<IFlawrightElement?> TryFindFirstAsync()
    {
        try
        {
            var backends = await RunPipelineAsync().ConfigureAwait(false);
            var filtered = await ApplyFilters(backends).ConfigureAwait(false);
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
            async _ =>
            {
                try
                {
                    var backends = await RunPipelineAsync().ConfigureAwait(false);
                    var filtered = await ApplyFilters(backends).ConfigureAwait(false);
                    return PickIndex(filtered);
                }
                catch (Exception)
                {
                    return null;
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
    /// The base pipeline run is synchronous; AND/OR filter application is async
    /// (to support external <see cref="IFlawrightLocator"/> <c>Has</c>/<c>HasNot</c>
    /// filters without blocking).
    /// </summary>
    private async Task<List<IElementBackend>> RunPipelineAsync()
    {
        var results = ExecutePipeline(_ctx.Root, _ctx.Pipeline);

        // AND composition: intersect with the other locator's results.
        if (_ctx.AndWith is FlawrightLocator andLocator)
        {
            var andResults = await andLocator.RunPipelineAsync().ConfigureAwait(false);
            var andFiltered = await andLocator.ApplyFilters(andResults).ConfigureAwait(false);
            results = results.Where(r => andFiltered.Any(a => ReferenceEquals(a, r))).ToList();
        }

        // OR composition: union with the other locator's results.
        if (_ctx.OrWith is FlawrightLocator orLocator)
        {
            var orResults = await orLocator.RunPipelineAsync().ConfigureAwait(false);
            var orFiltered = await orLocator.ApplyFilters(orResults).ConfigureAwait(false);
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
    /// Synchronous pipeline execution (no AND/OR filter application).
    /// Used as the entry point before the async <see cref="ApplyFilters"/> call.
    /// </summary>
    private List<IElementBackend> RunPipeline() => ExecutePipeline(_ctx.Root, _ctx.Pipeline);

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
    /// For <c>Has</c>/<c>HasNot</c> filters involving an external <see cref="IFlawrightLocator"/>
    /// (non-<see cref="FlawrightLocator"/> implementation), the inner count is awaited
    /// properly instead of using <c>GetAwaiter().GetResult()</c>.
    /// </summary>
    private async Task<List<IElementBackend>> ApplyFilters(List<IElementBackend> candidates)
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

            // HasName filter — matches the UIA Name property directly (not value/document fallback)
            if (filter.HasName != null)
            {
                var name = filter.HasName;
                result = result.Where(b => (b.Name ?? string.Empty).Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // HasNameRegex filter — matches the UIA Name property directly (not value/document fallback)
            if (filter.HasNameRegex != null)
            {
                var regex = filter.HasNameRegex;
                result = result.Where(b => regex.IsMatch(b.Name ?? string.Empty)).ToList();
            }

            // Has(innerLocator) filter
            if (filter.Has is FlawrightLocator hasLocator)
            {
                var hasMatchResults = new List<IElementBackend>(result.Count);
                foreach (var b in result)
                {
                    // Create a scoped locator rooted at this element and count results.
                    var scopedCtx = hasLocator._ctx with { Root = b };
                    var scoped = new FlawrightLocator(scopedCtx);
                    var scopedResults = await scoped.RunPipelineAsync().ConfigureAwait(false);
                    var scopedFiltered = await scoped.ApplyFilters(scopedResults).ConfigureAwait(false);
                    if (scopedFiltered.Count > 0) hasMatchResults.Add(b);
                }
                result = hasMatchResults;
            }
            else if (filter.Has != null)
            {
                // Non-FlawrightLocator: CountAsync is fast (no auto-wait), so we
                // evaluate it up front for each candidate via a captured task result
                // rather than blocking with GetAwaiter().GetResult().
                var hasResults = new List<IElementBackend>(result.Count);
                foreach (var b in result)
                {
                    try
                    {
                        var count = await filter.Has.CountAsync().ConfigureAwait(false);
                        if (count > 0) hasResults.Add(b);
                    }
                    catch
                    {
                        // Exclude element if inner locator evaluation fails.
                    }
                }
                result = hasResults;
            }

            // HasNot(innerLocator) filter
            if (filter.HasNot is FlawrightLocator hasNotLocator)
            {
                var hasNotMatchResults = new List<IElementBackend>(result.Count);
                foreach (var b in result)
                {
                    var scopedCtx = hasNotLocator._ctx with { Root = b };
                    var scoped = new FlawrightLocator(scopedCtx);
                    var scopedResults = await scoped.RunPipelineAsync().ConfigureAwait(false);
                    var scopedFiltered = await scoped.ApplyFilters(scopedResults).ConfigureAwait(false);
                    if (scopedFiltered.Count == 0) hasNotMatchResults.Add(b);
                }
                result = hasNotMatchResults;
            }
            else if (filter.HasNot != null)
            {
                var hasNotResults = new List<IElementBackend>(result.Count);
                foreach (var b in result)
                {
                    try
                    {
                        var count = await filter.HasNot.CountAsync().ConfigureAwait(false);
                        if (count == 0) hasNotResults.Add(b);
                    }
                    catch
                    {
                        // Include element if inner locator evaluation fails.
                        hasNotResults.Add(b);
                    }
                }
                result = hasNotResults;
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

    /// <summary>
    /// Builds a <c>[name=...]</c> or <c>[name*=...]</c> selector for the given text,
    /// depending on whether an exact match is required.
    /// </summary>
    /// <param name="text">The name text to match.</param>
    /// <param name="exact">
    /// <see langword="true"/> for an exact equality match (<c>[name=...]</c>);
    /// <see langword="false"/> for a substring/contains match (<c>[name*=...]</c>).
    /// </param>
    private static string BuildNameSelector(string text, bool exact)
        => exact ? $"[name={QuoteSelector(text)}]" : $"[name*={QuoteSelector(text)}]";

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

/// <summary>
/// Forwards all <see cref="IElementBackend"/> members to an inner backend, but
/// overrides <see cref="BoundingRectangle"/> with a 1×1 rectangle at the given
/// point.  Used by <see cref="FlawrightLocator.DragToAsync"/> to translate
/// <c>LocatorDragToOptions.SourcePosition</c> / <c>TargetPosition</c> offsets
/// into a form that <see cref="IInputMode.DragTo"/>
/// can understand.
/// </summary>
file sealed class PointElementBackend : IElementBackend
{
    private readonly IElementBackend _inner;
    private readonly System.Drawing.Rectangle _rect;

    internal PointElementBackend(IElementBackend inner, int x, int y)
    {
        _inner = inner;
        _rect = new System.Drawing.Rectangle(x, y, 1, 1);
    }

    public string? AutomationId => _inner.AutomationId;
    public string? Name => _inner.Name;
    public string? ClassName => _inner.ClassName;
    public string ControlTypeName => _inner.ControlTypeName;
    public bool IsEnabled => _inner.IsEnabled;
    public bool IsOffscreen => _inner.IsOffscreen;
    public System.Drawing.Rectangle BoundingRectangle => _rect;
    public void Click() => _inner.Click();
    public void DoubleClick() => _inner.DoubleClick();
    public void Focus() => _inner.Focus();
    public bool TryInvoke() => _inner.TryInvoke();
    public bool TrySetValue(string text) => _inner.TrySetValue(text);
    public string? TryGetValue() => _inner.TryGetValue();
    public string? TryGetDocumentText() => _inner.TryGetDocumentText();
    public bool TrySelect() => _inner.TrySelect();
    public bool TryToggleOn() => _inner.TryToggleOn();
    public bool TryToggleOff() => _inner.TryToggleOff();
    public bool? GetToggleState() => _inner.GetToggleState();
    public bool? GetSelectionState() => _inner.GetSelectionState();
    public string? GetSelectedText() => _inner.GetSelectedText();
    public bool TryScrollIntoView() => _inner.TryScrollIntoView();
    public bool TryExpand() => _inner.TryExpand();
    public bool TrySelectItem(string nameOrId) => _inner.TrySelectItem(nameOrId);
    public IEnumerable<IElementBackend> FindAll(IElementCondition condition) => _inner.FindAll(condition);
    public IElementBackend? FindFirst(IElementCondition condition) => _inner.FindFirst(condition);
    public byte[] CaptureScreenshot() => _inner.CaptureScreenshot();
}
