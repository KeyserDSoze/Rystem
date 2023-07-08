using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Rystem.Content.Infrastructure.Storage
{
    internal sealed class InMemoryRepository : IContentRepository
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ContentRepositoryDownloadResult>> _all = new();
        private ConcurrentDictionary<string, ContentRepositoryDownloadResult> Files => _all[_name];
        private string _name = string.Empty;
        public ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            if (Files.ContainsKey(path))
            {
                Files.Remove(path, out _);
                return ValueTask.FromResult(true);
            }
            return ValueTask.FromResult(false);
        }

        public Task<ContentRepositoryDownloadResult?> DownloadAsync(string path, ContentInformationType informationRetrieve = ContentInformationType.None, CancellationToken cancellationToken = default)
        {
            if (Files.TryGetValue(path, out var value))
            {
                var content = value;
                return Task.FromResult(content)!;
            }
            return Task.FromResult(default(ContentRepositoryDownloadResult));
        }

        public ValueTask<bool> ExistAsync(string path, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Files.ContainsKey(path));

        public Task<ContentRepositoryResult?> GetPropertiesAsync(string path, ContentInformationType informationRetrieve = ContentInformationType.All, CancellationToken cancellationToken = default)
        {
            if (Files.TryGetValue(path, out var value))
            {
                var content = value;
                return Task.FromResult(new ContentRepositoryResult
                {
                    Options = content.Options,
                    Path = content.Path,
                    Uri = content.Uri
                })!;
            }
            return Task.FromResult(default(ContentRepositoryResult));
        }

        public async IAsyncEnumerable<ContentRepositoryDownloadResult> ListAsync(string? prefix = null, bool downloadContent = false, ContentInformationType informationRetrieve = ContentInformationType.None,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            foreach (var item in Files)
            {
                if (prefix == null || item.Key.StartsWith(prefix))
                {
                    yield return item.Value;
                }
            }
        }

        public void SetName(string name)
        {
            _name = name;
            if (!_all.ContainsKey(name))
            {
                _all.TryAdd(name, new());
            }
        }

        public ValueTask<bool> SetPropertiesAsync(string path, ContentRepositoryOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (Files.TryGetValue(path, out var value))
            {
                value.Options ??= new();
                if (options?.HttpHeaders != null)
                    value.Options.HttpHeaders = options.HttpHeaders;
                if (options?.Metadata != null)
                    value.Options.Metadata = options.Metadata;
                if (options?.Tags != null)
                    value.Options.Tags = options.Tags;

                return ValueTask.FromResult(true);
            }
            return ValueTask.FromResult(false);
        }

        public ValueTask<bool> UploadAsync(string path, byte[] data, ContentRepositoryOptions? options = null, bool overwrite = true, CancellationToken cancellationToken = default)
        {
            if (overwrite || !Files.ContainsKey(path))
            {
                if (!Files.TryGetValue(path, out var downloadResult))
                {
                    downloadResult = new ContentRepositoryDownloadResult();
                    Files.TryAdd(path, downloadResult);
                }
                downloadResult.Path = path;
                downloadResult.Uri = path;
                downloadResult.Data = data;
                downloadResult.Options = options;
                return ValueTask.FromResult(true);
            }
            return ValueTask.FromResult(false);
        }
    }
}
