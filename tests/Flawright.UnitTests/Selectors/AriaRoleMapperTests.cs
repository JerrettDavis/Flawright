using FlaUI.Core.Definitions;
using Flawright.Selectors;
using Xunit;

namespace Flawright.UnitTests.Selectors;

/// <summary>
/// Comprehensive tests for <see cref="AriaRoleMapper"/>.
/// </summary>
public class AriaRoleMapperTests
{
    // ── Data sources ─────────────────────────────────────────────────────────

    /// <summary>
    /// Every mapped (supported) AriaRole paired with its expected ControlType.
    /// </summary>
    public static TheoryData<AriaRole, ControlType> MappedRoles { get; } = new()
    {
        { AriaRole.Alert,            ControlType.StatusBar  },
        { AriaRole.Alertdialog,      ControlType.Window     },
        { AriaRole.Button,           ControlType.Button     },
        { AriaRole.Checkbox,         ControlType.CheckBox   },
        { AriaRole.Columnheader,     ControlType.HeaderItem },
        { AriaRole.Combobox,         ControlType.ComboBox   },
        { AriaRole.Dialog,           ControlType.Window     },
        { AriaRole.Document,         ControlType.Document   },
        { AriaRole.Form,             ControlType.Group      },
        { AriaRole.Generic,          ControlType.Pane       },
        { AriaRole.Grid,             ControlType.Table      },
        { AriaRole.Group,            ControlType.Group      },
        { AriaRole.Heading,          ControlType.Text       },
        { AriaRole.Img,              ControlType.Image      },
        { AriaRole.Link,             ControlType.Hyperlink  },
        { AriaRole.List,             ControlType.List       },
        { AriaRole.Listbox,          ControlType.List       },
        { AriaRole.Listitem,         ControlType.ListItem   },
        { AriaRole.Log,              ControlType.StatusBar  },
        { AriaRole.Menu,             ControlType.Menu       },
        { AriaRole.Menubar,          ControlType.MenuBar    },
        { AriaRole.Menuitem,         ControlType.MenuItem   },
        { AriaRole.Menuitemcheckbox, ControlType.MenuItem   },
        { AriaRole.Menuitemradio,    ControlType.MenuItem   },
        { AriaRole.None,             ControlType.Pane       },
        { AriaRole.Option,           ControlType.ListItem   },
        { AriaRole.Presentation,     ControlType.Pane       },
        { AriaRole.Progressbar,      ControlType.ProgressBar},
        { AriaRole.Radio,            ControlType.RadioButton},
        { AriaRole.Radiogroup,       ControlType.Group      },
        { AriaRole.Rowheader,        ControlType.HeaderItem },
        { AriaRole.Scrollbar,        ControlType.ScrollBar  },
        { AriaRole.Searchbox,        ControlType.Edit       },
        { AriaRole.Separator,        ControlType.Separator  },
        { AriaRole.Slider,           ControlType.Slider     },
        { AriaRole.Spinbutton,       ControlType.Spinner    },
        { AriaRole.Status,           ControlType.StatusBar  },
        { AriaRole.Switch,           ControlType.CheckBox   },
        { AriaRole.Tab,              ControlType.TabItem    },
        { AriaRole.Table,            ControlType.Table      },
        { AriaRole.Tablist,          ControlType.Tab        },
        { AriaRole.Tabpanel,         ControlType.Pane       },
        { AriaRole.Textbox,          ControlType.Edit       },
        { AriaRole.Toolbar,          ControlType.ToolBar    },
        { AriaRole.Tooltip,          ControlType.ToolTip    },
        { AriaRole.Tree,             ControlType.Tree       },
        { AriaRole.Treeitem,         ControlType.TreeItem   },
    };

