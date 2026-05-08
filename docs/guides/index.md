# Per-App Guides

These guides cover the selector patterns, launch options, and gotchas specific to each major Windows application category. Start with the guide for your app type, then refer to the cross-cutting docs for deeper detail.

## Guides

| Guide | App type | Key notes |
|---|---|---|
| [Win11 Notepad](win11-notepad.md) | WinUI3 packaged app | Tabbed Notepad — editor selector changed in newer builds |
| [Win11 Calculator](win11-calculator.md) | WinUI3 packaged app | Stable AutomationIds for every button and the display |
| [Classic Win32](classic-win32.md) | Native Win32 (MFC, raw API) | ClassName-based selectors; UIA support varies |
| [WinForms](winforms.md) | .NET WinForms | AutomationId from designer Name property |
| [WPF](wpf.md) | .NET WPF | AutomationId from `x:Name` or `AutomationProperties.AutomationId` |
| [WinUI 3](winui3.md) | Project Reunion / modern MSIX | AUMID launch, nested Pane wrappers |
| [UWP / Store apps](uwp-store-apps.md) | Universal Windows Platform | AUMID mandatory; ApplicationFrameHost notes |
| [File Explorer](file-explorer.md) | Windows shell | Virtualized list; address bar patterns |
| [Multi-window apps](multi-window.md) | Any app with dialogs or MDI | `GetAllPagesAsync` / `WaitForPageAsync` patterns |
| [Installer wizards](installer-wizards.md) | MSI, Inno Setup, InstallShield | Step-by-step wizard navigation; UAC notes |
| [Elevated apps](elevated-apps.md) | Admin / UAC-required apps | Integrity-level boundary; known limitation |

## Related docs

- [Selectors](../selectors.md) — full selector grammar
- [Auto-waiting](../auto-waiting.md) — timeout and polling configuration
- [Assertions](../assertions.md) — `Expect()` chain reference
- [Troubleshooting](../troubleshooting.md) — common failure modes
