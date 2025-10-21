### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## 📚 Resources

- **📖 Complete Documentation**: [https://rystem.net](https://rystem.net)
- **🤖 MCP Server for AI**: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- **💬 Discord Community**: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- **☕ Support the Project**: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Get Started with Localization and Rystem

# Rystem Localization Library

## Key Features

- **Integration-Friendly**: Built specifically for integration with Repository Framework and CQRS (Query side).
- **Dynamic Content**: Easily manage localization strings dynamically via repositories.
- **Extensible and Maintainable**: Define your localization classes clearly and manage them with ease.

---

## Getting Started

### Step 1: Define Your Localization Classes

Create a strongly-typed dictionary for your localized strings:

```csharp
public sealed class TheDictionary
{
    public string Value { get; set; }
    public TheFirstPage TheFirstPage { get; set; }
    public TheSecondPage TheSecondPage { get; set; }
}

public sealed class TheFirstPage
{
    public string Title { get; set; }
    public string Description { get; set; }
}

public sealed class TheSecondPage
{
    public FormattedString Title { get; set; }
}
```

### Step 2: Register Localization with Repository Framework

In your `Program.cs` or `Startup.cs`, register localization:

```csharp
services.AddLocalizationWithRepositoryFramework<TheDictionary>(builder =>
{
    builder.WithInMemory(name: "localization");
},
"localization",
async (serviceProvider) =>
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

### Step 3: Usage in Blazor Components

Use localization in your Blazor components easily:

```razor
@inject IRystemLocalizer<TheDictionary> Localizer

<h2>@Localizer.Instance.Value</h2>
<h3>@Localizer.Instance.TheFirstPage.Title</h3>
<p>@Localizer.Instance.TheFirstPage.Description</p>
<p>@Localizer.Instance.TheSecondPage.Title["Your parameter"]</p>
```

### Step 4: Browser Language Detection in Blazor

Automatically set the culture based on browser language. Use `Routes.razor` as follows:

```razor
@using Microsoft.AspNetCore.WebUtilities

@code {
    private Userone? user;
    private string userId = "1";

    protected override async Task OnInitializedAsync()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("userId", out var userIdValue))
        {
            userId = userId.FirstOrDefault();
        }

        var repository = serviceProvider.GetRequiredService<IRepository<Userone, string>>();
        user = await repository.GetAsync(userId);

        if (user?.Language != null)
        {
            var culture = new CultureInfo(user.Language);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (user?.Language == null)
        {
            var browserLanguage = "en"; // obtain actual browser language here
            user.Language = browserLanguage;
            await repository.UpdateAsync(userId, user);
            NavigationManager.Refresh();
        }
    }
}
```

## How It Works

- **Initialization**: Localization resources are loaded into repositories at startup.
- **Retrieval**: Strings retrieved based on the user's current culture.
- **Fallback**: Defaults to English if a localization string is missing.

## Why Use This Library?

- **Scalable**: Central repository simplifies localization management.
- **Maintainable**: Easy updates to localization without redeployment.
- **Consistent**: Strongly-typed localization for error reduction.

---

## Example with CQRS Framework

```csharp
services.AddLocalizationWithRepositoryFramework<TheDictionary>(builder =>
{
    builder.WithInMemory(name: "localization");
}, "localization");
```

---

## Error Handling

An exception is thrown during startup if no languages are configured:

```shell
Exception: No languages found
```

Ensure at least one localization entry is provided.

---

© 2024 Rystem Localization. All Rights Reserved.