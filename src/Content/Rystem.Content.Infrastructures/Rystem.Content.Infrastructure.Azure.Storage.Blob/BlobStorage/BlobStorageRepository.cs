﻿using System.Runtime.CompilerServices;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Rystem.Content;

namespace Rystem.Content.Infrastructure.Storage
{
    internal sealed class BlobStorageRepository : IContentRepository
    {
        private BlobServiceClientWrapper _blobServiceClientWrapper;
        private BlobContainerClient Client => _blobServiceClientWrapper?.ContainerClient ?? throw new ArgumentException("Client for F not installed correctly");
        public BlobStorageRepository()
        {
            _blobServiceClientWrapper = BlobServiceClientFactory.Instance.First();
        }
        public void SetName(string name)
        {
            _blobServiceClientWrapper = BlobServiceClientFactory.Instance.Get(name);
        }
        private string GetFileName(string path)
            => $"{_blobServiceClientWrapper?.Prefix}{path}";
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
            var response = await blobClient.UploadAsync(new BinaryData(data), overwrite, cancellationToken).NoContext();
            var result = response.Value != null;
            if (result)
            {
                await SetPropertiesAsync(path, options, cancellationToken).NoContext();
            }
            return result;
        }

        public async IAsyncEnumerable<ContentRepositoryDownloadResult> ListAsync(string? prefix = null,
            bool downloadContent = false,
            ContentInformationType informationRetrieve = ContentInformationType.None,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            prefix = $"{_blobServiceClientWrapper?.Prefix}{prefix}";
            await foreach (var blob in Client.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var blobClient = Client.GetBlobClient(blob.Name);
                var blobData = Array.Empty<byte>();
                if (downloadContent)
                    blobData = (await blobClient.DownloadContentAsync(cancellationToken).NoContext()).Value.Content.ToArray();
                var options = await ReadOptionsAsync(blobClient, informationRetrieve);
                yield return new ContentRepositoryDownloadResult
                {
                    Uri = blobClient.Uri.ToString(),
                    Path = blob.Name,
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
    }
}