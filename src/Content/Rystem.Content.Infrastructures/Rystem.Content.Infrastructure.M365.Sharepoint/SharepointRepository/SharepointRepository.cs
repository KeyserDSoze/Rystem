using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace Rystem.Content.Infrastructure
{
    internal sealed class SharepointRepository : IContentRepository, IServiceWithOptions<SharepointClientWrapper>
    {
        private GraphServiceClient _graphClient;
        private string _documentLibraryId;
        private SharepointClientWrapper _clientWrapper;
        public SharepointClientWrapper? Options
        {
            get => _clientWrapper;
            set
            {
                if (value != null)
                {
                    _clientWrapper = value;
                    _graphClient = _clientWrapper.Creator.Invoke();
                    _documentLibraryId = _clientWrapper.DocumentLibraryId;
                }
            }
        }
        public SharepointRepository(SharepointClientWrapper? sharepointClientWrapper = null)
        {
            if (sharepointClientWrapper != null)
                Options = sharepointClientWrapper;
        }
        public async ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            try
            {
                await _graphClient
                   .Drives[_documentLibraryId]
                   .Root
                   .ItemWithPath(path)
                   .DeleteAsync(cancellationToken: cancellationToken)
                   .NoContext();
                return true;
            }
            catch (ODataError oDataError)
            {
                if (oDataError?.Error?.Code == ItemNotFoundKey)
                    return true;
                else
                    throw;
            }
        }

        public async Task<ContentRepositoryDownloadResult?> DownloadAsync(string path, ContentInformationType informationRetrieve = ContentInformationType.None, CancellationToken cancellationToken = default)
        {
            try
            {
                var stream = await _graphClient
                    .Drives[_documentLibraryId]
                    .Root
                    .ItemWithPath(path)
                    .Content
                    .GetAsync()
                    .NoContext();
                if (stream != null)
                {
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream).NoContext();
                    ContentRepositoryResult? contentRepositoryResult = null;
                    if (informationRetrieve != ContentInformationType.None)
                    {
                        contentRepositoryResult = await GetPropertiesAsync(path, informationRetrieve, cancellationToken)
                            .NoContext();
                    }
                    return new ContentRepositoryDownloadResult
                    {
                        Data = memoryStream.ToArray(),
                        Options = contentRepositoryResult?.Options,
                        Path = path,
                        Uri = contentRepositoryResult?.Uri
                    };
                }

                return default;
            }
            catch (ODataError oDataError)
            {
                if (oDataError?.Error?.Code == ItemNotFoundKey)
                    return default;
                else
                    throw;
            }
        }
        private const string ItemNotFoundKey = "itemNotFound";
        public async ValueTask<bool> ExistAsync(string path, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _graphClient
                            .Drives[_documentLibraryId]
                            .Root
                            .ItemWithPath(path)
                            .GetAsync(cancellationToken: cancellationToken)
                            .NoContext();
                return response != null;
            }
            catch (ODataError oDataError)
            {
                if (oDataError?.Error?.Code == ItemNotFoundKey)
                    return false;
                else
                    throw;
            }
        }
        private const string DownloadUriKey = "@microsoft.graph.downloadUrl";

        public async Task<ContentRepositoryResult?> GetPropertiesAsync(string path, ContentInformationType informationRetrieve = ContentInformationType.All, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _graphClient
                            .Drives[_documentLibraryId]
                            .Root
                            .ItemWithPath(path)
                            .GetAsync(cancellationToken: cancellationToken)
                            .NoContext();
                if (response != null)
                {
                    response.AdditionalData.TryGetValue(DownloadUriKey, out var uri);
                    return new ContentRepositoryResult
                    {
                        Uri = uri?.ToString(),
                        Path = path,
                        Options = response.Description != null ? HttpUtility.HtmlDecode(response.Description).FromJson<ContentRepositoryOptions>() : null
                    };
                }
                return default;
            }
            catch (ODataError oDataError)
            {
                if (oDataError?.Error?.Code == ItemNotFoundKey)
                    return default;
                else
                    throw;
            }
        }
        public async ValueTask<bool> SetPropertiesAsync(string path, ContentRepositoryOptions? options = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _graphClient
                           .Drives[_documentLibraryId]
                           .Root
                           .ItemWithPath(path)
                           .PatchAsync(new Microsoft.Graph.Models.DriveItem
                           {
                               Description = options?.ToJson()
                           },
                           cancellationToken: cancellationToken)
                           .NoContext();
                return response != null;
            }
            catch (ODataError oDataError)
            {
                if (oDataError?.Error?.Code == ItemNotFoundKey)
                    return false;
                else
                    throw;
            }
        }
        public async IAsyncEnumerable<ContentRepositoryDownloadResult> ListAsync(string? prefix = null,
            bool downloadContent = false,
            ContentInformationType informationRetrieve = ContentInformationType.None,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var items = new List<DriveItem>();
            DriveItemCollectionResponse? driveItemCollectionResponse = null;
            if (string.IsNullOrWhiteSpace(prefix))
            {
                var root = await _graphClient
                    .Drives[_documentLibraryId]
                    .Root
                    .GetAsync(cancellationToken: cancellationToken)
                    .NoContext();
                if (root == null)
                    yield break;
                driveItemCollectionResponse = await _graphClient
                            .Drives[_documentLibraryId]
                            .Items[root.Id]
                            .Children
                            .GetAsync(cancellationToken: cancellationToken)
                            .NoContext();
            }
            else
            {
                if (!prefix.StartsWith("/"))
                    prefix = "/" + prefix;
                driveItemCollectionResponse = await _graphClient
                   .Drives[_documentLibraryId]
                   .Root
                   .ItemWithPath(prefix)
                   .Children
                   .GetAsync(cancellationToken: cancellationToken)
                   .NoContext();
            }
            if (driveItemCollectionResponse == null)
                yield break;
            var pageIterator = PageIterator<DriveItem, DriveItemCollectionResponse>
                     .CreatePageIterator(_graphClient, driveItemCollectionResponse, Iterator);
            bool Iterator(DriveItem item)
            {
                items.Add(item);
                return true;
            }
            await pageIterator.IterateAsync();
            foreach (var item in items)
            {
                if (item.Folder != null)
                {
                    await foreach (var parentItem in ListAsync($"{prefix}/{item.Name}", downloadContent, informationRetrieve, cancellationToken))
                        yield return parentItem;
                }
                else
                {
                    var currentPath = prefix != null ? $"{prefix}/{item.Name}" : item.Name!;
                    if (downloadContent)
                    {
                        var content = await DownloadAsync(currentPath,
                            informationRetrieve,
                            cancellationToken).NoContext();
                        if (content != null)
                            yield return content;
                    }
                    else if (informationRetrieve != ContentInformationType.None)
                    {
                        var response = await GetPropertiesAsync(currentPath, informationRetrieve, cancellationToken)
                            .NoContext();
                        if (response != null)
                            yield return new ContentRepositoryDownloadResult
                            {
                                Options = response.Options,
                                Uri = response.Uri,
                                Path = response.Path,
                            };
                    }
                    else
                    {
                        yield return new ContentRepositoryDownloadResult
                        {
                            Uri = item.WebUrl,
                            Path = currentPath,
                        };
                    }
                }
            }
        }
        public async ValueTask<bool> UploadAsync(string path, byte[] data, ContentRepositoryOptions? options = null, bool overwrite = true, CancellationToken cancellationToken = default)
        {
            var exists = await ExistAsync(path, cancellationToken).NoContext();
            if (exists && overwrite)
            {
                var response = await _graphClient
                    .Drives[_documentLibraryId]
                    .Items[path]
                    .Content
                    .PutAsync(new MemoryStream(data), cancellationToken: cancellationToken)
                    .NoContext();
                if (options != null)
                    await SetPropertiesAsync(path, options, cancellationToken).NoContext();
                return response?.Size > 0;
            }
            else if (!exists)
            {
                var response = await _graphClient
                   .Drives[_documentLibraryId]
                   .Root
                   .ItemWithPath(path)
                   .Content
                   .PutAsync(new MemoryStream(data), cancellationToken: cancellationToken)
                   .NoContext();
                if (options != null)
                    await SetPropertiesAsync(path, options, cancellationToken).NoContext();
                return response?.Size > 0;
            }
            return false;
        }
    }
}
