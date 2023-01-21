namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    public sealed record BlobStoragePathComposer<T>(Func<T, string?> Retriever, string Name);
}
