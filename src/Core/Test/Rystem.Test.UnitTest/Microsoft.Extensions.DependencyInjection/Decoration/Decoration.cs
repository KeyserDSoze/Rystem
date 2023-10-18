using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    internal sealed class Repository<T> : IRepository<T>, IServiceForFactory, IServiceWithFactoryWithOptions<RepositoryOptions>
    {
        private RepositoryOptions _options = null!;
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
    internal sealed class Cache<T> : IRepository<T>, IDecoratorService<IRepository<T>>, IServiceWithFactoryWithOptions<RepositoryOptions>
    {
        private RepositoryOptions _options;
        private IRepository<T>? _repository;
        public string Format { get; set; }
        public Cache(IDecoratedService<IRepository<T>>? repository = null)
        {
            _repository = repository.Service;
        }
        public T Get()
        {
            return _repository.Get();
        }

        public void SetDecoratedServices(IEnumerable<IRepository<T>> services)
        {
            _repository = services.First();
        }

        public void SetOptions(RepositoryOptions options)
        {
            _options = options;
        }

        public void SetFactoryName(string name)
        {
            return;
        }
    }
    public class ToSave
    {
        public string Name { get; set; } = "Hello!!";
    }
    public class RepositoryOptions : IFactoryOptions
    {
        public string Name { get; set; }
    }
}
