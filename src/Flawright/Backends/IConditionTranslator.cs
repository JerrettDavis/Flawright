using Flawright.Selectors;

namespace Flawright.Backends;

/// <summary>
/// Translates a backend-agnostic <see cref="SelectorAst"/> into a
/// <see cref="SelectorPipeline"/> of backend-native <see cref="IElementCondition"/>
/// steps.
///
/// <para>
/// Each backend provides its own implementation (e.g.
/// <c>UiaConditionTranslator</c> for FlaUI; <c>FakeConditionTranslator</c> for
/// tests).  The translator is the only place where <see cref="SelectorAst"/>
/// nodes are converted to native conditions, keeping the parser free of any
/// backend dependency.
/// </para>
/// </summary>
internal interface IConditionTranslator
{
    /// <summary>
    /// Translates <paramref name="ast"/> into a <see cref="SelectorPipeline"/>.
    /// </summary>
    /// <param name="ast">The parsed selector AST produced by <see cref="SelectorParser"/>.</param>
    /// <returns>
    /// A <see cref="SelectorPipeline"/> whose <see cref="SelectorPipeline.Steps"/>
    /// list contains one <see cref="IElementCondition"/> per chain step (or a
    /// single-element list for non-chain selectors).
    /// </returns>
    SelectorPipeline Translate(SelectorAst ast);
}

/// <summary>
/// An ordered list of <see cref="IElementCondition"/> steps that together
/// represent a full selector, including any <c>&gt;&gt;</c> chaining.
///
/// <para>
/// <c>FlawrightLocator.FindAll</c> (Wave C) iterates: from root find all
/// matching <c>Steps[0]</c>, then for each find descendants matching
/// <c>Steps[1]</c>, and so on.
/// </para>
/// </summary>
/// <param name="Steps">
/// One condition per chain step.  For a non-chain selector this is a
/// single-element list.
/// </param>
internal sealed record SelectorPipeline(IReadOnlyList<IElementCondition> Steps)
{
    /// <summary>
    /// Convenience factory for a single-step pipeline.
    /// </summary>
    public static SelectorPipeline Single(IElementCondition condition) =>
        new(new[] { condition });
}
