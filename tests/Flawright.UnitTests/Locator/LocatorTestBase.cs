using Flawright.Backends;
using Flawright.InputModes;
using Flawright.Locator;
using Flawright.Selectors;
using Flawright.UnitTests.Fakes;

namespace Flawright.UnitTests.Locator;

/// <summary>
/// Shared helpers and factory methods for all FlawrightLocator test classes.
/// </summary>
internal static class LocatorTestBase
{
    internal static readonly FlawrightOptions FastOptions = new()
    {
        DefaultTimeout = TimeSpan.FromMilliseconds(200),
        DefaultRetryInterval = TimeSpan.FromMilliseconds(10),
    };

    /// <summary>
    /// Creates a <see cref="FlawrightLocator"/> rooted at <paramref name="root"/>
    /// using the provided selector and fake translator.
    /// </summary>
    internal static FlawrightLocator CreateLocator(
        string selector,
        FakeElementBackend root,
        FakeInputBackend? input = null,
        FakeConditionTranslator? translator = null,
        FlawrightOptions? options = null,
        IInputMode? inputMode = null)
    {
        var t = translator ?? new FakeConditionTranslator();
        var ast = SelectorParser.Parse(selector);
        var pipeline = t.Translate(ast);
        var resolvedOptions = options ?? FastOptions;
        var ctx = new LocatorContext
        {
            Root = root,
            Input = input ?? new FakeInputBackend(),
            InputMode = inputMode ?? resolvedOptions.InputMode,
            Translator = t,
            Selector = selector,
            Pipeline = pipeline,
            Options = resolvedOptions,
        };
        return new FlawrightLocator(ctx);
    }

}
