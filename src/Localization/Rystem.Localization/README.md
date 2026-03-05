# Rystem.Localization

[![Version](https://img.shields.io/nuget/v/Rystem.Localization)](https://www.nuget.org/packages/Rystem.Localization)
[![Downloads](https://img.shields.io/nuget/dt/Rystem.Localization)](https://www.nuget.org/packages/Rystem.Localization)

**Rystem.Localization** provides strongly-typed, repository-backed localization for .NET applications. Instead of resource files or key/value dictionaries, you define a plain C# class for your translation strings and store language variants in any data source supported by `RepositoryFramework`.

Languages are loaded once at startup (warm-up), held in memory as a keyed dictionary, and resolved at runtime by matching `CultureInfo.CurrentUICulture`.

---

## Install

```bash
dotnet add package Rystem.Localization
```

> **Dependencies**: `Rystem.DependencyInjection`, `RepositoryFramework.Abstractions`

---

## Define Your Localization Model

Create a POCO class to hold the text values for one language. Every property is a string (or `FormattedString` for parameterized messages).

```csharp
public class AppDictionary
{
    public string WelcomeMessage { get; set; } = string.Empty;
    public string LogoutLabel { get; set; } = string.Empty;
    public FormattedString ItemCount { get; set; } = default!;
}
```

The repository key for each entry is the two-letter ISO 639-1 language code (e.g. `"en"`, `"it"`, `"fr"`).

---

## Registration

### `AddLocalizationWithRepositoryFramework<T>`

Use this when you need full read/write access to the localization store (e.g. to update translations at runtime).

```csharp
builder.Services.AddLocalizationWithRepositoryFramework<AppDictionary>(
    repositoryBuilder => repositoryBuilder.WithEntityFramework<AppDbContext>(),
    name: null,                   // optional factory name (string or Enum)
    storageWarmup: async sp =>    // optional: seed the store before warm-up loads it
    {
        var repo = sp.GetRequiredService<IRepository<AppDictionary, string>>();
        await repo.InsertAsync("en", new AppDictionary { WelcomeMessage = "Welcome" });
        await repo.InsertAsync("it", new AppDictionary { WelcomeMessage = "Benvenuto" });
    }
);
```

Internally this registers:
- `IRepository<T, string>` via RepositoryFramework
- `ILanguages<T>` (singleton) — holds the loaded dictionary
- `IRepositoryLocalizer<T>` (singleton) — resolves by current culture
- `T` (transient) — direct injection of the resolved instance
- A warm-up that calls `ILanguages<T>.WarmUpAsync` on app start

### `AddLocalizationWithQueryFramework<T>`

Use this for read-only access (e.g. translations stored in a static source).

```csharp
builder.Services.AddLocalizationWithQueryFramework<AppDictionary>(
    queryBuilder => queryBuilder.WithInMemory(builder =>
    {
        builder.PopulateWithData(new[]
        {
            new Entity<AppDictionary, string>(new AppDictionary { WelcomeMessage = "Welcome" }, "en"),
            new Entity<AppDictionary, string>(new AppDictionary { WelcomeMessage = "Benvenuto" }, "it"),
        });
    })
);
```

Registers `IQueryModel<T, string>` (read-only) instead of the full repository, plus the same `ILanguages<T>`, `IRepositoryLocalizer<T>`, and warm-up.

---

## Interfaces

### `IRepositoryLocalizer<T>`

Resolves the translation instance for the current UI culture.

```csharp
public interface IRepositoryLocalizer<T>
{
    T Instance { get; }
}
```

### `ILanguages<T>`

Exposes the full in-memory dictionary across all loaded languages.

```csharp
public interface ILanguages<T>
{
    RystemLocalizationFiles<T> Localizer { get; }
}
```

### `RystemLocalizationFiles<T>`

```csharp
public sealed class RystemLocalizationFiles<T>
{
    public Dictionary<string, T> AllLanguages { get; set; } = [];
}
```

---

## Models

### `FormattedString`

A wrapper around a format string that supports `string.Format`-style parameters via the indexer.

```csharp
public sealed class FormattedString
{
    public required string Value { get; init; }

    // Apply parameters via string.Format
    public string this[params object[] parameters]
        => string.Format(Value, parameters);

    public static implicit operator FormattedString(string formattableString)
        => new() { Value = formattableString };
}
```

**Usage:**

```csharp
public class AppDictionary
{
    public FormattedString CartItems { get; set; } = "You have {0} items in your cart.";
}

// At runtime:
var text = localizer.Instance.CartItems[itemCount];
// → "You have 5 items in your cart."
```

---

## Inject and Use

### Inject the resolved instance directly

```csharp
public class MyService(AppDictionary strings)
{
    public string GetWelcome() => strings.WelcomeMessage;
}
```

### Inject `IRepositoryLocalizer<T>`

```csharp
public class MyService(IRepositoryLocalizer<AppDictionary> localizer)
{
    public string GetWelcome() => localizer.Instance.WelcomeMessage;
}
```

### Inject `ILanguages<T>` for all languages

```csharp
public class TranslationAdmin(ILanguages<AppDictionary> languages)
{
    public IEnumerable<string> LoadedLanguages()
        => languages.Localizer.AllLanguages.Keys;

    public AppDictionary? GetForLanguage(string lang)
        => languages.Localizer.AllLanguages.GetValueOrDefault(lang);
}
```

---

## Language Resolution and Fallback

`RepositoryLocalizer<T>` resolves the language in this order:

1. `CultureInfo.CurrentUICulture.TwoLetterISOLanguageName` — exact match (e.g. `"it"`)
2. `"en"` — English fallback
3. First entry in `AllLanguages` — last-resort fallback

If no languages are loaded after warm-up, an `Exception` is thrown at startup.

---

## Multiple Independent Localizations

You can register multiple dictionaries for the same `T` (or different types) using the `name` parameter, which accepts a `string` or `Enum` via `AnyOf<string?, Enum>?`.

```csharp
public enum LocalizationKey { Main, Admin }

// Main UI translations
builder.Services.AddLocalizationWithQueryFramework<AppDictionary>(
    queryBuilder => queryBuilder.WithInMemory(...),
    name: LocalizationKey.Main
);

// Admin panel translations
builder.Services.AddLocalizationWithQueryFramework<AppDictionary>(
    queryBuilder => queryBuilder.WithInMemory(...),
    name: LocalizationKey.Admin
);
```

Resolve by name using the factory:

```csharp
public class AdminService(IFactory<IRepositoryLocalizer<AppDictionary>> factory)
{
    private readonly IRepositoryLocalizer<AppDictionary> _adminLocalizer
        = factory.Create(LocalizationKey.Admin);
}
```

---

## Access All Languages

```csharp
public class TranslationExport(ILanguages<AppDictionary> languages)
{
    public Dictionary<string, AppDictionary> ExportAll()
        => languages.Localizer.AllLanguages;

    public AppDictionary GetForLanguage(string lang)
        => languages.Localizer.AllLanguages[lang];
}
```

