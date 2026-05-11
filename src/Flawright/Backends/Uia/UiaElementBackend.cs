using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
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

    /// <summary>Gets the native window handle (HWND) of the top-level window.</summary>
    internal nint NativeWindowHandle => _element.Properties.NativeWindowHandle.ValueOrDefault;

    // ── Identity ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public string? AutomationId => _element.AutomationId;

    /// <inheritdoc/>
    public string? Name => _element.Name;

    /// <inheritdoc/>
    public string? ClassName => _element.ClassName;

    /// <inheritdoc/>
    public string ControlTypeName => _element.ControlType.ToString();

    /// <inheritdoc/>
    public string? FrameworkId
    {
        get
        {
            // FlaUI v5.0.0 does not expose FrameworkId as a public property on AutomationElement;
            // we access it via reflection against the underlying UIA automation element.
            try
            {
                var frameworkIdProperty = typeof(AutomationElement).GetProperty("FrameworkId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
                if (frameworkIdProperty is not null && frameworkIdProperty.CanRead)
                {
                    return (string?)frameworkIdProperty.GetValue(_element);
                }

                return null;
            }
            catch (System.Reflection.TargetException)
            {
                // Property exists but cannot be accessed on this object.
                return null;
            }
            catch (System.Reflection.TargetInvocationException)
            {
                // Property getter threw an exception.
                return null;
            }
        }
    }

    // ── State ─────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool IsEnabled => _element.IsEnabled;

    /// <inheritdoc/>
    public bool IsOffscreen => _element.IsOffscreen;

    /// <inheritdoc/>
    public bool HasKeyboardFocus
    {
        get
        {
#pragma warning disable CA1031 // Return false if property read fails
            try
            {
                return _element.Properties.HasKeyboardFocus.TryGetValue(out var v) && v;
            }
            catch (Exception)
            {
                return false;
            }
#pragma warning restore CA1031
        }
    }

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
    public bool TrySelect()
    {
        var sip = _element.Patterns.SelectionItem;
        if (!sip.IsSupported)
            return false;

        sip.Pattern.Select();
        return true;
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
    public bool? GetSelectionState()
    {
        var sip = _element.Patterns.SelectionItem;
        if (!sip.IsSupported)
            return null;

        return sip.Pattern.IsSelected.Value;
    }

    /// <inheritdoc/>
    public string? GetSelectedText()
    {
        // Primary path: SelectionPattern.Selection — works for ListBox, ComboBox, etc.
        var sp = _element.Patterns.Selection;
        if (sp.IsSupported)
        {
#pragma warning disable CA1031 // Best-effort; Selection.Value may throw if container is empty
            try
            {
                var selected = sp.Pattern.Selection.Value;
                if (selected is { Length: > 0 })
                    return selected[0].Name;
            }
            catch (Exception)
            {
                // Fall through to next path
            }
#pragma warning restore CA1031
        }

        // Fallback: ValuePattern.Value — works for editable ComboBox controls.
        var vp = _element.Patterns.Value;
        if (vp.IsSupported)
            return vp.Pattern.Value.Value;

        return null;
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
    public bool TryExpand()
    {
        var ecp = _element.Patterns.ExpandCollapse;
        if (!ecp.IsSupported)
            return false;

        ecp.Pattern.Expand();
        return true;
    }

    /// <inheritdoc/>
    public bool? GetExpandCollapseState()
    {
#pragma warning disable CA1031 // Return null if pattern read fails
        try
        {
            var ecp = _element.Patterns.ExpandCollapse;
            if (!ecp.IsSupported)
                return null;

            var state = ecp.Pattern.ExpandCollapseState;
            return state.Value == FlaUI.Core.Definitions.ExpandCollapseState.Expanded;
        }
        catch (Exception)
        {
            return null;
        }
#pragma warning restore CA1031
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

    // ── Screenshot ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Captures the element's window using <c>PrintWindow</c> (Win32 P/Invoke)
    /// with <c>PW_RENDERFULLCONTENT</c> (flag 2), which renders the window into
    /// an off-screen GDI bitmap.  This approach works on Windows Server/CI runners
    /// where GDI <c>BitBlt</c> from screen would capture a blank window because the
    /// session desktop is not composited.  Falls back to an empty byte array when
    /// the native window handle is zero or the bounding rectangle is empty.
    /// </remarks>
    public byte[] CaptureScreenshot()
    {
        // Retrieve the native HWND from the UIA element's NativeWindowHandle property.
        IntPtr hwnd = IntPtr.Zero;
#pragma warning disable CA1031 // Tolerate UIA property read failures
        try
        {
            if (_element.Properties.NativeWindowHandle.TryGetValue(out var rawHandle))
                hwnd = new IntPtr(rawHandle);
        }
        catch
        {
            // Property not available on this element — hwnd stays Zero.
        }
#pragma warning restore CA1031

        // If we didn't get a window handle from the element itself, try the
        // bounding rectangle as a fallback signal (zero rect = off-screen element).
        if (hwnd == IntPtr.Zero)
        {
            var rect = BoundingRectangle;
            if (rect.Width <= 0 || rect.Height <= 0)
                return Array.Empty<byte>();

            // Walk the ancestor chain to find a top-level HWND that encloses
            // this element's bounding rectangle.
            hwnd = FindNearestHwnd(_element);
        }

        if (hwnd == IntPtr.Zero)
            return Array.Empty<byte>();

        return CaptureHwnd(hwnd);
    }

    /// <summary>
    /// Renders the window referenced by <paramref name="hwnd"/> into an off-screen
    /// bitmap using <c>PrintWindow</c> with <c>PW_RENDERFULLCONTENT</c> (0x2), then
    /// encodes the bitmap as PNG and returns the bytes.
    /// </summary>
    private static byte[] CaptureHwnd(IntPtr hwnd)
    {
        // Query the window client-area dimensions.
        if (!NativeMethods.GetClientRect(hwnd, out var clientRect))
            return Array.Empty<byte>();

        int width = clientRect.Right - clientRect.Left;
        int height = clientRect.Bottom - clientRect.Top;

        if (width <= 0 || height <= 0)
            return Array.Empty<byte>();

#pragma warning disable CA1031 // Any GDI failure should produce empty bytes, not crash the test runner
        try
        {
            using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            IntPtr hdc = g.GetHdc();
            try
            {
                // PW_RENDERFULLCONTENT = 0x2  — renders DirectComposition / WinUI content.
                // PW_CLIENTONLY         = 0x1  — captures client area only (no title bar).
                // Combining both (0x3) captures the full client area including layered surfaces.
                const uint PW_CLIENTONLY_AND_RENDERFULL = 0x3;
                NativeMethods.PrintWindow(hwnd, hdc, PW_CLIENTONLY_AND_RENDERFULL);
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }

            using var ms = new System.IO.MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        catch
        {
            return Array.Empty<byte>();
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Walks the UIA ancestor chain looking for an element that reports a non-zero
    /// <c>NativeWindowHandle</c>.  Returns <see cref="IntPtr.Zero"/> when none is found.
    /// </summary>
    private static IntPtr FindNearestHwnd(FlaUI.Core.AutomationElements.AutomationElement element)
    {
#pragma warning disable CA1031
        try
        {
            var current = element;
            for (var depth = 0; depth < 20 && current != null; depth++)
            {
                if (current.Properties.NativeWindowHandle.TryGetValue(out var h) && h != 0)
                    return new IntPtr(h);
                current = current.Parent;
            }
        }
        catch
        {
            // UIA ancestor walk may fail (e.g. element destroyed during traversal).
        }
#pragma warning restore CA1031

        return IntPtr.Zero;
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

/// <summary>
/// Win32 P/Invoke declarations used by <see cref="UiaElementBackend.CaptureScreenshot"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "P/Invoke wrappers; exercised only by E2E tests.")]
internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = false)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);

    [DllImport("user32.dll", SetLastError = false)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetClientRect(IntPtr hwnd, out RECT rect);
}
