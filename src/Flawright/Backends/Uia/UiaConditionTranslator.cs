using System.Diagnostics.CodeAnalysis;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using Flawright.Selectors;

namespace Flawright.Backends.Uia;

/// <summary>
/// FlaUI-backed implementation of <see cref="IConditionTranslator"/> that
/// translates <see cref="SelectorAst"/> nodes into <see cref="UiaElementCondition"/>
/// instances using a <see cref="ConditionFactory"/>.
///
/// <para>
/// Exact-equality conditions (<c>=</c>) are expressed as native FlaUI
/// <see cref="PropertyCondition"/> objects.  Substring, prefix, suffix, and
/// word-match operators are not supported by UIA natively, so they are
/// implemented as in-memory post-filters on top of <see cref="TrueCondition.Default"/>
/// (a match-all native condition that causes <c>FindAllDescendants</c> to return
/// every element, letting the post-filter narrow the set).
/// </para>
///
/// <para>
/// <b>AriaRole dependency (Wave B.2 seam):</b> The <see cref="PrefixKind.Aria"/>
/// case delegates to the <c>ariaRoleToControlType</c> function supplied at
/// construction time.  <c>AriaRoleMapper.Map</c> (Wave B.2) must be wired here
/// once it exists.  Until then the constructor accepts any compatible delegate
/// (including one that throws <see cref="NotImplementedException"/>).
/// </para>
/// </summary>
[ExcludeFromCodeCoverage(Justification = "FlaUI I/O; covered by E2E tests only.")]
internal sealed class UiaConditionTranslator : IConditionTranslator
{
    private readonly ConditionFactory _cf;

    // ── Wave B.2 seam ─────────────────────────────────────────────────────────
    // AriaRoleMapper.Map will be wired in here once Wave B.2 lands.
    // Until then, pass: _ => throw new NotImplementedException("AriaRoleMapper not wired yet.")
    private readonly Func<AriaRole, ControlType> _ariaRoleToControlType;

    /// <summary>
    /// Initialises the translator.
    /// </summary>
    /// <param name="cf">
    /// The FlaUI <see cref="ConditionFactory"/> obtained from the automation instance.
    /// </param>
    /// <param name="ariaRoleToControlType">
    /// Function that maps an <see cref="AriaRole"/> to a UIA <see cref="ControlType"/>.
    /// Provided by <c>AriaRoleMapper.Map</c> (Wave B.2).
    /// </param>
    internal UiaConditionTranslator(
        ConditionFactory cf,
        Func<AriaRole, ControlType> ariaRoleToControlType)
    {
        _cf = cf;
        _ariaRoleToControlType = ariaRoleToControlType;
    }

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

    // ── Per-step translation ──────────────────────────────────────────────────

    private IElementCondition TranslateStep(SelectorAst ast) =>
        ast switch
        {
            SelectorAst.AutomationId a => TranslateAutomationId(a),
            SelectorAst.Attribute a => TranslateAttribute(a),
            SelectorAst.Prefix p => TranslatePrefix(p),
            SelectorAst.BareName b => TranslateBareName(b),
            SelectorAst.Chain => throw new ArgumentException(
                "Nested Chain nodes are not valid; Chain is handled at the top level of Translate.",
                nameof(ast)),
            _ => throw new ArgumentException(
                $"Unknown SelectorAst type: {ast.GetType().Name}",
                nameof(ast)),
        };

    private UiaElementCondition TranslateAutomationId(SelectorAst.AutomationId ast) =>
        new(_cf.ByAutomationId(ast.Value));

    private UiaElementCondition TranslateBareName(SelectorAst.BareName ast) =>
        new(_cf.ByName(ast.Value));

    private UiaElementCondition TranslateAttribute(SelectorAst.Attribute ast)
    {
        return ast.Name switch
        {
            AttributeName.Name => TranslateStringProperty(
                ast.Op,
                ast.Value,
                exactCondition: _cf.ByName(ast.Value),
                getter: e => e.Name),

            AttributeName.AutomationId => TranslateStringProperty(
                ast.Op,
                ast.Value,
                exactCondition: _cf.ByAutomationId(ast.Value),
                getter: e => e.AutomationId),

            AttributeName.ClassName => TranslateStringProperty(
                ast.Op,
                ast.Value,
                exactCondition: _cf.ByClassName(ast.Value),
                getter: e => e.ClassName),

            AttributeName.ControlType => TranslateControlTypeAttribute(ast),

            AttributeName.FrameworkId => TranslateFrameworkIdAttribute(ast),

            _ => throw new ArgumentException(
                $"Unknown AttributeName: {ast.Name}",
                nameof(ast)),
        };
    }

