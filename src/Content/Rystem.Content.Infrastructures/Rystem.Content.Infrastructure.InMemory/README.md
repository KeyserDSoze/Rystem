# Rystem.Content.Infrastructure.InMemory

[![NuGet](https://img.shields.io/nuget/v/Rystem.Content.Infrastructure.InMemory)](https://www.nuget.org/packages/Rystem.Content.Infrastructure.InMemory)

In-memory backend for [Rystem Content Framework](../Rystem.Content.Abstractions). Stores all file data in a thread-safe in-process dictionary. No external dependencies — perfect for unit tests, integration tests, and local development.

## Installation

```bash
dotnet add package Rystem.Content.Infrastructure.InMemory
```

---

## Registration

```csharp
services
    .AddContentRepository()
    .WithInMemoryIntegration("inmemory");  // name is optional; omit if single backend
```

The backend is registered as a **singleton** — state is shared for the lifetime of the application/test host.

---

## Usage

```csharp
public sealed class FileServiceTests
{
    private readonly IContentRepository _repo;

    public FileServiceTests(IContentRepositoryFactory factory)
        => _repo = factory.Create("inmemory");

    public async Task RoundtripAsync()
    {
        var data        = System.Text.Encoding.UTF8.GetBytes("hello world");
        var contentType = "text/plain";
        var metadata    = new Dictionary<string, string> { { "author", "test" } };

        // upload
        var ok = await _repo.UploadAsync("folder/file.txt", data, new ContentRepositoryOptions
        {
            HttpHeaders = new ContentRepositoryHttpHeaders { ContentType = contentType },
            Metadata    = metadata
        });
        // ok == true

        // exist
        var exists = await _repo.ExistAsync("folder/file.txt");  // true

        // properties
        var props = await _repo.GetPropertiesAsync("folder/file.txt", ContentInformationType.All);
        // props.Options.HttpHeaders.ContentType == "text/plain"
        // props.Options.Metadata["author"]      == "test"

        // download
        var downloaded = await _repo.DownloadAsync("folder/file.txt");
        // downloaded.Data == data

        // set properties
        await _repo.SetPropertiesAsync("folder/file.txt", new ContentRepositoryOptions
        {
            Metadata = new Dictionary<string, string> { { "author", "test" }, { "revised", "yes" } }
        });

        // delete
        await _repo.DeleteAsync("folder/file.txt");
        exists = await _repo.ExistAsync("folder/file.txt");  // false
    }
}
```

---

## Notes

- **Singleton lifetime**: data persists for the whole process lifetime. Between test cases use `DeleteAsync` or create a fresh host.
- **No I/O**: all operations are synchronous under the hood — `await` resolves immediately.
- **Full API support**: unlike real storage backends, `Tags`, `Metadata`, and all `ContentInformationType` flags are fully supported in memory.
- **Thread-safety**: the store uses a `ConcurrentDictionary` and is safe for parallel tests.

    services
    .AddContentRepository()
    .WithInMemoryIntegration("inmemory");

### How to use in a business class

    public class AllStorageTest
    {
        private readonly IContentRepositoryFactory _contentRepositoryFactory;
        private readonly Utility _utility;
        public AllStorageTest(IContentRepositoryFactory contentRepositoryFactory, Utility utility)
        {
            _contentRepositoryFactory = contentRepositoryFactory;
            _utility = utility;
        }
        
        public async Task ExecuteAsync()
        {
            var _contentRepository = _contentRepositoryFactory.Create("inmemory");
            var file = await _utility.GetFileAsync();
            var name = "file.png";
            var contentType = "images/png";
            var metadata = new Dictionary<string, string>()
            {
                { "name", "ale" }
            };
            var tags = new Dictionary<string, string>()
            {
                { "version", "1" }
            };
            var response = await _contentRepository.ExistAsync(name).NoContext();
            if (response)
            {
                await _contentRepository.DeleteAsync(name).NoContext();
                response = await _contentRepository.ExistAsync(name).NoContext();
            }
            Assert.False(response);
            response = await _contentRepository.UploadAsync(name, file.ToArray(), new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata,
                Tags = tags
            }, true).NoContext();
            Assert.True(response);
            response = await _contentRepository.ExistAsync(name).NoContext();
            Assert.True(response);
            var options = await _contentRepository.GetPropertiesAsync(name, ContentInformationType.All).NoContext();
            Assert.NotNull(options.Uri);
            foreach (var x in metadata)
            {
                Assert.Equal(x.Value, options.Options.Metadata[x.Key]);
            }
            foreach (var x in tags)
            {
                Assert.Equal(x.Value, options.Options.Tags[x.Key]);
            }
            Assert.Equal(contentType, options.Options.HttpHeaders.ContentType);
            metadata.Add("ale2", "single");
            response = await _contentRepository.SetPropertiesAsync(name, new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata,
                Tags = tags
            }).NoContext();
            Assert.True(response);
            options = await _contentRepository.GetPropertiesAsync(name, ContentInformationType.All).NoContext();
            Assert.Equal("single", options.Options.Metadata["ale2"]);
            response = await _contentRepository.DeleteAsync(name).NoContext();
            Assert.True(response);
            response = await _contentRepository.ExistAsync(name).NoContext();
            Assert.False(response);
        }
    }
