using System.Runtime.CompilerServices;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Content.Infrastructure.Storage
{
    internal sealed class BlobStorageRepository : IContentRepository, IServiceWithFactoryWithOptions<BlobServiceClientWrapper>
    {
        public void SetOptions(BlobServiceClientWrapper options)
        {
            Options = options;
        }
        private BlobContainerClient Client => Options?.ContainerClient ?? throw new ArgumentException($"Client for Blob storage repository and factory {_factoryName} not installed correctly");
        public BlobServiceClientWrapper? Options { get; set; }
        public BlobStorageRepository(BlobServiceClientWrapper? options = null)
        {
            if (options != null)
                Options = options;
        }
        private int? _lenghtOfPrefix;
        public int LengthOfPrefix => _lenghtOfPrefix ??= Options?.Prefix?.Length ?? 0;
        private string GetFileName(string path)
            => $"{Options?.Prefix}{path}";
        public async ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            var response = await Client.DeleteBlobAsync(GetFileName(path), cancellationToken: cancellationToken).NoContext();
            return !response.IsError;
        }

        public async Task<ContentRepositoryDownloadResult?> DownloadAsync(string path, ContentInformationType informationRetrieve = ContentInformationType.None, CancellationToken cancellationToken = default)
        {
            var blobClient = Client.GetBlobClient(GetFileName(path));
            if (await blobClient.ExistsAsync(cancellationToken).NoContext())
            {
                var blobData = await blobClient.DownloadContentAsync(cancellationToken).NoContext();
                return new ContentRepositoryDownloadResult
                {
                    Data = blobData.Value.Content.ToArray(),
                    Path = blobClient.Name,
                    Uri = blobClient.Uri.ToString(),
                    Options = await ReadOptionsAsync(blobClient, informationRetrieve).NoContext()
                };
            }
            return default;
        }
        public async ValueTask<bool> ExistAsync(string path, CancellationToken cancellationToken = default)
        {
            var blobClient = Client.GetBlobClient(GetFileName(path));
            return (await blobClient.ExistsAsync(cancellationToken).NoContext()).Value;
        }
        public async ValueTask<bool> UploadAsync(string path, byte[] data, ContentRepositoryOptions? options = default, bool overwrite = true, CancellationToken cancellationToken = default)
        {
            var blobClient = Client.GetBlobClient(GetFileName(path));
            var uploadOptions = Options?.UploadOptions;
            if (uploadOptions != null)
            {
                if (uploadOptions.Conditions != null && overwrite)
                    uploadOptions.Conditions = null;
            }
            var response = await blobClient.UploadAsync(new BinaryData(data), uploadOptions, cancellationToken).NoContext();
            var result = response.Value != null;
            if (result)
            {
                await SetPropertiesAsync(path, options, cancellationToken).NoContext();
            }
            return result;
        }

        public async IAsyncEnumerable<ContentRepositoryDownloadResult> ListAsync(
            string? prefix = null,
            bool downloadContent = false,
            ContentInformationType informationRetrieve = ContentInformationType.None,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            prefix = $"{Options?.Prefix}{prefix}";
            await foreach (var blob in Client.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var blobClient = Client.GetBlobClient(blob.Name);
                var blobData = Array.Empty<byte>();
                if (downloadContent)
                    blobData = (await blobClient.DownloadContentAsync(cancellationToken).NoContext()).Value.Content.ToArray();
                var options = await ReadOptionsAsync(blobClient, informationRetrieve);
                var path = blob.Name;
                if (Options?.Prefix != null)
                    path = path[LengthOfPrefix..];
                yield return new ContentRepositoryDownloadResult
                {
                    Uri = blobClient.Uri.ToString(),
                    Path = path,
                    Data = blobData,
                    Options = options
                };
            }
        }
        public async Task<ContentRepositoryResult?> GetPropertiesAsync(string path, ContentInformationType informationRetrieve = ContentInformationType.All, CancellationToken cancellationToken = default)
        {
            var blobClient = Client.GetBlobClient(GetFileName(path));
            if (await blobClient.ExistsAsync(cancellationToken).NoContext())
            {
                return new ContentRepositoryResult
                {
                    Path = blobClient.Name,
                    Uri = blobClient.Uri.ToString(),
                    Options = await ReadOptionsAsync(blobClient, informationRetrieve).NoContext()
                };
            }
            return default;
        }
        private static Task<ContentRepositoryOptions> ReadOptionsAsync(BlobClient blobClient, ContentInformationType informationRetrieve)
        {
            if (informationRetrieve == ContentInformationType.None)
                return Task.FromResult(new ContentRepositoryOptions());
            return InternalReadOptionsAsync(blobClient, informationRetrieve);
        }
        private static async Task<ContentRepositoryOptions> InternalReadOptionsAsync(BlobClient blobClient, ContentInformationType informationRetrieve)
        {
            ContentRepositoryHttpHeaders? headers = null;
            Dictionary<string, string>? metadata = null;
            Dictionary<string, string>? tags = null;
            if (informationRetrieve.HasFlag(ContentInformationType.HttpHeaders) || informationRetrieve.HasFlag(ContentInformationType.Metadata))
            {
                BlobProperties properties = await blobClient.GetPropertiesAsync().NoContext();
                headers = new ContentRepositoryHttpHeaders
                {
                    CacheControl = properties.CacheControl,
                    ContentDisposition = properties.ContentDisposition,
                    ContentEncoding = properties.ContentEncoding,
                    ContentHash = properties.ContentHash,
                    ContentLanguage = properties.ContentLanguage,
                    ContentType = properties.ContentType,
                };
                metadata = properties.Metadata.ToDictionary(x => x.Key, x => x.Value);
            }
            if (informationRetrieve.HasFlag(ContentInformationType.Tags))
            {
                var retrieveTags = await blobClient.GetTagsAsync().NoContext();
                tags = retrieveTags.Value.Tags.ToDictionary(x => x.Key, x => x.Value);
            }
            return new ContentRepositoryOptions
            {
                HttpHeaders = headers,
                Metadata = metadata,
                Tags = tags
            };
        }

        public async ValueTask<bool> SetPropertiesAsync(string path, ContentRepositoryOptions? options = null, CancellationToken cancellationToken = default)
        {
            var blobClient = Client.GetBlobClient(GetFileName(path));
            if (options?.HttpHeaders != null)
            {
                await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders
                {
                    CacheControl = options.HttpHeaders.CacheControl,
                    ContentDisposition = options.HttpHeaders.ContentDisposition,
                    ContentEncoding = options.HttpHeaders.ContentEncoding,
                    ContentHash = options.HttpHeaders.ContentHash,
                    ContentLanguage = options.HttpHeaders.ContentLanguage,
                    ContentType = options.HttpHeaders.ContentType,
                }, cancellationToken: cancellationToken).NoContext();
            }
            if (options?.Metadata != null)
            {
                await blobClient.SetMetadataAsync(options.Metadata, cancellationToken: cancellationToken).NoContext();
            }
            if (options?.Tags != null)
            {
                await blobClient.SetTagsAsync(options.Tags, cancellationToken: cancellationToken).NoContext();
            }
            return true;
        }
        private string _factoryName;
        public void SetFactoryName(string name)
        {
            _factoryName = name;
        }
    }
}
