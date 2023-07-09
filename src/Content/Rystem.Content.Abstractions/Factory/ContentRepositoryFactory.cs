namespace Rystem.Content
{
    internal sealed class ContentRepositoryFactory : IContentRepositoryFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public ContentRepositoryFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IContentRepository Create(string? name = null)
            => ContentRepositoryFactoryWrapper.Instance.Creators[name ?? string.Empty].Invoke(_serviceProvider)!;
    }
}
