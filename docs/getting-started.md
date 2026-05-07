# Getting Started

## Prerequisites

**Operating system**

Windows 10 version 1903 or later. UI Automation (UIA3) is a Windows-only technology. Running in a Linux or macOS container will not work; the test runner must execute on a machine with a live desktop session.

**Runtime**

.NET 10.0 SDK or later. Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download).

**FlaUI requirements**

Flawright wraps [FlaUI](https://github.com/FlaUI/FlaUI), which in turn wraps Microsoft's UI Automation (UIA3) APIs. No additional installation is required — FlaUI ships as NuGet packages and is pulled in transitively. However, the target application must:

- Expose its controls via UIA. Most Win32, WinForms, WPF, and WinUI3 applications do. Legacy applications using custom-drawn UI may not.
- Not run at a higher integrity level than the test process. If the application requires elevation (UAC prompt), the test runner must also run as administrator.

**Inspect tool**

Before writing selectors, use an inspection tool to browse the UIA tree of your application:

- **Accessibility Insights for Windows** — [accessibilityinsights.io](https://accessibilityinsights.io/) (recommended)
- **inspect.exe** — ships with the Windows SDK at `C:\Program Files (x86)\Windows Kits\10\bin\<version>\x64\inspect.exe`

These tools show AutomationId, Name, ControlType, and other properties for each element, which map directly to Flawright selectors.

## Installing

Add the package to your test project:

```bash
dotnet add package Flawright
```

Or via the `Directory.Packages.props` / `.csproj` pattern:

```xml
<PackageReference Include="Flawright" />
```

Flawright targets `net10.0-windows`. Your test project must target a Windows TFM (e.g., `net10.0-windows`).

## First Test: Notepad

The following is a complete, runnable xUnit test that launches Notepad, types text, and asserts visibility.

**Project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Flawright" Version="*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
  </ItemGroup>
</Project>
```

**Test file**

```csharp
using JerrettDavis.Flawright;
using Xunit;

public class NotepadTests : IAsyncLifetime
{
    private Flawright? _fw;

    public async Task InitializeAsync()
    {
        _fw = await Flawright.LaunchAsync(new LaunchOptions
        {
            ApplicationPath = "notepad.exe"
        });
    }

    [Fact]
    public async Task TypeText_AppearsInEditor()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.FillAsync("controltype:Edit", "Hello from Flawright!");

        var textBox = await page.Locator("controltype:Edit").FirstAsync();
        var text = await textBox.TextAsync();
        Assert.Equal("Hello from Flawright!", text);
    }

    [Fact]
    public async Task MenuBar_IsVisible()
    {
        var page = await _fw!.Browser.NewPageAsync();

        await page.Locator("controltype:MenuBar").Expect().ToBeVisibleAsync();
    }

    [Fact]
    public async Task Screenshot_ReturnsPngBytes()
    {
        var page = await _fw!.Browser.NewPageAsync();

        var png = await page.ScreenshotAsync();

        Assert.NotNull(png);
        Assert.True(png.Length > 0);
    }

    public async Task DisposeAsync()
    {
        if (_fw != null)
            await _fw.DisposeAsync();
    }
}
```

**Run the tests**

```bash
dotnet test
```

The test runner launches Notepad, drives it, and closes it. Tests run sequentially by default in xUnit. For parallel execution with UI automation, pin each test class to a single thread using `[Collection]` or a custom `ITestCollectionOrderer`.

## What just happened

1. `Flawright.LaunchAsync(options)` is a single static call that starts `notepad.exe` using `Application.AttachOrLaunch` under the hood and wraps it in a `Flawright` instance whose `Browser` is ready to use.
2. `Browser.NewPageAsync()` calls `GetMainWindow` on the FlaUI `Application` and wraps the resulting `Window` in a `FlawrightPage`.
3. `FillAsync` finds the first `ControlType.Edit` element, auto-waiting up to 5 seconds (the default timeout), and sets its value via `ValuePattern.SetValue`.
4. `Expect().ToBeVisibleAsync()` resolves the locator and polls until the element is not offscreen, or throws `FlawrightTimeoutException` after the timeout.
5. `IAsyncLifetime.DisposeAsync()` calls `Flawright.DisposeAsync()`, which closes the Notepad process and releases UIA resources.

## Where to go next

- [Selectors](selectors.md) — learn the full selector grammar
- [Assertions](assertions.md) — reference for all `Expect()` assertions
- [Auto-Waiting](auto-waiting.md) — understand timing and retries
- [Examples](examples.md) — Calculator, File Explorer, installer wizard
- [Troubleshooting](troubleshooting.md) — if something is not working
