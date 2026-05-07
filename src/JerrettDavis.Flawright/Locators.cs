using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace JerrettDavis.Flawright;

/// <summary>
/// Locator for finding elements using Playwright-style selectors.
/// Syntax: text:Foo, #id, name:Title, controltype:Button
/// </summary>
public sealed class FlawrightLocator
{
    private readonly string _selector;
    private readonly AutomationElement _root;
    private readonly UIA3Automation _automation;
    private readonly ConditionFactory _cf;

    public FlawrightLocator(string selector, AutomationElement root, UIA3Automation automation)
    {
        _selector = selector;
        _root = root;
        _automation = automation;
        _cf = new ConditionFactory(automation.PropertyLibrary);
    }

    private (string prefix, string value) ParseSelector()
    {
        var idx = _selector.IndexOf(':');
        if (idx > 0)
        {
            return (_selector[..idx], _selector[(idx + 1)..]);
        }
        return ("text", _selector);
    }

    private ConditionBase GetCondition(string prefix, string value)
    {
        return prefix.ToLowerInvariant() switch
        {
            "text" => _cf.ByName(value, PropertyConditionFlags.None),
            "#" or "automationid" => _cf.ByAutomationId(value, PropertyConditionFlags.None),
            "name" => _cf.ByName(value, PropertyConditionFlags.None),
            "controltype" => _cf.ByControlType(ParseControlType(value)),
            "xpath" => throw new NotSupportedException("XPath locators not yet supported"),
            _ => throw new ArgumentException($"Unknown locator prefix: {prefix}")
        };
    }

    private ControlType ParseControlType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "button" => ControlType.Button,
            "checkbox" => ControlType.CheckBox,
            "combobox" or "dropdown" => ControlType.ComboBox,
            "edit" or "textbox" => ControlType.Edit,
            "list" => ControlType.List,
            "listitem" => ControlType.ListItem,
            "menu" => ControlType.Menu,
            "menubar" => ControlType.MenuBar,
            "menuitem" => ControlType.MenuItem,
            "radiobutton" => ControlType.RadioButton,
            "tab" => ControlType.Tab,
            "tabitem" => ControlType.TabItem,
            "text" => ControlType.Text,
            "window" => ControlType.Window,
            "group" => ControlType.Group,
            "image" => ControlType.Image,
            "link" => ControlType.Hyperlink,
            _ => ControlType.Custom
        };
    }

    public async Task<FlawrightElement> FirstAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var (prefix, value) = ParseSelector();
            var condition = GetCondition(prefix, value);
            var element = _root.FindFirstDescendant(condition);
            if (element == null)
                throw new InvalidOperationException($"Element not found: {_selector}");
            return new FlawrightElement(element, this);
        }, cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var (prefix, value) = ParseSelector();
            var condition = GetCondition(prefix, value);
            var elements = _root.FindAllDescendants(condition);
            return elements.Length;
        }, cancellationToken);
    }

    public async Task<FlawrightElement> NthAsync(int index, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var (prefix, value) = ParseSelector();
            var condition = GetCondition(prefix, value);
            var elements = _root.FindAllDescendants(condition);
            if (index < 0 || index >= elements.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return new FlawrightElement(elements[index], this);
        }, cancellationToken);
    }

    public FlawrightAssertions Expect() => new(this);

    internal AutomationElement Root => _root;
    internal string Selector => _selector;
}

/// <summary>
/// Wrapper around a FlaUI AutomationElement with async actions.
/// </summary>
public sealed class FlawrightElement
{
    private readonly AutomationElement _element;

    internal FlawrightElement(AutomationElement element, FlawrightLocator locator)
    {
        _element = element;
        Locator = locator;
    }

    public FlawrightLocator Locator { get; }

    public Task ClickAsync(CancellationToken cancellationToken = default)
        => Task.Run(() => _element.Click(), cancellationToken);

    public Task DoubleClickAsync(CancellationToken cancellationToken = default)
        => Task.Run(() => _element.DoubleClick(), cancellationToken);

    public Task FillAsync(string text, CancellationToken cancellationToken = default)
        => Task.Run(() =>
        {
            var edit = _element.AsTextBox();
            if (edit != null)
            {
                edit.Text = text;
            }
            else
            {
                throw new InvalidOperationException("Element is not editable");
            }
        }, cancellationToken);

    public Task<string> TextAsync(CancellationToken cancellationToken = default)
        => Task.Run(() => _element.Name ?? string.Empty, cancellationToken);

    public Task<bool> IsVisibleAsync(CancellationToken cancellationToken = default)
        => Task.Run(() => !_element.IsOffscreen, cancellationToken);

    public Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
        => Task.Run(() => _element.IsEnabled, cancellationToken);
}

/// <summary>
/// Assertion helpers for locator-based expect chains.
/// </summary>
public sealed class FlawrightAssertions
{
    private readonly FlawrightLocator _locator;

    internal FlawrightAssertions(FlawrightLocator locator) => _locator = locator;

    public async Task ToBeVisibleAsync(CancellationToken cancellationToken = default)
    {
        var element = await _locator.FirstAsync(cancellationToken);
        var isVisible = await element.IsVisibleAsync(cancellationToken);
        if (!isVisible)
            throw new AssertionException($"Expected element '{_locator.Selector}' to be visible");
    }

    public async Task ToBeHiddenAsync(CancellationToken cancellationToken = default)
    {
        var element = await _locator.FirstAsync(cancellationToken);
        var isVisible = await element.IsVisibleAsync(cancellationToken);
        if (isVisible)
            throw new AssertionException($"Expected element '{_locator.Selector}' to be hidden");
    }

    public async Task ToBeEnabledAsync(CancellationToken cancellationToken = default)
    {
        var element = await _locator.FirstAsync(cancellationToken);
        var isEnabled = await element.IsEnabledAsync(cancellationToken);
        if (!isEnabled)
            throw new AssertionException($"Expected element '{_locator.Selector}' to be enabled");
    }

    public async Task ToBeDisabledAsync(CancellationToken cancellationToken = default)
    {
        var element = await _locator.FirstAsync(cancellationToken);
        var isEnabled = await element.IsEnabledAsync(cancellationToken);
        if (isEnabled)
            throw new AssertionException($"Expected element '{_locator.Selector}' to be disabled");
    }

    public async Task ToHaveTextAsync(string expectedText, CancellationToken cancellationToken = default)
    {
        var element = await _locator.FirstAsync(cancellationToken);
        var actualText = await element.TextAsync(cancellationToken);
        if (actualText != expectedText)
            throw new AssertionException($"Expected text '{expectedText}' but got '{actualText}'");
    }

    public async Task ToHaveCountAsync(int expectedCount, CancellationToken cancellationToken = default)
    {
        var actualCount = await _locator.CountAsync(cancellationToken);
        if (actualCount != expectedCount)
            throw new AssertionException($"Expected {expectedCount} elements but found {actualCount}");
    }
}

public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}