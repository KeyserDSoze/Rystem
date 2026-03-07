### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Web.Components

`Rystem.Web.Components` is a Blazor component library centered on Bootstrap-flavored layout helpers, a fluent CSS-class builder, a small set of UI services, and static assets for styles and Material Symbols.

The package is still clearly work-in-progress from the current source layout, so the most useful way to document it is to stay very close to the public code that actually exists today.

It is most useful for:

- lightweight Bootstrap-style Blazor layouts
- reusable button and icon components
- fluent CSS class composition in C#
- clipboard and loader services for interactive UI flows

The strongest example source is the sample app in `src/Extensions/Web/Test/Rystem.Web.Components.Test`.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Installation

```bash
dotnet add package Rystem.Web.Components
```

The current package metadata in `src/Extensions/Web/Rystem.Web.Components/Rystem.Web.Components.csproj` is:

- package id: `Rystem.Web.Components`
- version: `3.1.33`
- target framework: `net10.0`

The package builds on top of:

- `Microsoft.AspNetCore.App`
- `Microsoft.AspNetCore.Components.Web`
- `Rystem`
- `Rystem.Extensions.Localization.Multiple`

## Package Architecture

The current package surface is organized around five areas.

| Area | Purpose |
|---|---|
| `AddRystemWeb()` / `RystemWebBuilder` | DI entry point and service registration builder |
| Layout components | `Container`, `Row`, `Col`, `Wrapper` |
| UI components | `Button`, `Icon` |
| Styling helpers | `Bootstrap` and the related builder types |
| Services and assets | `ILoaderService`, `ICopyService`, plus packaged CSS/JS partials |

There are also table-related support models under `Contents/DataTable`, but in the current public source tree those models are present without a completed public table component.

## Table of Contents

