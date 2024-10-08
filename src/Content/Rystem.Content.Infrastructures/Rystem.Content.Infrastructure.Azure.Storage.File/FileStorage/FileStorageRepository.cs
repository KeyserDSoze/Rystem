﻿using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Content.Infrastructure.Storage
{
    internal sealed class FileStorageRepository : IContentRepository, IServiceWithFactoryWithOptions<FileServiceClientWrapper>
    {
        public void SetOptions(FileServiceClientWrapper options)
        {
            Options = options;
        }
        private ShareClient Client => Options?.ShareClient ?? throw new ArgumentException($"Client for Blob storage repository and factory {_factoryName} not installed correctly");
        public FileServiceClientWrapper? Options { get; set; }
        public FileStorageRepository(FileServiceClientWrapper? options = null)
        {
            if (options != null)
                Options = options;
        }
        private int? _lenghtOfPrefix;
        public int LengthOfPrefix => _lenghtOfPrefix ??= Options?.Prefix?.Length ?? 0;
        private async Task<(ShareDirectoryClient Client, string LastPath)> GetDirectoryClientAsync(string? path, bool createIfNotExists)
        {
            path ??= string.Empty;
            var pathSplitted = path.Split('/');
            var fileName = pathSplitted.Last();
            var directoryClient = Client.GetRootDirectoryClient();
            if (createIfNotExists)
            {
                StringBuilder pathBuilder = new();
                if (!string.IsNullOrWhiteSpace(Options?.Prefix))
                    pathBuilder.Append(Options.Prefix);
                foreach (var directory in pathSplitted.SkipLast(1))
                {
                    pathBuilder.Append($"{directory}/");
                    directoryClient = Client.GetDirectoryClient(pathBuilder.ToString());
                    await directoryClient.CreateIfNotExistsAsync().NoContext();
                }
            }
            else if (pathSplitted.Length > 1)
            {
                directoryClient = Client.GetDirectoryClient($"{Options?.Prefix}{string.Join('/', pathSplitted.SkipLast(1))}");
            }
            return (directoryClient, fileName);
        }
        private async Task<ShareFileClient> GetFileClientAsync(string path, bool createIfNotExists)
        {
            (var directoryClient, var fileName) = await GetDirectoryClientAsync(path, createIfNotExists).NoContext();
            var file = directoryClient.GetFileClient(fileName);
            return file;
        }

        public async ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            var client = await GetFileClientAsync(path, false);
            var response = await client.DeleteAsync(cancellationToken: cancellationToken).NoContext();
            return !response.IsError;
        }

        public async Task<ContentRepositoryDownloadResult?> DownloadAsync(string path, ContentInformationType informationRetrieve = ContentInformationType.None, CancellationToken cancellationToken = default)
        {
            var client = await GetFileClientAsync(path, false);
            if (await client.ExistsAsync(cancellationToken).NoContext())
            {
                var file = await client.DownloadAsync(cancellationToken: cancellationToken);
                return new ContentRepositoryDownloadResult
                {
                    Data = file.Value.Content.ToArray(),
                    Path = path,
                    Uri = path,
                    Options = await ReadOptionsAsync(client, informationRetrieve).NoContext()
                };
            }
            return default;
        }
        public async ValueTask<bool> ExistAsync(string path, CancellationToken cancellationToken = default)
        {
            var fileClient = await GetFileClientAsync(path, false);
            return (await fileClient.ExistsAsync(cancellationToken).NoContext()).Value;
        }
        public async ValueTask<bool> UploadAsync(string path, byte[] data, ContentRepositoryOptions? options = default, bool overwrite = true, CancellationToken cancellationToken = default)
        {
            var fileClient = await GetFileClientAsync(path, true);
            if (!await fileClient.ExistsAsync(cancellationToken).NoContext())
                await fileClient.CreateAsync(data.Length, cancellationToken: cancellationToken).NoContext();
            else
                await fileClient.SetHttpHeadersAsync(new ShareFileSetHttpHeadersOptions { NewSize = data.Length }, cancellationToken: cancellationToken).NoContext();
            var response = await fileClient.UploadAsync(data.ToStream(), cancellationToken: cancellationToken).NoContext();
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
            (var directoryClient, var finalPrefix) = await GetDirectoryClientAsync(prefix, false).NoContext();
            await foreach (var fileOrDirectory in directoryClient.GetFilesAndDirectoriesAsync(
                new ShareDirectoryGetFilesAndDirectoriesOptions()
                {
                    IncludeExtendedInfo = true,
                    Prefix = finalPrefix,
                    Traits = ShareFileTraits.None
                }, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (fileOrDirectory.IsDirectory)
                    continue;
                var path = $"{directoryClient.Path}/{fileOrDirectory.Name}";
                if (Options?.Prefix != null)
                    path = path[LengthOfPrefix..];
                var fileClient = await GetFileClientAsync(path, false).NoContext();
                if (downloadContent)
                {
                    var file = await DownloadAsync(path, informationRetrieve, cancellationToken).NoContext();
                    if (file == null)
                        continue;
                    yield return file;
                }
                else
                {
                    ContentRepositoryResult? propertyResult = null;
                    if (informationRetrieve != ContentInformationType.None)
                    {
                        propertyResult = await GetPropertiesAsync(path, informationRetrieve, cancellationToken).NoContext();
                    }
                    yield return new ContentRepositoryDownloadResult
                    {
                        Uri = fileClient.Uri.ToString(),
                        Path = path,
                        Options = propertyResult?.Options
                    };
                }
            }
        }
        public async Task<ContentRepositoryResult?> GetPropertiesAsync(string path, ContentInformationType informationRetrieve = ContentInformationType.All, CancellationToken cancellationToken = default)
        {
            var fileClient = await GetFileClientAsync(path, false);
            if (await fileClient.ExistsAsync(cancellationToken).NoContext())
            {
                return new ContentRepositoryResult
                {
                    Path = fileClient.Name,
                    Uri = fileClient.Uri.ToString(),
                    Options = await ReadOptionsAsync(fileClient, informationRetrieve).NoContext()
                };
            }
            return default;
        }
        private static Task<ContentRepositoryOptions> ReadOptionsAsync(ShareFileClient fileClient, ContentInformationType informationRetrieve)
        {
            if (informationRetrieve == ContentInformationType.None)
                return Task.FromResult(new ContentRepositoryOptions());
            return InternalReadOptionsAsync(fileClient, informationRetrieve);
        }
        private static async Task<ContentRepositoryOptions> InternalReadOptionsAsync(ShareFileClient fileClient, ContentInformationType informationRetrieve)
        {
            ContentRepositoryHttpHeaders? headers = null;
            Dictionary<string, string>? metadata = null;
            Dictionary<string, string>? tags = null;
            if (informationRetrieve.HasFlag(ContentInformationType.HttpHeaders) || informationRetrieve.HasFlag(ContentInformationType.Metadata))
            {
                var properties = await fileClient.GetPropertiesAsync().NoContext();

                headers = new ContentRepositoryHttpHeaders
                {
                    CacheControl = properties.Value.CacheControl,
                    ContentDisposition = properties.Value.ContentDisposition,
                    ContentEncoding = properties.Value.ContentEncoding != null ? string.Join(",", properties.Value.ContentEncoding) : null,
                    ContentHash = properties.Value.ContentHash,
                    ContentLanguage = properties.Value.ContentLanguage != null ? string.Join(",", properties.Value.ContentLanguage) : null,
                    ContentType = properties.Value.ContentType,
                };
                metadata = properties.Value.Metadata.ToDictionary(x => x.Key, x => x.Value);
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
            var blobClient = await GetFileClientAsync(path, false);
            if (options?.HttpHeaders != null)
            {
                await blobClient.SetHttpHeadersAsync(new ShareFileSetHttpHeadersOptions
                {
                    HttpHeaders = new ShareFileHttpHeaders
                    {
                        CacheControl = options?.HttpHeaders?.CacheControl,
                        ContentDisposition = options?.HttpHeaders?.ContentDisposition,
                        ContentEncoding = new string[] { options?.HttpHeaders?.ContentEncoding },
                        ContentHash = options?.HttpHeaders?.ContentHash,
                        ContentLanguage = new string[] { options?.HttpHeaders?.ContentLanguage },
                        ContentType = options?.HttpHeaders?.ContentType,
                    }
                }, cancellationToken: cancellationToken).NoContext();
            }
            if (options?.Metadata != null)
            {
                await blobClient.SetMetadataAsync(options.Metadata, cancellationToken: cancellationToken).NoContext();
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
