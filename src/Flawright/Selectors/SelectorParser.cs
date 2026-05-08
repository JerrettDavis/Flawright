#pragma warning disable MA0015 // MA0015: private parser helpers throw ArgumentException without a param name
// — these are internal state-machine errors propagated to the public API with context.

using System.Text;

namespace Flawright.Selectors;

/// <summary>
/// Recursive-descent parser that converts a Playwright-style selector string into
/// a backend-agnostic <see cref="SelectorAst"/>.
///
/// <para><b>Grammar</b></para>
/// <code>
/// selector   := simple ( ws? '>>' ws? simple )*
/// simple     := id | attr | prefix | bareName
/// id         := '#' ident
/// attr       := '[' attrName op value ']'
/// attrName   := 'name' | 'id' | 'automationid' | 'class' | 'classname'
///             | 'role' | 'controltype' | 'frameworkid'
/// op         := '=' | '*=' | '^=' | '$=' | '~='
/// value      := quoted | unquoted
/// quoted     := '"' ( '\"' | '\\' | [^"] )* '"'
///             | "'" ( "\'" | "\\" | [^'] )* "'"
/// unquoted   := [^]\s]+
/// prefix     := ('name'|'text'|'automationid'|'class'|'classname'
///             |  'role'|'controltype'|'aria') ':' value
/// bareName   := non-empty raw string  (Name equals)
/// </code>
///
/// <para><b>Examples</b></para>
/// <code>
/// #btn_ok                         → AutomationId("btn_ok")
/// [name=Save]                     → Attribute(Name, Equals, "Save")
/// [name*="Hello World"]           → Attribute(Name, Contains, "Hello World")
/// name:Save                       → Prefix(Name, "Save")
/// role:Button                     → Prefix(ControlType, "Button")
/// aria:button                     → Prefix(Aria, "button")
/// Save                            → BareName("Save")
/// [role=List] >> [name=Item]      → Chain([Prefix(ControlType,"List"), Attribute(Name,Equals,"Item")])
/// </code>
///
/// <para>
/// <c>xpath:</c> always throws <see cref="NotSupportedException"/> with a helpful message.
/// </para>
/// </summary>
internal static class SelectorParser
{
    /// <summary>
    /// Parses a selector string into a <see cref="SelectorAst"/>.
    /// </summary>
    /// <param name="selector">The raw selector string.</param>
    /// <returns>
    /// A <see cref="SelectorAst.Chain"/> when the selector contains <c>&gt;&gt;</c>
    /// combinators; a single-step AST node otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the selector is empty, whitespace-only, contains invalid syntax,
    /// or uses an unrecognised prefix.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the selector starts with <c>xpath:</c>.
    /// </exception>
    public static SelectorAst Parse(string selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var trimmed = selector.Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Selector must not be empty or whitespace.", nameof(selector));

        var ctx = new ParseContext(trimmed);
        var steps = new List<SelectorAst>();

        steps.Add(ParseSimple(ctx));

        while (true)
        {
            ctx.SkipWhitespace();
            if (!ctx.TryConsumeCombinator())
                break;
            ctx.SkipWhitespace();
            if (ctx.IsAtEnd)
                throw new ArgumentException(
                    $"Dangling combinator '>>' at end of selector: {selector}", nameof(selector));
            steps.Add(ParseSimple(ctx));
        }

        ctx.SkipWhitespace();
        if (!ctx.IsAtEnd)
            throw new ArgumentException(
                $"Unexpected characters at position {ctx.Position} in selector: {selector}", nameof(selector));

        return steps.Count == 1 ? steps[0] : new SelectorAst.Chain(steps.AsReadOnly());
    }

    /// <summary>
    /// Attempts to parse a selector string into a <see cref="SelectorAst"/>.
    /// </summary>
    /// <param name="selector">The raw selector string.</param>
    /// <param name="ast">
    /// When this method returns <see langword="true"/>, contains the parsed AST;
    /// otherwise <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if parsing succeeded; <see langword="false"/> otherwise.</returns>
    public static bool TryParse(string selector, out SelectorAst? ast)
    {
        try
        {
            ast = Parse(selector);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException or NotSupportedException)
        {
            ast = null;
            return false;
        }
    }

