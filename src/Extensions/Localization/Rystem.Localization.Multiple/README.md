### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Localization.Multiple

`Rystem.Localization.Multiple` extends the default ASP.NET Core localization model so multiple assemblies can participate as independent resource sources at the same time.

The package is useful when:

- the host application has its own `.resx` files
- one or more class libraries also ship their own `.resx` files
- you still want to inject the normal `IStringLocalizer<T>` API
- each `T` should resolve against the resources of its own assembly

This package does not replace `IStringLocalizer<T>`. It replaces the factory behind it so localization can be resolved per registered assembly instead of being tied to a single localization setup.

The best source-backed examples come from the sample app in `src/Extensions/Localization/Test/LocalizationApp` and the companion Razor library in `src/Extensions/Localization/Test/Localization.RazorLibrary`.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Installation

Use the current package id from the project metadata:

```bash
dotnet add package Rystem.Extensions.Localization.Multiple
```

The current package metadata in `src/Extensions/Localization/Rystem.Localization.Multiple/Rystem.Localization.Multiple.csproj` is:

- package id: `Rystem.Extensions.Localization.Multiple`
- version: `7.0.0`
- target framework: `net10.0`

The package builds on top of:

- `Microsoft.Extensions.Localization`
- `Microsoft.Extensions.Localization.Abstractions`

## Package Architecture

The package is small and built around four pieces.

| Piece | Purpose |
|---|---|
| `AddMultipleLocalization<T>(...)` | Registers one assembly as a localization source |
| `MultipleLocalizationOptions` | Stores `ResourcesPath` plus the registered assembly name |
| `IMultipleStringLocalizerFactory` | Selects the right `IStringLocalizerFactory` for a requested type |
| `MultipleStringLocalizer<T>` | Transparent `IStringLocalizer<T>` implementation that delegates to the selected factory |

At a high level, the flow is:

- call `AddMultipleLocalization<T>(...)` once for each assembly you want to expose
- the package stores one `MultipleLocalizationOptions` instance per registration
- `IStringLocalizer<TResource>` resolves to `MultipleStringLocalizer<TResource>`
- that wrapper asks `IMultipleStringLocalizerFactory` for the factory matching `TResource.Assembly`
- the selected factory uses the right resource path and assembly when resolving `.resx` entries

## Table of Contents

