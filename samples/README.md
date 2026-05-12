# Flawright Samples

End-to-end demos showcasing Flawright's API on real Windows apps. Each demo is a runnable Reqnroll/xUnit project — `dotnet test` from its directory will execute the scenarios.

Demos skip gracefully when their target app isn't installed (e.g., on Windows Server CI runners) — see each project's README for prerequisites and CI behavior.

| Demo | What it shows |
|------|---------------|
| [NotepadDemo](Flawright.Reqnroll.NotepadDemo/README.md) | Text input, fill, type, clear, and value-pattern verification in classic Notepad. |
| [CalculatorDemo](Flawright.Reqnroll.CalculatorDemo/README.md) | Launching a Windows Store app via AUMID and clicking buttons by name. |
| [NotepadMenuDemo](Flawright.Reqnroll.NotepadMenuDemo/README.md) | File/Edit menu navigation and the unsaved-changes message-box dialog flow. |
| [PaintDrawDemo](Flawright.Reqnroll.PaintDrawDemo/README.md) | Click-and-drag on the mspaint canvas using `page.Mouse` primitives + `DragToAsync`. |
| [SettingsDemo](Flawright.Reqnroll.SettingsDemo/README.md) | Windows Settings app navigation, search, and Back-button traversal. |
| [ExplorerDemo](Flawright.Reqnroll.ExplorerDemo/README.md) | File Explorer launch, address-bar (`Alt+D`) navigation, column sort, and search. |
| [QuickSettingsDemo](Flawright.Reqnroll.QuickSettingsDemo/README.md) | Wi-Fi / Quick Settings system flyout via `Win+A`, attaching to ShellExperienceHost. |
