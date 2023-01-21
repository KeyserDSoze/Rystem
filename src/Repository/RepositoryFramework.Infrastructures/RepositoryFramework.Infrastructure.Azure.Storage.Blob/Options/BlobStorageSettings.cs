namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    public class BlobStorageSettings<T, TKey>
        where TKey : notnull
    {
        internal static BlobStorageSettings<T, TKey> Instance { get; } = new BlobStorageSettings<T, TKey>();
        public List<BlobStoragePathComposer<T>> Paths { get; } = new();
        public string GetCurrentPath(T? entity) => Paths.Count > 0 && entity != null ? $"{string.Join('/', Paths.Select(x => x.Retriever(entity)))}/" : string.Empty;
    }
}
