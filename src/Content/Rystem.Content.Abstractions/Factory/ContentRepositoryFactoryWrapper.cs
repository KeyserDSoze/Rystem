namespace Rystem.Content
{
    internal sealed class ContentRepositoryFactoryWrapper
    {
        public static ContentRepositoryFactoryWrapper Instance { get; } = new();
        private ContentRepositoryFactoryWrapper() { }
        public Dictionary<string, Func<IServiceProvider, IContentRepository?>> Creators { get; } = new();
    }
}