    /// <summary>
    /// Every unsupported (web-only) AriaRole.
    /// </summary>
    public static TheoryData<AriaRole> UnsupportedRoles { get; } = new()
    {
        AriaRole.Application,
        AriaRole.Article,
        AriaRole.Banner,
        AriaRole.Blockquote,
        AriaRole.Caption,
        AriaRole.Cell,
        AriaRole.Code,
        AriaRole.Complementary,
        AriaRole.Contentinfo,
        AriaRole.Definition,
        AriaRole.Deletion,
        AriaRole.Directory,
        AriaRole.Emphasis,
        AriaRole.Feed,
        AriaRole.Figure,
        AriaRole.Gridcell,
        AriaRole.Insertion,
        AriaRole.Main,
        AriaRole.Marquee,
        AriaRole.Math,
        AriaRole.Meter,
        AriaRole.Navigation,
        AriaRole.Note,
        AriaRole.Paragraph,
        AriaRole.Region,
        AriaRole.Row,
        AriaRole.Rowgroup,
        AriaRole.Search,
        AriaRole.Strong,
        AriaRole.Subscript,
        AriaRole.Superscript,
        AriaRole.Term,
        AriaRole.Time,
        AriaRole.Timer,
        AriaRole.Treegrid,
    };

    // ── TryMap: supported roles ───────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(MappedRoles))]
    public void TryMap_ReturnsTrueAndCorrectControlType_ForSupportedRole(
        AriaRole role, ControlType expectedControlType)
    {
        var result = AriaRoleMapper.TryMap(role, out var actual);

        Assert.True(result);
        Assert.Equal(expectedControlType, actual);
    }

