### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Localization.Multiple

Extends ASP.NET Core localization to support **multiple resource assemblies** simultaneously. The standard `IStringLocalizer<T>` implementation only looks up resources in a single configured assembly; this library lets each registered source resolve to its own assembly — enabling shared libraries to ship their own string resources and be used side-by-side.

## 📦 Installation

```bash
dotnet add package Rystem.Localization.Multiple
```

## Table of Contents

- [Rystem.Localization.Multiple](#rystemlocalizationmultiple)
- [📦 Installation](#-installation)
- [Table of Contents](#table-of-contents)
- [The Problem it Solves](#the-problem-it-solves)
- [Setup](#setup)
- [MultipleLocalizationOptions](#multiplelocalizationoptions)
- [Usage with IStringLocalizer](#usage-with-istringlocalizer)
- [How it Works](#how-it-works)

---

## The Problem it Solves

Standard `services.AddLocalization()` ties all `IStringLocalizer<T>` lookups to a single resource path in a single assembly. When you have two class libraries — each with their own `.resx` files — the standard setup cannot distinguish between them.

`AddMultipleLocalization<T>` maps the anchor type `T` to its containing assembly, so `IStringLocalizer<AnyTypeInThatAssembly>` always resolves against the correct `.resx` files, even when multiple libraries are registered.

---

## Setup

Call `AddMultipleLocalization<T>` once per library, where `T` is any type from that library (typically a shared marker class or an existing model). The assembly of `T` becomes the resource assembly for all `IStringLocalizer<>` injections whose type argument lives in the same assembly.

```csharp
// Register resources from Library 1
services.AddMultipleLocalization<MarkerFromLibrary1>(options =>
{
    options.ResourcesPath = "Resources"; // folder inside Library 1
});

// Register resources from Library 2
services.AddMultipleLocalization<MarkerFromLibrary2>(options =>
{
    options.ResourcesPath = "Resources"; // folder inside Library 2
});
```

You can also call the overload without options — `ResourcesPath` defaults to the assembly root:

```csharp
services.AddMultipleLocalization<MarkerFromLibrary1>();
```

---

## MultipleLocalizationOptions

Extends the standard `LocalizationOptions` with one additional property set automatically:

| Property | Type | Description |
|---|---|---|
| `ResourcesPath` | `string` | Sub-folder within the assembly where `.resx` files are located (inherited from `LocalizationOptions`) |
| `FullNameAssembly` | `string` | Set automatically from the assembly of `T`; do not set manually |

---

## Usage with IStringLocalizer

After setup, inject `IStringLocalizer<T>` as usual. The library routes each request to the assembly that was registered for `T`'s assembly:

```csharp
// Resolves strings from Library 1's Resources folder
public class MyService1
{
    private readonly IStringLocalizer<SomeClassInLibrary1> _localizer;

    public MyService1(IStringLocalizer<SomeClassInLibrary1> localizer)
        => _localizer = localizer;

    public string GetGreeting() => _localizer["Hello"];
}

// Resolves strings from Library 2's Resources folder
public class MyService2
{
    private readonly IStringLocalizer<SomeClassInLibrary2> _localizer;

    public MyService2(IStringLocalizer<SomeClassInLibrary2> localizer)
        => _localizer = localizer;

    public string GetGreeting() => _localizer["Hello"];
}
```

Both services inject `IStringLocalizer<T>` and receive independent localizations from their respective assemblies — even if both define the same resource key with different translations.

---

## How it Works

Internally, `AddMultipleLocalization<T>` registers a singleton `IMultipleStringLocalizerFactory` that keeps a map of assembly name → `IStringLocalizerFactory`. The custom `MultipleStringLocalizer<T>` asks the factory for the correct `IStringLocalizerFactory` based on the assembly of `T`, then delegates all lookups to it. This is fully transparent — all standard `IStringLocalizer<T>` features (culture switching, fallback, named lookups) work unchanged.
