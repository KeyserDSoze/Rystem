# Rystem.RepositoryFramework.Web.Components

Blazor UI components and endpoint helpers for Repository Framework management dashboards.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Web.Components
```

## Service registration

```csharp
builder.Services
    .AddRepositoryUi(settings =>
    {
        settings.Name = "Repository Dashboard";
    })
    .AddDefaultSkinForUi()
    .AddDefaultLocalization();
```

Optional authenticated UI:

```csharp
builder.Services
    .AddRepositoryUi(settings =>
    {
        settings.Name = "Repository Dashboard";
    })
    .WithAuthenticatedUi();
```

## Endpoint registration

```csharp
var app = builder.Build();
app.AddDefaultRepositoryEndpoints();
```

## Host page integration

```cshtml
<head>
    <partial name="RepositoryStyle" />
</head>
<body>
    <component type="typeof(RepositoryApp)" render-mode="ServerPrerendered" />
    <partial name="RepositoryScript" />
</body>
```

## Notes

- Use `AddRepositoryUi` (lowercase `Ui`), not `AddRepositoryUI`.
- Combine with `Rystem.RepositoryFramework.Api.Client` or other integrations for repository data sources.
