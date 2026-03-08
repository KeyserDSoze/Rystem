# Rystem.Localization

`Rystem.Localization` is a strongly typed localization layer built on top of `RepositoryFramework`.

It does not use `.resx` files or per-key lookups. Instead, each language stores one full object of type `T`, keyed by language code, and the package resolves the right object at runtime from an in-memory snapshot.

## Installation

```bash
dotnet add package Rystem.Localization
```

## Architecture

The package is small and repository-centric:

1. define a localization model `T`
2. register a repository-backed or query-backed source for `T`
3. warm the app up with `WarmUpAsync()`
4. let `ILanguages<T>` load all language rows into memory
5. resolve the current language through `IRepositoryLocalizer<T>` or direct `T` injection

Runtime lookup uses:

- `CultureInfo.CurrentUICulture.TwoLetterISOLanguageName`
- then `en`
- then the first loaded language

## Define the localization model

A language is one full object graph.

```csharp
public sealed class TheDictionary
{
    public string Value { get; set; } = string.Empty;
    public TheFirstPage TheFirstPage { get; set; } = new();
    public TheSecondPage TheSecondPage { get; set; } = new();
}

public sealed class TheFirstPage
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class TheSecondPage
{
    public FormattedString Title { get; set; } = "Title {0}";
}
```

The repository key is expected to be the language code, typically the two-letter ISO form such as `en` or `it`.

## Registration with Repository Framework

The most grounded path is `AddLocalizationWithRepositoryFramework<T>(...)`.

This is how the sample app wires it in `src/Localization/Tests/Rystem.Localization.Test.App/Repository/ServiceCollectionExtensions.cs`.

```csharp
services.AddLocalizationWithRepositoryFramework<TheDictionary>(builder =>
{
    builder.WithInMemory(name: "localization");
},
name: "localization",
storageWarmup: async serviceProvider =>
{
    var repository = serviceProvider.GetRequiredService<IRepository<TheDictionary, string>>();

    await repository.InsertAsync("it", new TheDictionary
    {
        Value = "Valore",
        TheFirstPage = new TheFirstPage
        {
            Title = "Titolo",
            Description = "Descrizione"
        },
        TheSecondPage = new TheSecondPage
        {
            Title = "Titolo {0}"
        }
    });

    await repository.InsertAsync("en", new TheDictionary
    {
        Value = "Value",
        TheFirstPage = new TheFirstPage
        {
            Title = "Title",
            Description = "Description"
        },
        TheSecondPage = new TheSecondPage
        {
            Title = "Title {0}"
        }
    });
});
```

What this registration adds:

- the underlying `IRepository<T, string>`
- named `ILanguages<T>` as a singleton
- named `IRepositoryLocalizer<T>` as a singleton
- direct transient `T` injection via `localizer.Instance`
- warm-up hooks for optional storage seeding and language-cache loading

## Query-backed registration

The package also exposes:

```csharp
services.AddLocalizationWithQueryFramework<TheDictionary>(builder =>
{
    // configure a query source here
});
```

This API is intended for read-only localization sources.

Important caveat: the current implementation looks inconsistent here. The registration path adds query services, but the warm-up loader still resolves `IFactory<IRepository<T, string>>`. There is no sample app using this path, so treat it as a less-proven option until it is cleaned up.

Also note that, unlike the repository-backed path, the current query-backed registration does not add direct `T` injection.

## Warm-up is required

You need to warm the host up manually.

The sample app does this in `src/Localization/Tests/Rystem.Localization.Test.App/Program.cs`:

```csharp
var app = builder.Build();
await app.Services.WarmUpAsync();
```

Without warm-up, the in-memory language snapshot never loads.

## Culture management is manual

This package does not ship ASP.NET Core request-localization middleware, culture providers, or built-in culture selection.

You are expected to set `CultureInfo.CurrentCulture` and `CultureInfo.CurrentUICulture` yourself.

The sample app uses a custom middleware that reads a `lang` cookie:

```csharp
public sealed class LocalizationMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Cookies.TryGetValue("lang", out var cookieLanguage))
        {
            var language = cookieLanguage;
            CultureInfo.CurrentCulture = new CultureInfo(language ?? "en");
            CultureInfo.CurrentUICulture = new CultureInfo(language ?? "en");
        }

        await next(context);
    }
}
```

## Core abstractions

### `IRepositoryLocalizer<T>`

```csharp
public interface IRepositoryLocalizer<T>
{
    T Instance { get; }
}
```

`Instance` resolves the current language object every time you access it.

