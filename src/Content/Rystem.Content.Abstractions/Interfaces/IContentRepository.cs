namespace Rystem.Content
{
    public interface IContentRepository
    {
        IAsyncEnumerable<ContentRepositoryDownloadResult> ListAsync(string? prefix = null, bool downloadContent = false, ContentInformationType informationRetrieve = ContentInformationType.None, CancellationToken cancellationToken = default);
        Task<ContentRepositoryDownloadResult?> DownloadAsync(string path, ContentInformationType informationRetrieve = ContentInformationType.None, CancellationToken cancellationToken = default);
        Task<ContentRepositoryResult?> GetPropertiesAsync(string path, ContentInformationType informationRetrieve = ContentInformationType.All, CancellationToken cancellationToken = default);
        ValueTask<bool> UploadAsync(string path, byte[] data, ContentRepositoryOptions? options = default, bool overwrite = true, CancellationToken cancellationToken = default);
        ValueTask<bool> SetPropertiesAsync(string path, ContentRepositoryOptions? options = default, CancellationToken cancellationToken = default);
        ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);
        ValueTask<bool> ExistAsync(string path, CancellationToken cancellationToken = default);
    }
}
