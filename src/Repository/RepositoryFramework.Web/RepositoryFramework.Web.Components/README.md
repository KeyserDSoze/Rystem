# Rystem.RepositoryFramework.Web.Components

Auto-generated Blazor Server management UI for every repository registered in the [Repository Framework](https://github.com/KeyserDSoze/Rystem). Provides query, create, edit, and delete views with full customisation: theming, authentication, localisation, custom actions, and property-level UI mapping — all without writing Razor code.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Web.Components
```

Target framework: `net10.0`

---

## Quick start

### 1 — Register the UI (Program.cs)

```csharp
builder.Services
    .AddRepositoryUi(settings =>
    {
        settings.Name  = "My Dashboard";
        settings.Icon  = "dashboard";   // Google Material Icons Outlined name
        settings.Image = "/logo.png";   // optional top-bar image
    })
    .AddDefaultSkinForUi()      // registers "Light" and "Dark" themes
    .AddDefaultLocalization();  // enables culture routing and IStringLocalizer support
```

If you need to include Razor Pages / components from an additional assembly for routing:

```csharp
builder.Services.AddRepositoryUi<MyAssemblyMarker>(settings => { ... });
```

### 2 — Register endpoints (Program.cs)

```csharp
var app = builder.Build();
app.AddDefaultRepositoryEndpoints();
```

This single call registers:
- Static files
- Map fallback to the `_Host` (or `_AuthorizedHost`) Razor Page
- `MapRazorPages()`
- All built-in management endpoints (see [Built-in endpoints](#built-in-endpoints))

### 3 — Add the host page

Create `Pages/_Host.cshtml` (or reuse the one the package provides) and include the two partials plus the root component:

```cshtml
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <partial name="RepositoryStyle" />
</head>
<body>
    <component type="typeof(RepositoryApp)" render-mode="ServerPrerendered" />
    <partial name="RepositoryScript" />
</body>
</html>
```

---

## Authentication

Call `WithAuthenticatedUi()` to protect the dashboard behind cookie authentication. The framework then maps to `_AuthorizedHost.cshtml` instead of `_Host.cshtml` and registers a logout endpoint.

```csharp
builder.Services
    .AddRepositoryUi(settings => { settings.Name = "Admin"; })
    .WithAuthenticatedUi();
```

Logout is handled automatically at `GET /Repository/Identity/Logout`.

---

## Per-repository customisation

After calling `AddRepository<T, TKey, ...>()`, chain any of the extension methods below on the returned `IRepositoryBuilder<T, TKey>`:

| Method | Description |
|--------|-------------|
| `SetDefaultUiRoot<T, TKey>()` | Makes this model the dashboard home / landing entity |
| `DoNotExposeInUi<T, TKey>()` | Hides the model from the sidebar menu entirely |
| `ExposeFor<T, TKey>(int index)` | Controls the sidebar sort order (lower = higher in list) |
| `WithIcon<T, TKey>("icon_name")` | Sets the sidebar icon (Google Material Icons Outlined name, default: `hexagon`) |
| `WithName<T, TKey>("Display Name")` | Overrides the menu label (default: `typeof(T).Name`) |
| `AddAction<T, TKey, TAction>()` | Registers a custom action button on the edit view |
| `MapPropertiesForUi<T, TKey, TUiMapper>()` | Registers a custom property mapper for this model |
| `WithLocalization<T, TKey, TLocalization>()` | Attaches an `IStringLocalizer` to this model's views |

### Example

```csharp
builder.Services
    .AddRepository<Product, Guid, ProductRepository>()
    .WithIcon<Product, Guid>("inventory_2")
    .WithName<Product, Guid>("Products")
    .ExposeFor<Product, Guid>(0)
    .AddAction<Product, Guid, ArchiveProductAction>()
    .MapPropertiesForUi<Product, Guid, ProductUiMapper>();
```

---

## Custom actions (`IRepositoryEditAction<T, TKey>`)

A custom action appears as a button on the entity edit page. Implement the interface and register it with `AddAction<T, TKey, TAction>()`.

```csharp
public sealed class ArchiveProductAction : IRepositoryEditAction<Product, Guid>
{
    private readonly IRepository<Product, Guid> _repository;

    public ArchiveProductAction(IRepository<Product, Guid> repository)
        => _repository = repository;

    public string  Name     => "Archive";
    public string? IconName => "archive";

    public async ValueTask<bool> InvokeAsync(Entity<Product, Guid> entity)
    {
        await _repository.DeleteAsync(entity.Key);
        return true;   // return true to navigate away after the action
    }
}
```

Constructor dependencies are resolved from DI.

---

## Property UI mapping (`IRepositoryUiMapper<T, TKey>`)

Override how individual properties are rendered and validated in the edit form. Implement `IRepositoryUiMapper<T, TKey>` and register it with `MapPropertiesForUi<T, TKey, TUiMapper>()`.

```csharp
public sealed class ProductUiMapper : IRepositoryUiMapper<Product, Guid>
{
    public void Map(IRepositoryPropertyUiHelper<Product, Guid> mapper)
    {
        mapper
            // Set a literal default value for a property
            .MapDefault(x => x.Description, "Enter a description")

            // Set a factory-resolved default (called at render time)
            .MapDefault(x => x.Tags, () => new List<string> { "new" })

            // Use a related entity's key as the default (loads from IRepository)
            .MapDefault(x => x.CategoryId, Guid.Empty)

            // Render the property as a rich-text / HTML editor
            .SetTextEditor(x => x.LongDescription, minHeight: 400)

            // Single-select dropdown backed by an async data source
            .MapChoice(
                x => x.CategoryId,
                async (sp, entity) =>
                {
                    var repo = sp.GetRequiredService<IRepository<Category, Guid>>();
                    var items = new List<LabelValueDropdownItem>();
                    await foreach (var item in repo.QueryAsync())
                        items.Add(new LabelValueDropdownItem
                        {
                            Label = item.Value!.Name,
                            Id    = item.Key.ToString()!,
                            Value = item.Key
                        });
                    return items;
                },
                id => id.ToString())

            // Multi-select dropdown
            .MapChoices(
                x => x.Tags,
                (sp, entity) => Task.FromResult<IEnumerable<LabelValueDropdownItem>>(
                    new[] { "Electronics", "Books", "Clothing" }
                        .Select(t => new LabelValueDropdownItem { Label = t, Value = t, Id = t })),
                x => x);
    }
}
```

### `IRepositoryPropertyUiHelper<T, TKey>` methods

| Method | Description |
|--------|-------------|
| `MapDefault(expr, value)` | Sets a static default value for a property |
| `MapDefault(expr, factory)` | Sets a factory (`Func<TProperty>`) default evaluated at render time |
| `MapDefault(expr, key)` | Loads the default from the repository using a key |
| `SetTextEditor(expr, minHeight)` | Renders the property as a rich-text (HTML) editor |
| `MapChoice(expr, retriever, labelComparer)` | Single-select dropdown backed by an async `LabelValueDropdownItem` list |
| `MapChoices(expr, retriever, labelComparer)` | Multi-select dropdown backed by an async `LabelValueDropdownItem` list |

---

## Theming

### Default themes

```csharp
builder.Services.AddRepositoryUi(settings => { ... })
    .AddDefaultSkinForUi();   // registers "Light" and "Dark" named skins
```

Users can switch themes at runtime via `GET /Repository/Settings/Theme/{themeKey}`. The selection is stored in a 1-year cookie.

### Custom theme

```csharp
builder.Services.AddRepositoryUi(settings => { ... })
    .AddDefaultSkinForUi()
    .AddSkinForUi("Corporate", palette =>
    {
        palette.Primary         = "#003366";
        palette.Secondary       = "#0066cc";
        palette.BackgroundColor = "#f4f6f8";
        palette.Color           = "#1a1a2e";
    });
```

### `AppPalette` properties

| Property | Type | Description |
|----------|------|-------------|
| `Primary` | `string` | Primary brand colour (default Light: `#ff6d41`) |
| `Secondary` | `string` | Secondary accent colour (default Light: `#35a0d7`) |
| `Success` | `string` | Success / positive colour |
| `Info` | `string` | Informational colour |
| `Warning` | `string` | Warning colour |
| `Danger` | `string` | Error / danger colour |
| `Light` | `string` | Light surface colour |
| `Dark` | `string` | Dark surface colour (default Dark bg: `#222`) |
| `Color` | `string` | Default text colour (default Dark: `#e1e1e1`) |
| `BackgroundColor` | `string` | Page background colour |
| `Link` | `LinkPalette` | Link colour settings |
| `Button` | `ButtonPalette` | Button colour overrides |
| `Table` | `TablePalette` | Table row / header colours |

---

## Sizing

Customise typography and layout through `AppSizingSettings` on `AppSettings.Sizing`:

```csharp
builder.Services.AddRepositoryUi(settings =>
{
    settings.Name = "My App";
    settings.Sizing = new AppSizingSettings
    {
        RootFontSize       = "16px",
        BodyFontSize       = "14px",
        NavigationFontSize = "13px",
        ColumnWidth        = "200px"
    };
});
```

### `AppSizingSettings` properties

| Property | Description |
|----------|-------------|
| `BorderWidth` | Border width for UI elements |
| `RootFontSize` | `font-size` on `:root` |
| `BodyFontSize` | `font-size` on `body` |
| `BodyLineHeight` | `line-height` on `body` |
| `IconSize` | Size of Material Icons |
| `ColumnWidth` | Default grid column width |
| `InputFontSize` | `font-size` for form inputs |
| `NavigationFontSize` | Sidebar menu font size |
| `NavigationFontWeight` | Sidebar menu font weight |
| `TextFontFamily` | Global font family |

---

## Localisation

```csharp
builder.Services.AddRepositoryUi(settings => { ... })
    .AddDefaultLocalization();
```

Users change culture via `GET /Repository/Language/{culture}` (stores a `.AspNetCore.Culture` cookie).

To provide model-specific translations register a localiser per repository:

```csharp
builder.Services
    .AddRepository<Product, Guid, ProductRepository>()
    .WithLocalization<Product, Guid, ProductStringLocalizer>();
```

`ProductStringLocalizer` is a standard `IStringLocalizer<Product>` — create the matching `.resx` files and register the class normally.

---

## `AppSettings` reference

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Application name shown in the top bar |
| `Icon` | `string` | Material Icons Outlined name for the app icon |
| `Image` | `string` | URL of an image displayed in the top bar (alternative to icon) |
| `Palette` | `AppPalette` | Active colour palette (set at startup; runtime switching uses named skins) |
| `Sizing` | `AppSizingSettings` | Typography and layout sizing |
| `RazorPagesForRoutingAdditionalAssemblies` | `List<Assembly>` | Extra assemblies scanned for Razor Pages routing |

---

## Built-in endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/Repository/Language/{culture}` | Sets a `.AspNetCore.Culture` cookie and redirects back |
| `GET` | `/Repository/Settings/Theme/{themeKey}` | Activates a named skin (1-year cookie) |
| `GET` | `/Repository/Identity/Logout` | Signs out and redirects (only registered when `WithAuthenticatedUi()` is used) |

Per-entity routes (auto-generated from `typeof(T).Name`):

| Route | Description |
|-------|-------------|
| `/Repository/{ModelName}/Query` | Paginated query / search list view |
| `/Repository/{ModelName}/Create` | Create new entity form |
| `/Repository/{ModelName}/Edit/{key}` | Edit existing entity form |

---

## Full setup example

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRepository<Product, Guid, ProductRepository>()
        .WithIcon<Product, Guid>("inventory_2")
        .WithName<Product, Guid>("Products")
        .ExposeFor<Product, Guid>(0)
        .SetDefaultUiRoot<Product, Guid>()
        .AddAction<Product, Guid, ArchiveProductAction>()
        .MapPropertiesForUi<Product, Guid, ProductUiMapper>()
        .WithLocalization<Product, Guid, ProductStringLocalizer>()
    .Services
    .AddRepository<Category, int, CategoryRepository>()
        .WithIcon<Category, int>("category")
        .WithName<Category, int>("Categories")
        .ExposeFor<Category, int>(1)
    .Services
    .AddRepositoryUi(settings =>
    {
        settings.Name  = "Product Admin";
        settings.Icon  = "store";
    })
    .AddDefaultSkinForUi()
    .AddDefaultLocalization()
    .WithAuthenticatedUi();

var app = builder.Build();
app.AddDefaultRepositoryEndpoints();
app.Run();
```

## Notes

- Use `AddRepositoryUi` (lowercase `Ui`), not `AddRepositoryUI`.
- Combine with `Rystem.RepositoryFramework.Api.Client` or other integrations for repository data sources.
