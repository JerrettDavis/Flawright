# Contributing

Thank you for considering contributing to Flawright!

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/<you>/Flawright.git`
3. Create a branch: `git checkout -b feature/my-feature`
4. Make your changes
5. Run tests: `dotnet test`
6. Push and create a Pull Request

## Development Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Windows 10 / Windows 11 (FlaUI requires the Windows UI Automation APIs)
- Visual Studio 2022 or Rider (optional but recommended)

## Building

```bash
dotnet restore
dotnet build
```

## Running Tests

```bash
# Unit tests only (fast, no UI required)
dotnet test tests/Flawright.UnitTests

# E2E tests (requires a GUI session — do not run headless)
dotnet test tests/Flawright.E2ETests
```

## Coding Standards

- Follow the `.editorconfig` rules
- All public APIs must have XML documentation comments
- All changes must include tests where applicable
- Run `dotnet format` before committing

## Commit Messages

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
feat(locators): add ByAutomationId selector
fix(page): handle null window handle on close
docs: update README with usage examples
```

## Pull Request Process

1. Ensure all unit tests pass (`dotnet test tests/Flawright.UnitTests`)
2. Update `CHANGELOG.md` under `[Unreleased]`
3. One feature/fix per PR
4. PRs require at least one review before merging

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
