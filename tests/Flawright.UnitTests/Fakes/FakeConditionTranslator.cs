#pragma warning disable MA0015 // private translator helpers don't have named parameters to reference
#pragma warning disable CA1859  // keeping return type as IElementCondition for interface contract clarity

using Flawright.Backends;
using Flawright.Selectors;

namespace Flawright.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="IConditionTranslator"/> for unit tests.
///
/// <para>
/// Each <see cref="SelectorAst"/> node is translated into a
/// <see cref="FakeElementCondition"/> that applies a simple predicate against the
/// fake UIA tree.  This lets tests assert what the translator emitted without any
/// FlaUI dependency.
/// </para>
///
/// <para>
/// The translator also records translated AST nodes in
/// <see cref="TranslatedNodes"/> so tests can verify which AST types were
/// produced and in what order.
/// </para>
/// </summary>
internal sealed class FakeConditionTranslator : IConditionTranslator
{
    private readonly List<SelectorAst> _translatedNodes = [];

    /// <summary>
    /// All AST nodes translated so far, in order.  Chains are flattened to their
    /// individual steps.
    /// </summary>
    public IReadOnlyList<SelectorAst> TranslatedNodes => _translatedNodes.AsReadOnly();

    /// <summary>Resets the translation history.</summary>
    public void Reset() => _translatedNodes.Clear();

    /// <inheritdoc/>
    public SelectorPipeline Translate(SelectorAst ast)
    {
        if (ast is SelectorAst.Chain chain)
        {
            var steps = chain.Steps.Select(TranslateStep).ToList().AsReadOnly();
            return new SelectorPipeline(steps);
        }

        return SelectorPipeline.Single(TranslateStep(ast));
    }

    private IElementCondition TranslateStep(SelectorAst ast)
    {
        _translatedNodes.Add(ast);
        return ast switch
        {
            SelectorAst.AutomationId a => new FakeElementCondition(
                e => string.Equals(e.AutomationId, a.Value, StringComparison.Ordinal)),

            SelectorAst.BareName b => new FakeElementCondition(
                e => string.Equals(e.Name, b.Value, StringComparison.Ordinal)),

            SelectorAst.Prefix p => TranslatePrefix(p),

            SelectorAst.Attribute a => TranslateAttribute(a),

            SelectorAst.Chain => throw new ArgumentException(
                "Nested Chain nodes are not valid."),

            _ => throw new ArgumentException($"Unknown SelectorAst type: {ast.GetType().Name}"),
        };
    }

    private static IElementCondition TranslatePrefix(SelectorAst.Prefix prefix)
    {
        return prefix.Kind switch
        {
            PrefixKind.Name or PrefixKind.Text => new FakeElementCondition(
                e => string.Equals(e.Name, prefix.Value, StringComparison.Ordinal)),

            PrefixKind.AutomationId => new FakeElementCondition(
                e => string.Equals(e.AutomationId, prefix.Value, StringComparison.Ordinal)),

            PrefixKind.ClassName => new FakeElementCondition(
                e => string.Equals(e.ClassName, prefix.Value, StringComparison.Ordinal)),

            PrefixKind.ControlType => new FakeElementCondition(
                e => string.Equals(e.ControlTypeName, prefix.Value, StringComparison.OrdinalIgnoreCase)),

            PrefixKind.Aria => new FakeElementCondition(
                // Aria is translated to ControlType in production; in the fake we
                // just match on the role string against ControlTypeName for simplicity.
                e => string.Equals(e.ControlTypeName, prefix.Value, StringComparison.OrdinalIgnoreCase)),

            _ => throw new ArgumentException($"Unknown PrefixKind: {prefix.Kind}"),
        };
    }

    private static IElementCondition TranslateAttribute(SelectorAst.Attribute attr)
    {
        Func<IElementBackend, string?> getter = attr.Name switch
        {
            AttributeName.Name => e => e.Name,
            AttributeName.AutomationId => e => e.AutomationId,
            AttributeName.ClassName => e => e.ClassName,
            AttributeName.ControlType => e => e.ControlTypeName,
            AttributeName.FrameworkId => _ => null, // not on IElementBackend — always non-match
            _ => throw new ArgumentException($"Unknown AttributeName: {attr.Name}"),
        };

        var value = attr.Value;
        Func<IElementBackend, bool> predicate = attr.Op switch
        {
            AttributeOp.Equals => e => string.Equals(getter(e), value, StringComparison.Ordinal),
            AttributeOp.Contains => e => getter(e)?.Contains(value, StringComparison.OrdinalIgnoreCase) == true,
            AttributeOp.StartsWith => e => getter(e)?.StartsWith(value, StringComparison.OrdinalIgnoreCase) == true,
            AttributeOp.EndsWith => e => getter(e)?.EndsWith(value, StringComparison.OrdinalIgnoreCase) == true,
            AttributeOp.WordMatch => e => getter(e)?.Split(' ').Contains(value, StringComparer.OrdinalIgnoreCase) == true,
            _ => throw new ArgumentException($"Unknown AttributeOp: {attr.Op}"),
        };

        return new FakeElementCondition(predicate);
    }
}
