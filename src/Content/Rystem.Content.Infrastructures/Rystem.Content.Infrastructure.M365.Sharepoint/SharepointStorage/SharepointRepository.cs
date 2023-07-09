using Microsoft.Graph;

namespace Rystem.Content.Infrastructure
{
    internal sealed class SharepointRepository : IContentRepository
    {
        private GraphServiceClient _graphClient;
        private string _siteId;
        private string _documentLibraryId;
        public async ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            await _graphClient
                .Sites[_siteId]
                .Drives[_documentLibraryId]
                .Root
                .ItemWithPath(path)
                .DeleteAsync()
                .NoContext();
            return true;
        }

        public async Task<ContentRepositoryDownloadResult?> DownloadAsync(string path, ContentInformationType informationRetrieve = ContentInformationType.None, CancellationToken cancellationToken = default)
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
                return new ContentRepositoryDownloadResult
                {
                    Data = memoryStream.ToArray(),

                };
            }
            return default;
        }

        public async ValueTask<bool> ExistAsync(string path, CancellationToken cancellationToken = default)
        {
            var item = await _graphClient
                .Sites[_siteId]
                .Drives[_documentLibraryId]
                .
                .ToGetRequestInformation((x) =>
                {
                 x.   
                })
                .ItemWithPath(path)
                .GetAsync()
                .NoContext();
            return item != null;
        }

        public async Task<ContentRepositoryResult?> GetPropertiesAsync(string path, ContentInformationType informationRetrieve = ContentInformationType.All, CancellationToken cancellationToken = default)
        {
            var item = await _graphClient
                .Sites[_siteId]
                .Items[_documentLibraryId]
                .
               //.ItemWithPath(path)
               //.GetAsync()
               //.NoContext();
            return new ContentRepositoryResult
            {
                Uri = path,
                Path = path,
                Options = new ContentRepositoryOptions
                {
                    HttpHeaders = new ContentRepositoryHttpHeaders
                    {
                        ContentType = item.
                    }
                }
            }
        }

        public async IAsyncEnumerable<ContentRepositoryDownloadResult> ListAsync(string? prefix = null, bool downloadContent = false, ContentInformationType informationRetrieve = ContentInformationType.None, CancellationToken cancellationToken = default)
        {
            var items = await _graphClient
                .Sites[_siteId]
                .Lists[_documentLibraryId]
                .Items
                .GetAsync()
                .NoContext();
            foreach (var item in items.Value)
            {
                yield return new ContentRepositoryDownloadResult
                {
                    Uri = item.WebUrl,
                    Path = item.Id,
                    Data = item.DriveItem.Content,
                    Options = new ContentRepositoryOptions
                    {
                        HttpHeaders = new ContentRepositoryHttpHeaders
                        {
                            ContentType = item.ContentType.OdataType,
                        },
                        Metadata = item.AdditionalData.ToDictionary(x => x.Key, x => x.Value?.ToString()),
                    }
                };
            }
        }

        public void SetName(string name)
        {
            var clientFactory = SharepointServiceClientFactory.Instance.Get(name ?? string.Empty);
            _graphClient = clientFactory.Creator.Invoke();
            _siteId = clientFactory.SiteId;
            _documentLibraryId = clientFactory.DocumentLibraryId;
        }

        public ValueTask<bool> SetPropertiesAsync(string path, ContentRepositoryOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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
                return response.Size > 0;
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

                return response.Size > 0;
            }
            return false;
        }
    }
}