### `ILanguages<T>`

```csharp
public interface ILanguages<T>
{
    RystemLocalizationFiles<T> Localizer { get; }
}
```

This exposes the full loaded language snapshot.

### `RystemLocalizationFiles<T>`

```csharp
public sealed class RystemLocalizationFiles<T>
{
    public Dictionary<string, T> AllLanguages { get; set; } = [];
}
```

## `FormattedString`

`FormattedString` is the package's simple formatted-message helper.

```csharp
public sealed class FormattedString
{
    public required string Value { get; init; }
    public string this[params object[] parameters] => string.Format(Value, parameters);
}
```

Usage:

```csharp
var title = localizer.Instance.TheSecondPage.Title["something"];
```

## Consuming localizations

The sample Blazor app injects both the localizer and the direct `T` instance in `src/Localization/Tests/Rystem.Localization.Test.App/Components/_Imports.razor`:

```razor
@inject IRepositoryLocalizer<TheDictionary> Localizer
@inject TheDictionary InstanceOfLocalizer
```

And uses them in `src/Localization/Tests/Rystem.Localization.Test.App/Components/Pages/Home.razor`:

```razor
<h2>@Localizer.Instance.Value</h2>
<h3>@Localizer.Instance.TheFirstPage.Title</h3>
<h5>@Localizer.Instance.TheSecondPage.Title["something"]</h5>

<h2>@InstanceOfLocalizer.Value</h2>
```

### Difference between `IRepositoryLocalizer<T>` and direct `T`

- `IRepositoryLocalizer<T>.Instance` resolves from the current culture at access time
- injected `T` is resolved once when the consuming service or component is created

If culture can change during the lifetime of the consumer, prefer `IRepositoryLocalizer<T>`.

## Fallback behavior

`RepositoryLocalizer<T>` resolves languages in this order:

1. current UI culture's two-letter code
2. `en`
3. the first loaded language

Examples:

- `it-IT` resolves through `it`
- `en-US` resolves through `en`

This also means storing keys like `en-US` is not enough by itself, because the resolver only uses the two-letter code.

## Named localizers

Both registration methods accept `name`, which can be a `string` or `Enum` through `AnyOf<string?, Enum>?`.

That lets you host multiple independent localization sets.

```csharp
public enum LocalizationSet
{
    Main,
    Admin
}

services.AddLocalizationWithRepositoryFramework<TheDictionary>(builder =>
{
    builder.WithInMemory(name: LocalizationSet.Admin);
}, LocalizationSet.Admin);
```

Resolve named localizers through the factory layer:

```csharp
public sealed class AdminService
{
    private readonly IRepositoryLocalizer<TheDictionary> _localizer;

    public AdminService(IFactory<IRepositoryLocalizer<TheDictionary>> factory)
        => _localizer = factory.Create(LocalizationSet.Admin);
}
```

## Important caveats

### This is snapshot caching, not live synchronization

Warm-up loads all languages into memory once. Later repository updates do not automatically refresh `AllLanguages`.

### No built-in ASP.NET localization pipeline

You are responsible for setting the current culture yourself.

### Language keys are effectively two-letter keys

Lookup uses `TwoLetterISOLanguageName`, so the most reliable storage keys are values like `en`, `it`, and `fr`.

### Query-backed registration looks weaker than the repository path

Because the warm-up implementation currently resolves a repository factory, `AddLocalizationWithQueryFramework<T>(...)` should be treated cautiously until the source is aligned.

### Warm-up failures are not ideal

`Languages<T>.WarmUpAsync()` throws when no languages are found, but broader warm-up handling elsewhere in the repo can swallow failures. So do not rely on a clean startup exception as the only safety mechanism.

## Grounded by sample and source files

- `src/Localization/Tests/Rystem.Localization.Test.App/Program.cs`
- `src/Localization/Tests/Rystem.Localization.Test.App/Repository/ServiceCollectionExtensions.cs`
- `src/Localization/Tests/Rystem.Localization.Test.App/Components/_Imports.razor`
- `src/Localization/Tests/Rystem.Localization.Test.App/Components/Pages/Home.razor`
- `src/Localization/Rystem.Localization/ServiceCollectionExtensions/RepositoryFrameworkLocalizationServiceCollectionExtensions.cs`
- `src/Localization/Rystem.Localization/Services/Languages.cs`
- `src/Localization/Rystem.Localization/Services/RepositoryLocalizer.cs`

Use this package when you want repository-backed, strongly typed localized object graphs rather than `.resx` resources or key/value string lookups.