    // ── Grammar productions ───────────────────────────────────────────────────

    private static SelectorAst ParseSimple(ParseContext ctx)
    {
        ctx.SkipWhitespace();

        if (ctx.IsAtEnd)
            throw new ArgumentException("Expected a selector step but found end of input.");

        var ch = ctx.Current;

        if (ch == '#')
            return ParseId(ctx);

        if (ch == '[')
            return ParseAttr(ctx);

        return ParsePrefixOrBareName(ctx);
    }

    /// <summary>Parses <c>#ident</c> into <see cref="SelectorAst.AutomationId"/>.</summary>
    private static SelectorAst.AutomationId ParseId(ParseContext ctx)
    {
        ctx.Consume('#');
        var id = ctx.ReadUntilWhitespaceOrCombinator();
        if (id.Length == 0)
            throw new ArgumentException("Expected an automation ID after '#'.");
        return new SelectorAst.AutomationId(id);
    }

    /// <summary>Parses <c>[attrName op value]</c> into <see cref="SelectorAst.Attribute"/>.</summary>
    private static SelectorAst.Attribute ParseAttr(ParseContext ctx)
    {
        ctx.Consume('[');

        // Read attribute name (stops at '=', '*', '^', '$', '~', ']', or whitespace)
        var rawName = ctx.ReadAttrName();
        if (rawName.Length == 0)
            throw new ArgumentException("Expected an attribute name inside '[...]'.");

        var attrName = ParseAttributeName(rawName);

        // Read operator (one of =, *=, ^=, $=, ~=)
        var op = ParseAttributeOp(ctx);

        // Read value
        var value = ParseValue(ctx);

        ctx.SkipWhitespace();
        if (ctx.IsAtEnd || ctx.Current != ']')
            throw new ArgumentException(
                $"Expected ']' to close attribute selector, but found '{(ctx.IsAtEnd ? "end of input" : ctx.Current.ToString())}'.");
        ctx.Advance();

        return new SelectorAst.Attribute(attrName, op, value);
    }

    /// <summary>Parses a prefix selector or a bare name.</summary>
    private static SelectorAst ParsePrefixOrBareName(ParseContext ctx)
    {
        // Peek ahead to see if this looks like "prefix:value".
        // A prefix is a known keyword followed immediately by ':'.
        var peek = ctx.PeekToken();

        // Check for xpath: early so we can throw NotSupportedException
        if (peek.Equals("xpath", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "XPath selectors are not supported. " +
                "Use locator chaining: page.Locator(...).Locator(...) instead.");
        }

        var kind = TryGetPrefixKind(peek);
        if (kind.HasValue && !ctx.IsAtEnd && ctx.PeekCharAfterToken(peek) == ':')
        {
            // Consume the prefix token and the colon
            ctx.Advance(peek.Length);
            ctx.Consume(':');

            if (ctx.IsAtEnd)
                throw new ArgumentException($"Expected a value after prefix '{peek}:'.");

            var value = ParseValue(ctx);
            if (value.Length == 0)
                throw new ArgumentException($"Expected a non-empty value after prefix '{peek}:'.");

            return new SelectorAst.Prefix(kind.Value, value);
        }

        // Check if it's a colon-prefixed unknown prefix (e.g. "css:", "id:", "foo:")
        // We want to throw for unknown prefixes that look like prefix:value
        var colonPos = ctx.FindColonInNextToken();
        if (colonPos >= 0)
        {
            var unknownPrefix = peek.Length > 0 ? peek : ctx.ReadUntilChar(':');
            throw new ArgumentException(
                $"Unknown selector prefix '{unknownPrefix}:'. " +
                $"Supported prefixes: name, text, automationid, class, classname, role, controltype, aria.");
        }

        // Bare name
        var name = ctx.ReadUntilWhitespaceOrCombinator();
        if (name.Length == 0)
            throw new ArgumentException("Expected a non-empty selector step.");

        return new SelectorAst.BareName(name);
    }

