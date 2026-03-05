### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Web.Components

> ⚠️ **Work in progress.** This library is under active development. APIs may change between releases.

A Blazor component library for ASP.NET Core applications built on top of **Bootstrap 5** and **Google Material Symbols**. Provides layout components, a fluent CSS class builder, UI services, and a typed data table.

## 📦 Installation

```bash
dotnet add package Rystem.Web.Components
```

## Table of Contents

- [Setup](#setup)
- [Layout Components](#layout-components)
- [Button Component](#button-component)
- [Icon Component](#icon-component)
- [Bootstrap CSS Class Builder](#bootstrap-css-class-builder)
- [Services](#services)
- [DataTable](#datatable)
- [Enums Reference](#enums-reference)

---

## Setup

Register the services and optionally add the built-in services:

```csharp
builder.Services
    .AddRystemWeb()
    .WithLoaderService()   // ILoaderService — show/hide a loading indicator
    .WithCopyService();    // ICopyService — copy text to clipboard via JS interop
```

Or register everything at once:

```csharp
builder.Services
    .AddRystemWeb()
    .WithAllServices();
```

Add the namespace import to your `_Imports.razor`:

```razor
@using Rystem.Web.Components
@using Rystem.Web.Components.Customization
@using Rystem.Web.Components.Services
```

---

## Layout Components

### Container

Wraps content in a Bootstrap container. Accepts an optional `Breakpoint` and a `Bootstrap` CSS class builder instance.

```razor
<Container Breakpoint="BreakpointType.Lg">
    <!-- content -->
</Container>
```

### Row

Wraps content in a Bootstrap row. Accepts an optional `RowBuilder` (fluent) or a plain `Class` string.

```razor
<Row>
    <!-- columns -->
</Row>
```

### Col

Wraps content in a Bootstrap column.

```razor
<Col>
    content
</Col>
```

### Wrapper

Generic wrapper component for additional layout nesting.

---

## Button Component

```razor
<Button
    Color="ColorType.Primary"
    Size="SizeType.Medium"
    Outline="false"
    Message="Click me"
    Icon="IconType.Add"
    IconStyle="StyleType.Outlined"
    Click="@HandleClickAsync" />
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Click` | `Func<ValueTask>?` | `null` | Async click callback |
| `Color` | `ColorType` | `Primary` | Bootstrap color variant |
| `Size` | `SizeType` | `Medium` | Button size |
| `Outline` | `bool` | `false` | Use Bootstrap outline style |
| `Message` | `string?` | `null` | Button label text |
| `Icon` | `IconType` | `None` | Material Symbol icon (omitted when `None`) |
| `IconStyle` | `StyleType` | `Outlined` | Icon rendering style |
| `IconSize` | `SizeType` | `Medium` | Icon size |
| `Body` | `RenderFragment?` | `null` | Custom slot content (replaces `Message`) |
| `Disabled` | `bool` | `false` | Prevents the click callback from firing |

---

## Icon Component

Renders a Google Material Symbol icon as a `<span>` with the appropriate CSS class.

```razor
<Icon Value="IconType.Search" Style="StyleType.Rounded" Size="SizeType.Large" />
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Value` | `IconType` | `None` | The icon to render |
| `Style` | `StyleType` | `Outlined` | `Outlined`, `Sharp`, or `Rounded` |
| `Size` | `SizeType` | `Medium` | `Small`, `Medium`, or `Large` |

`IconType` covers all standard Material Symbols (2400+ icons).

---

## Bootstrap CSS Class Builder

A fluent builder that generates Bootstrap 5 CSS class strings in C# without string concatenation.

```csharp
// In a component's @code block
string css = Bootstrap.Style
    .Container
    .Fluid
    .ToString();
// → "container-fluid"

string rowCss = Bootstrap.Style
    .Row
    .ToString();
// → "row"

string colCss = Bootstrap.Style
    .Column
    .ToString();
// → "col"
```

Pass the builder directly to components:

```razor
<Container Class="@Bootstrap.Style.JustifyContent.Center">
    <!-- content -->
</Container>
```

---

## Services

### ILoaderService

Shows and hides a global loading indicator. Subscribe to `OnChange` in a loading overlay component to react to state changes.

```csharp
public interface ILoaderService
{
    bool IsVisible { get; }
    void Show();
    void Hide();
    event Action? OnChange;
}
```

```razor
@inject ILoaderService Loader

<button @onclick="LoadAsync">Load</button>

@code {
    private async Task LoadAsync()
    {
        Loader.Show();
        await FetchDataAsync();
        Loader.Hide();
    }
}
```

### ICopyService

Copies a string to the clipboard via JavaScript interop.

```csharp
public interface ICopyService
{
    ValueTask CopyAsync(string value);
}
```

```razor
@inject ICopyService Copy

<button @onclick="@(() => Copy.CopyAsync("text to copy"))">Copy</button>
```

### IDialogService

Shows a confirmation dialog. The `ok` callback is invoked when the user confirms.

```csharp
public interface IDialogService
{
    void Show(string title, Func<ValueTask> ok, string? message = null);
    void Cancel();
}
```

---

## DataTable

`DataTableSettings<T, TKey>` configures a data table component with server-side or in-memory data.

```csharp
var settings = new DataTableSettings<Product, Guid>
{
    Color      = ColorType.Primary,
    Size       = SizeType.Medium,
    Striped    = true,
    Hover      = true,
    Bordered   = BorderType.Default,
    Responsive = BreakpointType.Md,
    // Option A: pre-loaded dictionary
    Items = products.ToDictionary(p => p.Id),
    // Option B: server-side selector with pagination and filtering
    ItemsSelector = async (pagination, filter) =>
    {
        var page = await _repo.GetPageAsync(pagination.CurrentPage, filter);
        return (page.Items.ToDictionary(p => p.Id), page.TotalCount);
    }
};
```

---

## Enums Reference

| Enum | Values |
|---|---|
| `ColorType` | `Primary`, `Secondary`, `Success`, `Info`, `Warning`, `Danger`, `Light`, `Dark` |
| `SizeType` | `Small`, `Medium`, `Large` |
| `StyleType` | `Outlined`, `Sharp`, `Rounded` |
| `BorderType` | `Default`, `Borderless`, … |
| `BreakpointType` | `None`, `Sm`, `Md`, `Lg`, `Xl`, `Xxl` |
| `IconType` | 2400+ Google Material Symbols (`Search`, `Home`, `Add`, `Delete`, … ) |