    // ── TryMap: unsupported roles ─────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(UnsupportedRoles))]
    public void TryMap_ReturnsFalse_ForUnsupportedRole(AriaRole role)
    {
        var result = AriaRoleMapper.TryMap(role, out _);

        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(UnsupportedRoles))]
    public void TryMap_DoesNotThrow_ForUnsupportedRole(AriaRole role)
    {
        // TryMap must never throw, not even for web-only roles
        var exception = Record.Exception(() => AriaRoleMapper.TryMap(role, out _));

        Assert.Null(exception);
    }

    // ── Map: supported roles ──────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(MappedRoles))]
    public void Map_ReturnsCorrectControlType_ForSupportedRole(
        AriaRole role, ControlType expectedControlType)
    {
        var actual = AriaRoleMapper.Map(role);

        Assert.Equal(expectedControlType, actual);
    }

    // ── Map: unsupported roles ────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(UnsupportedRoles))]
    public void Map_ThrowsNotSupportedException_ForUnsupportedRole(AriaRole role)
    {
        Assert.Throws<NotSupportedException>(() => AriaRoleMapper.Map(role));
    }

    [Theory]
    [MemberData(nameof(UnsupportedRoles))]
    public void Map_ExceptionMessage_ContainsRoleName(AriaRole role)
    {
        var ex = Assert.Throws<NotSupportedException>(() => AriaRoleMapper.Map(role));

        Assert.Contains(role.ToString(), ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(UnsupportedRoles))]
    public void Map_ThrowsNotSupportedException_NotADerivedType(AriaRole role)
    {
        // Must be exactly NotSupportedException, not ArgumentException or other
        var ex = Record.Exception(() => AriaRoleMapper.Map(role));

        Assert.IsType<NotSupportedException>(ex);
    }

    // ── Private helpers for exhaustiveness checks ─────────────────────────────

    private static readonly HashSet<AriaRole> MappedRoleSet = new()
    {
        AriaRole.Alert,
        AriaRole.Alertdialog,
        AriaRole.Button,
        AriaRole.Checkbox,
        AriaRole.Columnheader,
        AriaRole.Combobox,
        AriaRole.Dialog,
        AriaRole.Document,
        AriaRole.Form,
        AriaRole.Generic,
        AriaRole.Grid,
        AriaRole.Group,
        AriaRole.Heading,
        AriaRole.Img,
        AriaRole.Link,
        AriaRole.List,
        AriaRole.Listbox,
        AriaRole.Listitem,
        AriaRole.Log,
        AriaRole.Menu,
        AriaRole.Menubar,
        AriaRole.Menuitem,
        AriaRole.Menuitemcheckbox,
        AriaRole.Menuitemradio,
        AriaRole.None,
        AriaRole.Option,
        AriaRole.Presentation,
        AriaRole.Progressbar,
        AriaRole.Radio,
        AriaRole.Radiogroup,
        AriaRole.Rowheader,
        AriaRole.Scrollbar,
        AriaRole.Searchbox,
        AriaRole.Separator,
        AriaRole.Slider,
        AriaRole.Spinbutton,
        AriaRole.Status,
        AriaRole.Switch,
        AriaRole.Tab,
        AriaRole.Table,
        AriaRole.Tablist,
        AriaRole.Tabpanel,
        AriaRole.Textbox,
        AriaRole.Toolbar,
        AriaRole.Tooltip,
        AriaRole.Tree,
        AriaRole.Treeitem,
    };

    private static readonly HashSet<AriaRole> UnsupportedRoleSet = new()
    {
        AriaRole.Application,
        AriaRole.Article,
        AriaRole.Banner,
        AriaRole.Blockquote,
        AriaRole.Caption,
        AriaRole.Cell,
        AriaRole.Code,
        AriaRole.Complementary,
        AriaRole.Contentinfo,
        AriaRole.Definition,
        AriaRole.Deletion,
        AriaRole.Directory,
        AriaRole.Emphasis,
        AriaRole.Feed,
        AriaRole.Figure,
        AriaRole.Gridcell,
        AriaRole.Insertion,
        AriaRole.Main,
        AriaRole.Marquee,
        AriaRole.Math,
        AriaRole.Meter,
        AriaRole.Navigation,
        AriaRole.Note,
        AriaRole.Paragraph,
        AriaRole.Region,
        AriaRole.Row,
        AriaRole.Rowgroup,
        AriaRole.Search,
        AriaRole.Strong,
        AriaRole.Subscript,
        AriaRole.Superscript,
        AriaRole.Term,
        AriaRole.Time,
        AriaRole.Timer,
        AriaRole.Treegrid,
    };

    // ── Exhaustiveness ────────────────────────────────────────────────────────

    [Fact]
    public void AllAriaRoleValues_AreHandled_ByEitherMappedOrUnsupported()
    {
        // Every value of the AriaRole enum must appear in exactly one of the two
        // datasets; none may silently fall through to an incorrect default.
        var allRoles = Enum.GetValues<AriaRole>();
        var mappedSet = MappedRoleSet;
        var unsupportedSet = UnsupportedRoleSet;

        var unaccounted = allRoles
            .Where(r => !mappedSet.Contains(r) && !unsupportedSet.Contains(r))
            .ToList();

        Assert.Empty(unaccounted);
    }

    [Fact]
    public void MappedAndUnsupportedSets_AreDisjoint()
    {
        var overlap = MappedRoleSet.Intersect(UnsupportedRoleSet).ToList();

        Assert.Empty(overlap);
    }

    [Fact]
    public void TryMap_NeverThrows_ForAnyEnumValue()
    {
        // Regression guard: ensure no code path in TryMap throws unexpectedly
        foreach (var role in Enum.GetValues<AriaRole>())
        {
            var ex = Record.Exception(() => AriaRoleMapper.TryMap(role, out _));
            Assert.Null(ex);
        }
    }

    [Fact]
    public void TryMap_AndMap_AreConsistent_ForAllSupportedRoles()
    {
        // For every role where TryMap returns true, Map must return the same value
        foreach (var role in Enum.GetValues<AriaRole>())
        {
            if (AriaRoleMapper.TryMap(role, out var fromTryMap))
            {
                var fromMap = AriaRoleMapper.Map(role);
                Assert.Equal(fromMap, fromTryMap);
            }
        }
    }
}