    // ── Parsing helpers ───────────────────────────────────────────────────────

    private static AttributeName ParseAttributeName(string raw)
    {
        return raw.ToUpperInvariant() switch
        {
            "NAME" => AttributeName.Name,
            "ID" or "AUTOMATIONID" => AttributeName.AutomationId,
            "CLASS" or "CLASSNAME" => AttributeName.ClassName,
            "ROLE" or "CONTROLTYPE" => AttributeName.ControlType,
            "FRAMEWORKID" => AttributeName.FrameworkId,
            _ => throw new ArgumentException(
                $"Unknown attribute name '{raw}'. " +
                $"Supported: name, id, automationid, class, classname, role, controltype, frameworkid."),
        };
    }

    private static AttributeOp ParseAttributeOp(ParseContext ctx)
    {
        ctx.SkipWhitespace();
        if (ctx.IsAtEnd)
            throw new ArgumentException("Expected an attribute operator but found end of input.");

        var ch = ctx.Current;
        if (ch == '=')
        {
            ctx.Advance();
            return AttributeOp.Equals;
        }

        if (ctx.Position + 1 < ctx.Length)
        {
            var twoChar = ctx.Source.Substring(ctx.Position, 2);
            if (string.Equals(twoChar, "*=", StringComparison.Ordinal)) { ctx.Advance(2); return AttributeOp.Contains; }
            if (string.Equals(twoChar, "^=", StringComparison.Ordinal)) { ctx.Advance(2); return AttributeOp.StartsWith; }
            if (string.Equals(twoChar, "$=", StringComparison.Ordinal)) { ctx.Advance(2); return AttributeOp.EndsWith; }
            if (string.Equals(twoChar, "~=", StringComparison.Ordinal)) { ctx.Advance(2); return AttributeOp.WordMatch; }
        }

        throw new ArgumentException(
            $"Expected an attribute operator ('=', '*=', '^=', '$=', '~=') but found '{ch}'.");
    }

    private static string ParseValue(ParseContext ctx)
    {
        ctx.SkipWhitespace();
        if (ctx.IsAtEnd)
            return string.Empty;

        var ch = ctx.Current;
        if (ch == '"' || ch == '\'')
            return ParseQuotedValue(ctx, ch);

        return ctx.ReadUnquotedValue();
    }

    private static string ParseQuotedValue(ParseContext ctx, char quote)
    {
        ctx.Advance(); // consume opening quote
        var sb = new StringBuilder();

        while (!ctx.IsAtEnd)
        {
            var ch = ctx.Current;
            if (ch == '\\' && ctx.Position + 1 < ctx.Length)
            {
                ctx.Advance();
                var escaped = ctx.Current;
                sb.Append(escaped switch
                {
                    '"' => '"',
                    '\'' => '\'',
                    '\\' => '\\',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => escaped, // unknown escape: keep the char
                });
                ctx.Advance();
            }
            else if (ch == quote)
            {
                ctx.Advance(); // consume closing quote
                return sb.ToString();
            }
            else
            {
                sb.Append(ch);
                ctx.Advance();
            }
        }

        throw new ArgumentException($"Unterminated quoted string (expected closing '{quote}').");
    }

    private static PrefixKind? TryGetPrefixKind(string token)
    {
        return token.ToUpperInvariant() switch
        {
            "NAME" => PrefixKind.Name,
            "TEXT" => PrefixKind.Text,
            "AUTOMATIONID" => PrefixKind.AutomationId,
            "CLASS" or "CLASSNAME" => PrefixKind.ClassName,
            "ROLE" or "CONTROLTYPE" => PrefixKind.ControlType,
            "ARIA" => PrefixKind.Aria,
            _ => null,
        };
    }

    // ── ParseContext ──────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight mutable cursor over the selector string.
    /// All positions are in terms of the <see cref="Source"/> string.
    /// </summary>
    private sealed class ParseContext(string source)
    {
        public string Source { get; } = source;
        public int Position { get; private set; }
        public int Length => Source.Length;
        public bool IsAtEnd => Position >= Length;
        public char Current => Source[Position];

