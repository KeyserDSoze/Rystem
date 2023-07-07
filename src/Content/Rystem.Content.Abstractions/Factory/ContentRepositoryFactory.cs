namespace Rystem.Content
{
    internal sealed class ContentRepositoryFactory : IContentRepositoryFactory
    {
        private readonly IEnumerable<Func<IServiceProvider, string?, IContentRepository?>> _factories;
        private readonly IServiceProvider _serviceProvider;

        public ContentRepositoryFactory(IEnumerable<Func<IServiceProvider, string?, IContentRepository?>> factories, IServiceProvider serviceProvider)
        {
            _factories = factories;
            _serviceProvider = serviceProvider;
        }

        public IContentRepository Create(string? name = null)
        {
            name ??= string.Empty;
            foreach (var factory in _factories)
            {
                var repository = factory.Invoke(_serviceProvider, name);
                if (repository != null)
                    return repository;
            }
            throw new ArgumentException($"File repository {name} is not installed.");
        }
    }
}
