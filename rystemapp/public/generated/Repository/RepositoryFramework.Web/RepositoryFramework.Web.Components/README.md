# Rystem.RepositoryFramework.Web.Components

`Rystem.RepositoryFramework.Web.Components` provides a server-side Blazor admin UI on top of Repository Framework registrations.

It gives you generic pages for:

- query/list
- show
- create
- edit
- delete
- theme and language settings

and lets you customize menu labels, icons, form rendering, localization, and edit-page actions per repository.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Web.Components
```

## What this package actually adds

The package registers:

- Razor Pages for the built-in UI area
- package host pages (`/_Host` and `/_AuthorizedHost` inside the package area)
- menu, localization, modal, loading, copy, and Radzen-related services
- per-repository UI metadata driven by Repository Framework registrations

It does not replace normal Blazor Server setup.

You still need:

- `AddServerSideBlazor()`
- `app.MapBlazorHub()`
- your own authentication middleware if you enable authenticated UI

## Minimal setup

This is the working shape used by the sample app.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServerSideBlazor();

builder.Services
    .AddRepositoryUi(settings =>
    {
        settings.Name = "Repository App";
        settings.Icon = "dashboard";
    })
    .AddDefaultSkinForUi();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.AddDefaultRepositoryEndpoints();

app.Run();
```

## Main registration API

### `AddRepositoryUi(...)`

```csharp
builder.Services.AddRepositoryUi(settings =>
{
    settings.Name = "Admin";
    settings.Icon = "savings";
    settings.Image = "/logo.png";
});
```

`AppSettings` includes:

| Property | Notes |
| --- | --- |
| `Name` | Required app name shown in the UI |
| `Icon` | Optional Material icon name |
| `Image` | Optional top-bar image |
| `Palette` | Active palette settings |
| `Sizing` | Sizing and typography settings |
| `RazorPagesForRoutingAdditionalAssemblies` | Extra assemblies for the Blazor router |

### `AddRepositoryUi<T>(...)`

There is also a generic overload intended to add an extra assembly for routing.

Important caveat: the current implementation does not preserve that additional assembly setting correctly, so treat this overload as unreliable for now.

## Endpoint mapping

Use:

```csharp
app.AddDefaultRepositoryEndpoints();
```

This method:

- calls `UseStaticFiles()`
- enables request localization when configured
- maps `/Repository/Language/{culture}`
- maps `/Repository/Settings/Theme/{themeKey}`
- maps `/Repository/Identity/Logout` when authenticated UI is enabled
- maps Razor Pages
- maps fallback to the package host page

It does not map the Blazor hub for you.

## Authentication

Use:

```csharp
builder.Services
    .AddRepositoryUi(settings =>
    {
        settings.Name = "Admin";
    })
    .WithAuthenticatedUi();
```

What this actually does:

- switches fallback from the package `/_Host` page to the package `/_AuthorizedHost` page
- enables the logout endpoint at `/Repository/Identity/Logout`

What it does not do:

- configure ASP.NET authentication
- configure cookies or OpenID Connect
- add `UseAuthentication()` or `UseAuthorization()`

Those are still your responsibility.

## Localization

Enable built-in localization with:

```csharp
builder.Services
    .AddRepositoryUi(settings =>
    {
        settings.Name = "Admin";
    })
    .AddDefaultLocalization();
```

The package then enables language switching through:

- `GET /Repository/Language/{culture}`

Important notes:

- the current supported cultures are hardcoded internally to `en-US`, `es-ES`, `it-IT`, `fr-FR`, and `de-DE`
- the endpoint redirects to the app root, not back to the current page

### Per-repository localization

```csharp
builder.Services
    .AddRepository<AppUser, int>(settings =>
    {
        settings
            .WithLocalization<AppUser, int, IStringLocalizer<SharedResource>>();
    });
```

## Themes and skins

### Default skins

```csharp
builder.Services
    .AddRepositoryUi(settings =>
    {
        settings.Name = "Admin";
    })
    .AddDefaultSkinForUi();
```

This registers:

- `Light`
- `Dark`

### Custom skin