- [Package Architecture](#package-architecture)
- [The Problem it Solves](#the-problem-it-solves)
- [Registration](#registration)
  - [Register one assembly](#register-one-assembly)
  - [Register multiple assemblies](#register-multiple-assemblies)
  - [ResourcesPath behavior](#resourcespath-behavior)
- [Usage with IStringLocalizer](#usage-with-istringlocalizer)
- [How Resolution Works](#how-resolution-works)
- [Fallback Behavior](#fallback-behavior)
- [Repository Examples](#repository-examples)

---

## The Problem it Solves

The standard localization setup is easy when one application owns all resources.

It becomes awkward when resources are split across assemblies. For example:

- the app has `LocalizationApp.Resources.Shared2.resx`
- a Razor class library has `Localization.RazorLibrary.Resources.Shared.resx`

The sample app demonstrates exactly that setup.

In `src/Extensions/Localization/Test/LocalizationApp/Pages/Index.razor`, both localizers are injected side by side:

```razor
@using Microsoft.Extensions.Localization;
@using Localization.RazorLibrary.Resources;
@inject IStringLocalizer<Shared> Localization
@inject IStringLocalizer<Shared2> Localization2

<h2>@Localization["Ale"]</h2>
<h2>@Localization2["Lisa"]</h2>
```

And the resources really do come from different assemblies:

- `src/Extensions/Localization/Test/Localization.RazorLibrary/Resources/Shared.resx` contains `Ale -> Yes I am`
- `src/Extensions/Localization/Test/LocalizationApp/Resources/Shared2.resx` contains `Lisa -> Yes I am in app`

That is the main capability this package adds.

---

## Registration

### Register one assembly

Register an assembly by passing any anchor type from that assembly.

```csharp
services.AddMultipleLocalization<Shared2>(options =>
{
    options.ResourcesPath = "Resources";
});
```

Internally the package captures:

- `ResourcesPath`
- `typeof(T).Assembly.GetName().Name()` as `FullNameAssembly`

You can also use the parameterless overload:

```csharp
services.AddMultipleLocalization<Shared2>();
```

That overload sets `ResourcesPath` to `string.Empty`.

### Register multiple assemblies

Call the method once per participating assembly.

The sample app does exactly that through two extension methods:

```csharp
builder.Services.AddInAppLocalization();
builder.Services.AddLibraryLocalization();
```

Those map to these registrations:

```csharp
services.AddMultipleLocalization<Shared2>(x =>
{
    x.ResourcesPath = "Resources";
});

services.AddMultipleLocalization<Shared>(x =>
{
    x.ResourcesPath = string.Empty;
});
```

`AddMultipleLocalization<T>(...)` can therefore be repeated safely for different assemblies. The shared singleton factory receives all registered `MultipleLocalizationOptions` instances through `IEnumerable<MultipleLocalizationOptions>`.

### ResourcesPath behavior

`ResourcesPath` works the same way it does in standard ASP.NET Core localization: it points to the folder that contains the `.resx` files inside the registered assembly.

From the current sample:

- the app registers `Shared2` with `ResourcesPath = "Resources"`
- the Razor library registers `Shared` with `ResourcesPath = string.Empty`

The internal resource manager factory converts the path to dotted notation when building the resource prefix.

---

## Usage with IStringLocalizer

After registration, usage remains standard ASP.NET Core localization.

```csharp
using Microsoft.Extensions.Localization;

public sealed class MyService
{
    private readonly IStringLocalizer<Shared2> _localizer;

    public MyService(IStringLocalizer<Shared2> localizer)
    {
        _localizer = localizer;
    }

    public string GetValue() => _localizer["Lisa"];
}
```

Or with multiple assemblies in the same component or service:

```csharp
public sealed class DashboardService
{
    private readonly IStringLocalizer<Shared> _libraryLocalizer;
    private readonly IStringLocalizer<Shared2> _appLocalizer;

    public DashboardService(
        IStringLocalizer<Shared> libraryLocalizer,
        IStringLocalizer<Shared2> appLocalizer)
    {
        _libraryLocalizer = libraryLocalizer;
        _appLocalizer = appLocalizer;
    }

    public string[] GetTexts() =>
    [
        _libraryLocalizer["Ale"],
        _appLocalizer["Lisa"]
    ];
}
```

No custom consumer API is needed. That is the main design goal of the package.

---

## How Resolution Works

`AddMultipleLocalization<T>(...)` registers:

- `IStringLocalizer<>` -> `MultipleStringLocalizer<>` as transient
- `IMultipleStringLocalizerFactory` -> `MultipleStringLocalizerFactory` as singleton
- one singleton `MultipleLocalizationOptions` instance for the registered assembly

`MultipleStringLocalizer<TResourceSource>` inherits from `StringLocalizer<TResourceSource>` and simply passes in the factory selected for `TResourceSource`.

The singleton `MultipleStringLocalizerFactory` builds a dictionary keyed by assembly name:

```csharp
_localizers.Add(options.FullNameAssembly,
    new ResourceManagerStringLocalizerFactory(new MultipleOptions(options), loggerFactory));
```

Then, at resolution time:

```csharp
var assemblyName = type.Assembly.GetName().Name;
if (_localizers.ContainsKey(assemblyName!))
    return _localizers[assemblyName!];
```

So the localizer used for `IStringLocalizer<Shared>` and `IStringLocalizer<Shared2>` is selected according to the assembly of `Shared` and `Shared2`.

---

## Fallback Behavior

There is one important implementation detail to know.

If a type is requested from an assembly that was not explicitly registered, the factory falls back to the first registered localizer factory:

```csharp
return _localizers.First().Value;
```

That means:

- registered assemblies resolve deterministically to their own factory
- unregistered assemblies do not fail immediately
- instead, they reuse the first configured localization source

So in practice, you should register every assembly that you expect to localize explicitly.

---

## Repository Examples

The most useful references for this package are:

- Registration extensions: [src/Extensions/Localization/Rystem.Localization.Multiple/LocalizationServiceCollectionExtensions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Localization/Rystem.Localization.Multiple/LocalizationServiceCollectionExtensions.cs)
- Options model: [src/Extensions/Localization/Rystem.Localization.Multiple/MultipleLocalizationOptions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Localization/Rystem.Localization.Multiple/MultipleLocalizationOptions.cs)
- Factory selector: [src/Extensions/Localization/Rystem.Localization.Multiple/MultipleStringLocalizerFactoryOfT.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Localization/Rystem.Localization.Multiple/MultipleStringLocalizerFactoryOfT.cs)
- Localizer wrapper: [src/Extensions/Localization/Rystem.Localization.Multiple/MultipleStringLocalizerOfT.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Localization/Rystem.Localization.Multiple/MultipleStringLocalizerOfT.cs)
- Sample app registration: [src/Extensions/Localization/Test/LocalizationApp/ServiceCollectionExtensions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Localization/Test/LocalizationApp/ServiceCollectionExtensions.cs)
- Sample library registration: [src/Extensions/Localization/Test/Localization.RazorLibrary/ServiceCollectionExtensions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Localization/Test/Localization.RazorLibrary/ServiceCollectionExtensions.cs)
- Sample app usage: [src/Extensions/Localization/Test/LocalizationApp/Pages/Index.razor](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Localization/Test/LocalizationApp/Pages/Index.razor)
- App resource file: [src/Extensions/Localization/Test/LocalizationApp/Resources/Shared2.resx](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Localization/Test/LocalizationApp/Resources/Shared2.resx)
- Library resource file: [src/Extensions/Localization/Test/Localization.RazorLibrary/Resources/Shared.resx](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Localization/Test/Localization.RazorLibrary/Resources/Shared.resx)

This README stays focused because `Rystem.Localization.Multiple` is a narrow infrastructure package. Its whole purpose is to keep the normal `IStringLocalizer<T>` developer experience while selecting the correct resource factory per assembly.
