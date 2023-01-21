namespace RepositoryFramework.Cache.Azure.Storage.Blob
{
    public class BlobStorageCacheModel
    {
        public DateTime Expiration { get; set; }
        public string? Value { get; set; }
    }
}
