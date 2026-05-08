#pragma warning disable CA1034 // Nested types are intentional — discriminated-union pattern for selector AST nodes
#pragma warning disable CA1711 // 'Attribute' suffix is intentional — it mirrors the selector grammar term

namespace Flawright.Selectors;

/// <summary>
/// Backend-agnostic abstract syntax tree produced by <see cref="SelectorParser"/>.
///
/// <para>
/// The AST represents a parsed Playwright-style selector string without any
/// dependency on FlaUI or any other UI-automation backend.  Backends translate
/// an <see cref="SelectorAst"/> into native conditions through
/// <c>IConditionTranslator</c>.
/// </para>
///
/// <para><b>Grammar summary</b></para>
/// <code>
/// selector   := simple ( ws? '>>' ws? simple )*
/// simple     := id | attr | prefix | bareName
/// id         := '#' ident
/// attr       := '[' attrName op value ']'
/// prefix     := prefixKind ':' value
/// bareName   := non-empty raw string (Name equals)
/// </code>
///
/// <para><b>Examples</b></para>
/// <code>
/// #btn_ok                          → AutomationId("btn_ok")
/// [name=Save]                      → Attribute(Name, Equals, "Save")
/// [name*=Save]                     → Attribute(Name, Contains, "Save")
/// name:Save                        → Prefix(Name, "Save")
/// role:Button                      → Prefix(ControlType, "Button")
/// aria:button                      → Prefix(Aria, "button")
/// [role=Button] >> [name=OK]       → Chain([Prefix(ControlType,"Button"), Attribute(Name,Equals,"OK")])
/// Save                             → BareName("Save")
/// </code>
/// </summary>
public abstract record SelectorAst
{
    // Prevent external derivation while keeping records open for pattern matching.
    private protected SelectorAst() { }

    /// <summary>
    /// Represents a chained selector made up of two or more descendant steps,
    /// separated by the <c>&gt;&gt;</c> combinator.
    ///
    /// <para>
    /// Each step must match a descendant of the elements matched by the
    /// previous step.  <c>FlawrightLocator.FindAll</c> (Wave C) iterates: start
    /// from the root, find all matches of <c>Steps[0]</c>, then for each find
    /// descendants matching <c>Steps[1]</c>, and so on.
    /// </para>
    /// </summary>
    /// <param name="Steps">The ordered list of per-step AST nodes (never empty, never a nested <see cref="Chain"/>).</param>
    public sealed record Chain(IReadOnlyList<SelectorAst> Steps) : SelectorAst;

    /// <summary>
    /// Matches elements whose <c>AutomationId</c> equals <paramref name="Value"/>.
    /// Produced by the <c>#ident</c> syntax.
    /// </summary>
    /// <param name="Value">The automation ID to match (exact, case-sensitive).</param>
    public sealed record AutomationId(string Value) : SelectorAst;

    /// <summary>
    /// Matches elements using an attribute selector of the form
    /// <c>[attrName op value]</c>.
    /// </summary>
    /// <param name="Name">Which attribute is being tested.</param>
    /// <param name="Op">The comparison operator.</param>
    /// <param name="Value">The value to compare against.</param>
    public sealed record Attribute(AttributeName Name, AttributeOp Op, string Value) : SelectorAst;

    /// <summary>
    /// Matches elements using a colon-prefix selector of the form
    /// <c>prefix:value</c>.
    /// </summary>
    /// <param name="Kind">The prefix kind.</param>
    /// <param name="Value">The value after the colon.</param>
    public sealed record Prefix(PrefixKind Kind, string Value) : SelectorAst;

    /// <summary>
    /// Matches elements whose <c>Name</c> equals <paramref name="Value"/> exactly.
    /// Produced by a bare (unprefixed, unbracketed) string that doesn't look like
    /// any other syntax.
    /// </summary>
    /// <param name="Value">The name to match.</param>
    public sealed record BareName(string Value) : SelectorAst;
}

/// <summary>
/// Identifies which element attribute an <see cref="SelectorAst.Attribute"/> node tests.
/// </summary>
public enum AttributeName
{
    /// <summary>The UIA <c>Name</c> property.</summary>
    Name,

    /// <summary>The UIA <c>AutomationId</c> property.</summary>
    AutomationId,

    /// <summary>The UIA <c>ClassName</c> property.</summary>
    ClassName,

    /// <summary>The UIA <c>ControlType</c> (matched by type name string).</summary>
    ControlType,

    /// <summary>The UIA <c>FrameworkId</c> property (e.g. "WPF", "Win32").</summary>
    FrameworkId,
}

/// <summary>
/// Comparison operator used in an <see cref="SelectorAst.Attribute"/> node.
/// </summary>
public enum AttributeOp
{
    /// <summary>Exact equality (<c>=</c>).</summary>
    Equals,

    /// <summary>Substring match (<c>*=</c>). Applied as an in-memory post-filter.</summary>
    Contains,

    /// <summary>Prefix match (<c>^=</c>). Applied as an in-memory post-filter.</summary>
    StartsWith,

    /// <summary>Suffix match (<c>$=</c>). Applied as an in-memory post-filter.</summary>
    EndsWith,

    /// <summary>
    /// Space-delimited word match (<c>~=</c>).
    /// The value must appear as a whole word in the attribute's space-separated list.
    /// Applied as an in-memory post-filter.
    /// </summary>
    WordMatch,
}

/// <summary>
/// Identifies the kind of colon-prefix used in a <see cref="SelectorAst.Prefix"/> node.
/// </summary>
public enum PrefixKind
{
    /// <summary><c>name:value</c> — matches by UIA Name (exact).</summary>
    Name,

    /// <summary><c>text:value</c> — alias for <see cref="Name"/>; matches by UIA Name.</summary>
    Text,

    /// <summary><c>automationid:value</c> — matches by UIA AutomationId (exact).</summary>
    AutomationId,

    /// <summary><c>class:value</c> or <c>classname:value</c> — matches by UIA ClassName (exact).</summary>
    ClassName,

    /// <summary><c>controltype:value</c> — matches by UIA ControlType name (exact).</summary>
    ControlType,

    /// <summary>
    /// <c>aria:role</c> — matches by ARIA role name, translated to a UIA ControlType
    /// via <c>AriaRoleMapper</c>.  Wave B.2 provides the mapping; the translator
    /// accepts a <c>Func&lt;AriaRole, ControlType&gt;</c> seam.
    /// </summary>
    Aria,
}