    private UiaElementCondition TranslateControlTypeAttribute(SelectorAst.Attribute ast)
    {
        if (ast.Op == AttributeOp.Equals)
        {
            var ct = ParseControlTypeName(ast.Value);
            return new UiaElementCondition(_cf.ByControlType(ct));
        }

        // Non-exact: post-filter on ControlTypeName string representation
        var value = ast.Value;
        Func<IElementBackend, bool> postFilter = ast.Op switch
        {
            AttributeOp.Contains => e => e.ControlTypeName.Contains(value, StringComparison.OrdinalIgnoreCase),
            AttributeOp.StartsWith => e => e.ControlTypeName.StartsWith(value, StringComparison.OrdinalIgnoreCase),
            AttributeOp.EndsWith => e => e.ControlTypeName.EndsWith(value, StringComparison.OrdinalIgnoreCase),
            AttributeOp.WordMatch => e => e.ControlTypeName.Split(' ').Contains(value, StringComparer.OrdinalIgnoreCase),
            _ => throw new ArgumentException($"Unknown AttributeOp: {ast.Op}", nameof(ast)),
        };

        return new UiaElementCondition(TrueCondition.Default, postFilter);
    }

    private UiaElementCondition TranslateFrameworkIdAttribute(SelectorAst.Attribute ast)
    {
        // FrameworkId exact match: use native ByFrameworkId condition
        if (ast.Op == AttributeOp.Equals)
            return new UiaElementCondition(_cf.ByFrameworkId(ast.Value));

        // Non-exact: TrueCondition + post-filter via UiaElementBackend.Element.FrameworkType
        var value = ast.Value;
        Func<IElementBackend, bool> postFilter = ast.Op switch
        {
            AttributeOp.Contains => e => GetFrameworkId(e)?.Contains(value, StringComparison.OrdinalIgnoreCase) == true,
            AttributeOp.StartsWith => e => GetFrameworkId(e)?.StartsWith(value, StringComparison.OrdinalIgnoreCase) == true,
            AttributeOp.EndsWith => e => GetFrameworkId(e)?.EndsWith(value, StringComparison.OrdinalIgnoreCase) == true,
            AttributeOp.WordMatch => e => GetFrameworkId(e)?.Split(' ').Contains(value, StringComparer.OrdinalIgnoreCase) == true,
            _ => throw new ArgumentException($"Unknown AttributeOp: {ast.Op}", nameof(ast)),
        };

        return new UiaElementCondition(TrueCondition.Default, postFilter);
    }

    private UiaElementCondition TranslateStringProperty(
        AttributeOp op,
        string value,
        ConditionBase exactCondition,
        Func<IElementBackend, string?> getter)
    {
        if (op == AttributeOp.Equals)
            return new UiaElementCondition(exactCondition);

        // Non-exact ops: TrueCondition + post-filter (UIA has no native substring support)
        Func<IElementBackend, bool> postFilter = op switch
        {
            AttributeOp.Contains => e => getter(e)?.Contains(value, StringComparison.OrdinalIgnoreCase) == true,
            AttributeOp.StartsWith => e => getter(e)?.StartsWith(value, StringComparison.OrdinalIgnoreCase) == true,
            AttributeOp.EndsWith => e => getter(e)?.EndsWith(value, StringComparison.OrdinalIgnoreCase) == true,
            AttributeOp.WordMatch => e => getter(e)?.Split(' ').Contains(value, StringComparer.OrdinalIgnoreCase) == true,
            _ => throw new ArgumentException($"Unknown AttributeOp: {op}", nameof(op)),
        };

        return new UiaElementCondition(TrueCondition.Default, postFilter);
    }

    private UiaElementCondition TranslatePrefix(SelectorAst.Prefix ast)
    {
        return ast.Kind switch
        {
            PrefixKind.Name => new UiaElementCondition(_cf.ByName(ast.Value)),
            PrefixKind.Text => new UiaElementCondition(_cf.ByName(ast.Value)),
            PrefixKind.AutomationId => new UiaElementCondition(_cf.ByAutomationId(ast.Value)),
            PrefixKind.ClassName => new UiaElementCondition(_cf.ByClassName(ast.Value)),
            PrefixKind.ControlType => new UiaElementCondition(_cf.ByControlType(ParseControlTypeName(ast.Value))),
            PrefixKind.Aria => TranslateAriaPrefix(ast.Value),
            _ => throw new ArgumentException($"Unknown PrefixKind: {ast.Kind}", nameof(ast)),
        };
    }

    private UiaElementCondition TranslateAriaPrefix(string roleValue)
    {
        if (!Enum.TryParse<AriaRole>(roleValue, ignoreCase: true, out var ariaRole))
            throw new ArgumentException(
                $"'{roleValue}' is not a recognised AriaRole. Check the AriaRole enum for valid values.",
                nameof(roleValue));

        // Wave B.2 seam: AriaRoleMapper.Map is injected via _ariaRoleToControlType
        var controlType = _ariaRoleToControlType(ariaRole);
        return new UiaElementCondition(_cf.ByControlType(controlType));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ControlType ParseControlTypeName(string value) =>
        ControlTypeParser.Parse(value);

    /// <summary>
    /// Gets the FrameworkId string of an element via its underlying
    /// <see cref="UiaElementBackend"/>. Returns <see langword="null"/> for
    /// non-UIA backends or on COM failure.
    /// </summary>
    private static string? GetFrameworkId(IElementBackend backend)
    {
        if (backend is not UiaElementBackend uia)
            return null;

#pragma warning disable CA1031 // catch-all for COM failures
        try
        {
            return uia.Element.FrameworkAutomationElement.FrameworkId;
        }
        catch
        {
            return null;
        }
#pragma warning restore CA1031
    }
}
