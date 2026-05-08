using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace Flawright.Backends.Uia;

/// <summary>
/// FlaUI-backed implementation of <see cref="IElementBackend"/>.
///
/// This is the <strong>only</strong> class in the production library that may
/// reference <c>FlaUI.Core.*</c> or <c>FlaUI.UIA3.*</c>.  All other classes
/// must depend only on <see cref="IElementBackend"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "FlaUI I/O; covered by E2E tests only.")]
internal sealed class UiaElementBackend : IElementBackend
{
    private readonly AutomationElement _element;

    internal UiaElementBackend(AutomationElement element)
    {
        _element = element;
    }

    /// <summary>Exposes the underlying FlaUI element for advanced scenarios.</summary>
    internal AutomationElement Element => _element;

    // ── Identity ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public string? AutomationId => _element.AutomationId;

    /// <inheritdoc/>
    public string? Name => _element.Name;

    /// <inheritdoc/>
    public string? ClassName => _element.ClassName;

    /// <inheritdoc/>
    public string ControlTypeName => _element.ControlType.ToString();

    // ── State ─────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool IsEnabled => _element.IsEnabled;

    /// <inheritdoc/>
    public bool IsOffscreen => _element.IsOffscreen;

    /// <inheritdoc/>
    public Rectangle BoundingRectangle
    {
        get
        {
            var r = _element.BoundingRectangle;
            return new Rectangle(r.X, r.Y, r.Width, r.Height);
        }
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Click() => _element.Click();

    /// <inheritdoc/>
    public void DoubleClick() => _element.DoubleClick();

    /// <inheritdoc/>
    public void Focus() => _element.Focus();

    // ── Pattern operations ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool TryInvoke()
    {
        var ip = _element.Patterns.Invoke;
        if (ip.IsSupported)
        {
            ip.Pattern.Invoke();
            return true;
        }

        var la = _element.Patterns.LegacyIAccessible;
        if (la.IsSupported)
        {
            la.Pattern.DoDefaultAction();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public bool TrySetValue(string text)
    {
        var vp = _element.Patterns.Value;
        if (vp.IsSupported)
        {
            vp.Pattern.SetValue(text);
            return true;
        }

#pragma warning disable CA1031 // Best-effort TextBox fallback
        try
        {
            var tb = _element.AsTextBox();
            if (tb != null)
            {
                tb.Text = text;
                return true;
            }
        }
        catch (Exception)
        {
            // AsTextBox can throw on controls that don't support the abstraction
        }
#pragma warning restore CA1031

        return false;
    }

    /// <inheritdoc/>
    public string? TryGetValue()
    {
        var vp = _element.Patterns.Value;
        return vp.IsSupported ? vp.Pattern.Value.Value : null;
    }

    /// <inheritdoc/>
    public string? TryGetDocumentText()
    {
        var tp = _element.Patterns.Text;
        if (!tp.IsSupported)
            return null;

#pragma warning disable CA1031 // Return null if TextPattern read fails
        try
        {
            return tp.Pattern.DocumentRange.GetText(-1);
        }
        catch (Exception)
        {
            return null;
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    public bool TryToggleOn()
    {
        var tp = _element.Patterns.Toggle;
        if (!tp.IsSupported)
            return false;

        for (var i = 0; i < 2; i++)
        {
            if (tp.Pattern.ToggleState.Value == ToggleState.On)
                return true;
            tp.Pattern.Toggle();
        }

        return tp.Pattern.ToggleState.Value == ToggleState.On;
    }

    /// <inheritdoc/>
    public bool TryToggleOff()
    {
        var tp = _element.Patterns.Toggle;
        if (!tp.IsSupported)
            return false;

        for (var i = 0; i < 2; i++)
        {
            if (tp.Pattern.ToggleState.Value == ToggleState.Off)
                return true;
            tp.Pattern.Toggle();
        }

        return tp.Pattern.ToggleState.Value == ToggleState.Off;
    }

    /// <inheritdoc/>
    public bool? GetToggleState()
    {
        var tp = _element.Patterns.Toggle;
        if (!tp.IsSupported)
            return null;

        return tp.Pattern.ToggleState.Value switch
        {
            ToggleState.On => true,
            ToggleState.Off => false,
            _ => null // Indeterminate
        };
    }

    /// <inheritdoc/>
    public bool TryScrollIntoView()
    {
        var sp = _element.Patterns.ScrollItem;
        if (!sp.IsSupported)
            return false;

        sp.Pattern.ScrollIntoView();
        return true;
    }

    /// <inheritdoc/>
    public bool TrySelectItem(string nameOrId)
    {
        var descendants = _element.FindAllDescendants();
        var target = System.Array.Find(
            descendants,
            d => string.Equals(d.Name, nameOrId, StringComparison.OrdinalIgnoreCase)
              || string.Equals(d.AutomationId, nameOrId, StringComparison.OrdinalIgnoreCase));

        if (target == null)
            return false;

        var sip = target.Patterns.SelectionItem;
        if (!sip.IsSupported)
            return false;

        sip.Pattern.Select();
        return true;
    }

    // ── Tree traversal ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public IEnumerable<IElementBackend> FindAll(IElementCondition condition)
    {
        if (condition is not UiaElementCondition uiaCondition)
            throw new ArgumentException(
                $"Expected a {nameof(UiaElementCondition)} but received {condition.GetType().Name}.",
                nameof(condition));

        var raw = _element.FindAllDescendants(uiaCondition.NativeCondition);
        IEnumerable<IElementBackend> backends = raw.Select(e => (IElementBackend)new UiaElementBackend(e));

        if (uiaCondition.PostFilter != null)
            backends = backends.Where(uiaCondition.PostFilter);

        return backends;
    }

    /// <inheritdoc/>
    public IElementBackend? FindFirst(IElementCondition condition)
    {
        if (condition is not UiaElementCondition uiaCondition)
            throw new ArgumentException(
                $"Expected a {nameof(UiaElementCondition)} but received {condition.GetType().Name}.",
                nameof(condition));

        if (uiaCondition.PostFilter != null)
        {
            // Post-filter requires us to enumerate; can't use FindFirstDescendant shortcut
            return FindAll(condition).FirstOrDefault();
        }

        var raw = _element.FindFirstDescendant(uiaCondition.NativeCondition);
        return raw == null ? null : new UiaElementBackend(raw);
    }
}
