using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Test.UnitTest.Microsoft.Extensions.DependencyInjection.Decoration
{
    public interface IRepositoryPattern<T>
    {
        T Get();
    }
    public interface IRepository<T> : IRepositoryPattern<T>
    {
        string Format { get; set; }
    }
    internal sealed class RepositoryPattern<T> : IRepositoryPattern<T>
    {
        public T Get() => default!;
    }
    internal sealed class Repository<T> : IRepository<T>, IFactoryService, IServiceWithOptions<RepositoryOptions>
    {
        private RepositoryOptions _options;
        private readonly IFactory<IRepositoryPattern<T>> _patternFactory;
        private IRepositoryPattern<T> _pattern;
        public string Format { get; set; }
        public Repository(IFactory<IRepositoryPattern<T>> patternFactory)
        {
            _patternFactory = patternFactory;
        }
        public T Get() => _pattern.Get();
        public void SetFactoryName(string name)
        {
            _pattern = _patternFactory.Create(name);
        }

        public void SetOptions(RepositoryOptions options)
        {
            _options = options;
        }
    }
    internal sealed class Cache<T> : IRepository<T>, IDecoratorService<IRepository<T>>, IServiceWithOptions<RepositoryOptions>
    {
        private RepositoryOptions _options;
        private IRepository<T>? _repository;
        public string Format { get; set; }
        public Cache(RepositoryOptions options, IDecoratedService<IRepository<T>>? repository = null)
        {
            _repository = repository.Service;
            _options = options;
        }
        public T Get()
        {
            return _repository.Get();
        }

        public void SetDecoratedService(IRepository<T> service)
        {
            _repository = service;
        }

        public void SetOptions(RepositoryOptions options)
        {
            _options = options;
        }
    }
    public class ToSave
    {
        public string Name { get; set; } = "Hello!!";
    }
    public class RepositoryOptions
    {
        public string Name { get; set; }
    }
}