        public void Advance(int count = 1) => Position += count;

        public void Consume(char expected)
        {
            if (IsAtEnd || Current != expected)
                throw new ArgumentException(
                    $"Expected '{expected}' at position {Position} but found '{(IsAtEnd ? "end" : Current.ToString())}'.");
            Advance();
        }

        public void SkipWhitespace()
        {
            while (!IsAtEnd && char.IsWhiteSpace(Current))
                Advance();
        }

        /// <summary>
        /// Attempts to consume the <c>&gt;&gt;</c> combinator.
        /// Returns false if the next non-whitespace chars are not <c>&gt;&gt;</c>.
        /// </summary>
        public bool TryConsumeCombinator()
        {
            var saved = Position;
            SkipWhitespace();
            if (Position + 1 < Length
                && Source[Position] == '>'
                && Source[Position + 1] == '>')
            {
                Advance(2);
                return true;
            }
            Position = saved;
            return false;
        }

        /// <summary>
        /// Reads the attribute name token: chars that are not '=', '*', '^',
        /// '$', '~', ']', '[', or whitespace.
        /// </summary>
        public string ReadAttrName()
        {
            var start = Position;
            while (!IsAtEnd)
            {
                var ch = Current;
                if (ch is '=' or '*' or '^' or '$' or '~' or ']' or '[' || char.IsWhiteSpace(ch))
                    break;
                Advance();
            }
            return Source[start..Position];
        }

        /// <summary>
        /// Reads until whitespace or the start of a <c>&gt;&gt;</c> combinator.
        /// </summary>
        public string ReadUntilWhitespaceOrCombinator()
        {
            var start = Position;
            while (!IsAtEnd)
            {
                var ch = Current;
                if (char.IsWhiteSpace(ch))
                    break;
                // Stop at >> combinator (two consecutive '>')
                if (ch == '>' && Position + 1 < Length && Source[Position + 1] == '>')
                    break;
                Advance();
            }
            return Source[start..Position];
        }

        /// <summary>
        /// Reads an unquoted value: chars that are not ']' or whitespace.
        /// </summary>
        public string ReadUnquotedValue()
        {
            var start = Position;
            while (!IsAtEnd)
            {
                var ch = Current;
                if (ch == ']' || char.IsWhiteSpace(ch))
                    break;
                // Stop at >> combinator
                if (ch == '>' && Position + 1 < Length && Source[Position + 1] == '>')
                    break;
                Advance();
            }
            return Source[start..Position];
        }

        /// <summary>
        /// Peeks the next identifier-like token (letters, digits, hyphens, underscores)
        /// without advancing the position.
        /// </summary>
        public string PeekToken()
        {
            var i = Position;
            while (i < Length)
            {
                var ch = Source[i];
                if (!char.IsLetterOrDigit(ch) && ch != '-' && ch != '_')
                    break;
                i++;
            }
            return Source[Position..i];
        }

        /// <summary>
        /// Returns the character immediately after the given token from the current
        /// position, or the null char if that would be out of range.
        /// </summary>
        public char PeekCharAfterToken(string token)
        {
            var idx = Position + token.Length;
            return idx < Length ? Source[idx] : '\0';
        }

        /// <summary>
        /// Returns the index (relative to the current position) of the first ':'
        /// in the upcoming token, or -1 if there is none before whitespace or end.
        /// </summary>
        public int FindColonInNextToken()
        {
            var i = Position;
            while (i < Length)
            {
                var ch = Source[i];
                if (ch == ':')
                    return i - Position;
                if (char.IsWhiteSpace(ch) || ch == '[' || ch == ']' || ch == '>')
                    return -1;
                i++;
            }
            return -1;
        }

        /// <summary>Reads until the specified character without consuming it.</summary>
        public string ReadUntilChar(char stop)
        {
            var start = Position;
            while (!IsAtEnd && Current != stop)
                Advance();
            return Source[start..Position];
        }
    }
}