- [Package Architecture](#package-architecture)
- [Setup](#setup)
  - [Service registration](#service-registration)
  - [Imports and static assets](#imports-and-static-assets)
- [Layout Components](#layout-components)
  - [Container](#container)
  - [Row](#row)
  - [Col](#col)
  - [Wrapper](#wrapper)
- [UI Components](#ui-components)
  - [Button](#button)
  - [Icon](#icon)
- [Bootstrap Class Builder](#bootstrap-class-builder)
- [Built-in Services](#built-in-services)
  - [ILoaderService](#iloaderservice)
  - [ICopyService](#icopyservice)
  - [IDialogService](#idialogservice)
- [Table-related Models](#table-related-models)
- [Repository Examples](#repository-examples)

---

## Setup

### Service registration

The DI entry point is:

```csharp
builder.Services.AddRystemWeb();
```

This currently does one thing in the source:

```csharp
services.AddRazorPages();
```

It then returns a `RystemWebBuilder`, which exposes the available service registrations.

Current builder methods are:

```csharp
builder.Services
    .AddRystemWeb()
    .WithLoaderService()
    .WithCopyService();
```

Or all currently implemented services at once:

```csharp
builder.Services
    .AddRystemWeb()
    .WithAllServices();
```

From the current implementation, `WithAllServices()` only includes:

- `WithLoaderService()`
- `WithCopyService()`

### Imports and static assets

The sample project imports the component namespace in `_Imports.razor`:

```razor
@using Rystem.Web.Components
@using Rystem.Web.Components.Customization
```

The package also ships two partials for assets:

- `RystemStyle`
- `RystemScript`

The sample host page includes them like this:

```cshtml
<partial name="RystemStyle" />
...
<partial name="RystemScript" />
```

Those partials add:

- Google Material Symbols font stylesheets
- `_content/Rystem.Web.Components/rystem.css`
- Bootstrap bundle JS from CDN
- `_content/Rystem.Web.Components/rystem.js`

---

## Layout Components

### Container

`Container` renders a `<div>` and combines a Bootstrap container class with an optional custom builder.

```razor
<Container Breakpoint="BreakpointType.Small">
    <Row>
        <Col>content</Col>
    </Row>
</Container>
```

Current public parameters:

- `ChildContent`
- `Class` as `Bootstrap`
- `Breakpoint` as `BreakpointType`

The CSS class is built as:

```csharp
$"{Breakpoint.ToBoostrapBreakpoint("container{0}")} {Class?.ToString()}"
```

### Row

`Row` renders a Bootstrap row container.

```razor
<Row RowBuilder="RowBuilder.Style.Default.S1.Large.S2">
    <Col>left</Col>
    <Col>right</Col>
</Row>
```

Current public parameters:

- `ChildContent`
- `Class` as plain string
- `RowBuilder` as `RowBuilder`

### Col

`Col` renders a Bootstrap column container.

```razor
<Col ColumnBuilder="ColumnBuilder.Style.ExtraExtraLarge.S11.ExtraLarge.S9">
    content
</Col>
```

Current public parameters:

- `ChildContent`
- `Class` as plain string
- `ColumnBuilder` as `ColumnBuilder`

### Wrapper

`Wrapper` is a generic `<div>` wrapper that accepts the top-level `Bootstrap` builder.

```razor
<Wrapper Bootstrap="Bootstrap.Style.Container.Large.And().Column.Small.S7.Build()">
    <div>content</div>
</Wrapper>
```

Unlike the name suggests, the current implementation prefixes the wrapper with `row` as part of `GetCssClass()`.

---

## UI Components

### Button

`Button` renders a Bootstrap-style button with optional body content, text, and icon.

```razor
<Button Message="Search"
        Size="SizeType.Large"
        Icon="IconType.Search"
        IconStyle="StyleType.Rounded"
        IconSize="SizeType.Large"
        Click="@Clicking">
    <Body>
        Search
    </Body>
</Button>
```

Current public parameters:

- `Click` as `Func<ValueTask>?`
- `Color` as `ColorType`
- `Size` as `SizeType`
- `Outline` as `bool`
- `Body` as `RenderFragment?`
- `Message` as `string?`
- `Icon` as `IconType`
- `IconStyle` as `StyleType`
- `IconSize` as `SizeType`
- `Disabled` as `bool`

Two important source-level details:

- `Body` does not replace `Message`; if both are set, both are rendered
- the click callback is invoked and discarded with `_ = Click?.Invoke()`, so the component itself does not await it

### Icon

`Icon` renders a Material Symbol icon inside a `<span>`.

```razor
<Icon Value="IconType.Search" Style="StyleType.Rounded" Size="SizeType.Large" />
```

The visible text is derived from the enum name:

```csharp
@Value.ToString().Trim('_').ToLower()
```

So enum members such as `Expand_More` become the lower-case Material Symbol name used by the font.

The package currently exposes a very large `IconType` enum covering many Material Symbol names.

---

## Bootstrap Class Builder

The `Bootstrap` builder is the package's fluent CSS composition entry point.

```csharp
string css = Bootstrap.Style
    .Container
    .Fluid
    .ToString();
```

Or for row and justify content:

```csharp
string rowCss = Bootstrap.Style.Row.ToString();
string justifyCss = Bootstrap.Style.JustifyContent.Default.End.Build();
```

The sample page shows the builder in real use:

```razor
<Wrapper Bootstrap="Bootstrap.Style.Container.Large.And().Column.Small.S7.And().JustifyContent.Small.End.Build()">
    <div class="@Bootstrap.Style.JustifyContent.Default.End.Build()">ciao</div>
</Wrapper>
```

This builder area is one of the stronger parts of the current package because it is actually exercised by the sample UI.

---

## Built-in Services

### ILoaderService

`ILoaderService` is fully implemented and registered through `WithLoaderService()`.

```csharp
public interface ILoaderService
{
    bool IsVisible { get; }
    void Show();
    void Hide();
    event Action? OnChange;
}
```

Current implementation details:

- the backing service is scoped
- `IsVisible` starts as `true`
- `Show()` and `Hide()` both raise `OnChange`

### ICopyService

`ICopyService` is fully implemented and registered through `WithCopyService()`.

```csharp
public interface ICopyService
{
    ValueTask CopyAsync(string value);
}
```

The current implementation uses:

```csharp
await _jsInterop.InvokeVoidAsync("navigator.clipboard.writeText", value)
```

So it depends on browser clipboard support and Blazor JS interop.

### IDialogService

`IDialogService` exists in the public interfaces:

```csharp
public interface IDialogService
{
    void Show(string title, Func<ValueTask> ok, string? message = null);
    void Cancel();
}
```

But in the current source tree there is no concrete implementation and no `RystemWebBuilder` method that registers it.

So it should be treated as a not-yet-wired public contract rather than a ready-to-use built-in service.

---

## Table-related Models

The package currently exposes table-support models under `Contents/DataTable`, including:

- `DataTableSettings<T, TKey>`
- `PaginationState`
- `FilterWrapper<T>`
- `SearchWrapper<T>`
- `OrderWrapper<T>`

`DataTableSettings<T, TKey>` currently exposes:

```csharp
public sealed class DataTableSettings<T, TKey>
    where TKey : notnull
{
    public string CssClass { get; set; } = string.Empty;
    public Dictionary<TKey, T>? Items { get; set; }
    public Func<PaginationState, FilterWrapper<T>, Task<(Dictionary<TKey, T> Items, int Count)>>? ItemsSelector { get; set; }
    public ColorType Color { get; set; }
    public SizeType Size { get; set; }
    public bool Striped { get; set; }
    public bool Sticky { get; set; }
    public BorderType Bordered { get; set; }
    public BreakpointType Responsive { get; set; }
    public bool Hover { get; set; }
}
```

However, the current public source tree does not include a matching completed table component. So these APIs are best understood as groundwork for a table feature rather than a finished top-level component surface.

---

## Repository Examples

The most useful references for this package are:

- Service registration entry point: [src/Extensions/Web/Rystem.Web.Components/ServiceCollectionExtensions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Web/Rystem.Web.Components/ServiceCollectionExtensions.cs)
- Builder registrations: [src/Extensions/Web/Rystem.Web.Components/Builder/RystemWebBuilder.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Web/Rystem.Web.Components/Builder/RystemWebBuilder.cs)
- `Container`: [src/Extensions/Web/Rystem.Web.Components/Components/Container.razor](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Web/Rystem.Web.Components/Components/Container.razor)
- `Row`: [src/Extensions/Web/Rystem.Web.Components/Components/Row.razor](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Web/Rystem.Web.Components/Components/Row.razor)
- `Col`: [src/Extensions/Web/Rystem.Web.Components/Components/Col.razor](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Web/Rystem.Web.Components/Components/Col.razor)
- `Button`: [src/Extensions/Web/Rystem.Web.Components/Components/Button.razor](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Web/Rystem.Web.Components/Components/Button.razor)
- `Icon`: [src/Extensions/Web/Rystem.Web.Components/Components/Icon.razor](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Web/Rystem.Web.Components/Components/Icon.razor)
- CSS builder entry point: [src/Extensions/Web/Rystem.Web.Components/Customization/Builder/Bootstrap.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Web/Rystem.Web.Components/Customization/Builder/Bootstrap.cs)
- Asset partials: [src/Extensions/Web/Rystem.Web.Components/Pages/Shared/RystemStyle.cshtml](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Web/Rystem.Web.Components/Pages/Shared/RystemStyle.cshtml) and [src/Extensions/Web/Rystem.Web.Components/Pages/Shared/RystemScript.cshtml](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Web/Rystem.Web.Components/Pages/Shared/RystemScript.cshtml)
- Sample app startup: [src/Extensions/Web/Test/Rystem.Web.Components.Test/Program.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Web/Test/Rystem.Web.Components.Test/Program.cs)
- Sample page usage: [src/Extensions/Web/Test/Rystem.Web.Components.Test/Pages/Index.razor](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Web/Test/Rystem.Web.Components.Test/Pages/Index.razor)
- Sample host page: [src/Extensions/Web/Test/Rystem.Web.Components.Test/Pages/_Host.cshtml](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Web/Test/Rystem.Web.Components.Test/Pages/_Host.cshtml)

This README stays intentionally conservative because the package is still evolving. It documents the component and service surface that is visibly present in the current source tree, rather than promising a broader UI framework than the current implementation actually provides.
