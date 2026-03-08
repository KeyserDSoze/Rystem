# Rystem.Content.Infrastructure.InMemory

This provider adds an in-memory content store for tests, local development, and simple non-persistent scenarios.

It is the lightest provider in the Content area: a singleton `ConcurrentDictionary<string, ContentRepositoryDownloadResult>` keyed by path.

## Installation

```bash
dotnet add package Rystem.Content.Infrastructure.InMemory
```

## Registration

```csharp
services
    .AddContentRepository()
    .WithInMemoryIntegration("inmemory");
```

Unlike the other built-in providers, this one registers with `ServiceLifetime.Singleton`.

## Example

```csharp
public sealed class InMemoryContentService
{
    private readonly IContentRepository _repository;

    public InMemoryContentService(IFactory<IContentRepository> factory)
        => _repository = factory.Create("inmemory");

    public async Task RoundTripAsync()
    {
        await _repository.UploadAsync("folder/file.txt", System.Text.Encoding.UTF8.GetBytes("hello"), new ContentRepositoryOptions
        {
            HttpHeaders = new ContentRepositoryHttpHeaders
            {
                ContentType = "text/plain"
            },
            Metadata = new Dictionary<string, string>
            {
                ["author"] = "test"
            },
            Tags = new Dictionary<string, string>
            {
                ["version"] = "1"
            }
        });

        var file = await _repository.DownloadAsync("folder/file.txt", ContentInformationType.All);
        var props = await _repository.GetPropertiesAsync("folder/file.txt", ContentInformationType.All);
        await _repository.DeleteAsync("folder/file.txt");
    }
}
```

## Provider behavior

The runtime is straightforward:

- `UploadAsync` stores `Path`, `Uri`, `Data`, and `Options` directly in memory
- `DeleteAsync` removes the item from the dictionary
- `ExistAsync` checks the dictionary key
- `SetPropertiesAsync` mutates the stored options object

This makes the provider very convenient for tests and also means it is not a fidelity model for remote storage services.

## Important caveats

### `downloadContent` is ignored

`ListAsync(...)` always yields the stored `ContentRepositoryDownloadResult`, so data is already present even when `downloadContent == false`.

### `informationRetrieve` is mostly ignored

`DownloadAsync(...)` and `GetPropertiesAsync(...)` return the stored object or stored options directly. They do not selectively materialize headers, metadata, or tags based on the requested flags.

### `Uri` is just the path

The provider stores:

- `Path = path`
- `Uri = path`

So `Uri` is only a placeholder string, not a real network URL.

### State lives for the process lifetime

Because the provider is a singleton, content persists until you remove it or rebuild the host.

## When to use this provider

Use it when you want:

- fast unit or integration tests
- no external infrastructure
- full round-trip of headers, metadata, and tags in memory

Do not treat it as a perfect simulator for Blob, File Share, or SharePoint behavior.
