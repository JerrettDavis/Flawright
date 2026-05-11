# Flawright.Reqnroll.CalculatorDemo

Demonstrates BDD automation with Flawright and Reqnroll against Windows Calculator.

## Prerequisites

- Windows 10 or later
- .NET 10 or later
- Calculator installed (available by default on Windows)

## Running the Tests

```bash
dotnet test samples/Flawright.Reqnroll.CalculatorDemo
```

**Note**: These tests will be skipped on CI runners (e.g., `windows-2025-vs2026`) where Calculator is not preinstalled. They run automatically on local Windows 11 machines with Calculator installed.

## What It Tests

The feature file (`Features/Calculator.feature`) demonstrates:

- Launching the Calculator application
- Clicking buttons to perform arithmetic operations
- Verifying calculation results
- Interacting with the Calculator UI using Flawright locators

## Selectors

The samples use control-type and automation ID selectors to identify Calculator buttons and the display. See the feature file and step definitions for examples.

## Reference

See the [main Flawright README](../../README.md) for more information about selectors and the Flawright framework.