```csharp
builder.Services
    .AddRepositoryUi(settings =>
    {
        settings.Name = "Admin";
    })
    .AddDefaultSkinForUi()
    .AddSkinForUi("Corporate", palette =>
    {
        palette.Primary = "#003366";
        palette.Secondary = "#0066cc";
        palette.BackgroundColor = "#f4f6f8";
        palette.Color = "#1a1a2e";
    });
```

Runtime switching happens through:

- `GET /Repository/Settings/Theme/{themeKey}`

Important note: the theme cookie is written with `Secure = true`, so HTTPS is the safe assumption for real use.

## Per-repository customization

These extensions hang off `IRepositoryBuilder<T, TKey>`.

| Method | Purpose |
| --- | --- |
| `SetDefaultUiRoot<T, TKey>()` | Make this repository the landing page for the package router |
| `DoNotExposeInUi<T, TKey>()` | Hide the repository from the menu |
| `ExposeFor<T, TKey>(index)` | Set menu ordering |
| `WithIcon<T, TKey>(icon)` | Set menu icon |
| `WithName<T, TKey>(name)` | Set menu label |
| `AddAction<T, TKey, TAction>()` | Add a custom edit-page action |
| `MapPropertiesForUi<T, TKey, TUiMapper>()` | Customize form rendering and defaults |
| `WithLocalization<T, TKey, TLocalization>()` | Attach a localizer for this repository |

### Example

```csharp
builder.Services
    .AddRepository<AppUser, int>(settings =>
    {
        settings
            .WithIcon("manage_accounts")
            .WithName("User")
            .ExposeFor(2)
            .SetDefaultUiRoot()
            .MapPropertiesForUi<AppUser, int, AppUserDesignMapper>()
            .WithLocalization<AppUser, int, IStringLocalizer<SharedResource>>();
    });
```

## Custom edit actions

Register actions with `AddAction<T, TKey, TAction>()`.

```csharp
public sealed class ArchiveUserAction : IRepositoryEditAction<AppUser, int>
{
    public string Name => "Archive";
    public string? IconName => "archive";

    public ValueTask<bool> InvokeAsync(Entity<AppUser, int> entity)
    {
        return ValueTask.FromResult(true);
    }
}
```

The action appears on the edit page and dependencies are resolved from DI.

## Property UI mapping

Implement `IRepositoryUiMapper<T, TKey>` and register it with `MapPropertiesForUi<T, TKey, TUiMapper>()`.

This lets you configure:

- default values
- default values loaded from repositories
- single-choice dropdowns
- multi-choice dropdowns
- text editor behavior

## Host pages and runtime requirements

The package ships its own host pages inside the area and maps fallback to them.

The package host includes:

- `<base href="~/" />`
- `HeadOutlet`
- `RepositoryApp`
- `_framework/blazor.server.js`
- the `RepositoryStyle` and `RepositoryScript` partials

So you do not need to create your own `_Host.cshtml` just to use the packaged UI.

## Built-in routes

Repository pages:

- `/Repository/{Name}/Query`
- `/Repository/{Name}/Create`
- `/Repository/{Name}/Edit/{Key}`
- `/Repository/{Name}/Show/{Key}`
- `/Repository/Settings`

Utility endpoints:

- `/Repository/Language/{culture}`
- `/Repository/Settings/Theme/{themeKey}`
- `/Repository/Identity/Logout` when authenticated UI is enabled

If you do not set a default UI root with `SetDefaultUiRoot()`, unmatched/fallback navigation ends on the router's not-found content.

## Important caveats

- `AddRepositoryUi(...)` does not replace `AddServerSideBlazor()` or `MapBlazorHub()`
- `WithAuthenticatedUi()` does not configure auth middleware; it only switches the package host and logout route
- language and theme endpoints redirect to the app root, not the current page
- supported cultures are fixed internally
- `AddRepositoryUi<T>(...)` is intended for additional router assemblies but is not reliable in the current implementation

## When to use this package

Use it when you want:

- a quick back-office UI over Repository Framework registrations
- server-side Blazor CRUD screens without hand-writing each page
- per-repository customization through metadata and DI

If you need a fully custom frontend, this package is better used as a reference or internal admin surface than as the only UI for public-facing workflows.
