using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Migration
{
    internal class MigrationManager<T, TKey> : IMigrationManager<T, TKey>, IServiceWithOptions<MigrationOptions<T, TKey>>
         where TKey : notnull
    {
        private readonly IFactory<IRepository<T, TKey>>? _repositoryFactory;
        private readonly IFactory<IQuery<T, TKey>>? _queryFactory;
        private readonly IFactory<ICommand<T, TKey>>? _commandFactory;
        private IQuery<T, TKey> _source;
        private ICommand<T, TKey> _destination;
        private IRepository<T, TKey>? _destinationAsRepository;
        private MigrationOptions<T, TKey> _options;
        public MigrationManager(
            MigrationOptions<T, TKey>? options = null,
            IFactory<IRepository<T, TKey>>? repositoryFactory = null,
            IFactory<IQuery<T, TKey>>? queryFactory = null,
            IFactory<ICommand<T, TKey>>? commandFactory = null)
        {
            _repositoryFactory = repositoryFactory;
            _queryFactory = queryFactory;
            _commandFactory = commandFactory;
            if (options != null)
                SetOptions(options);
        }

        public async Task<bool> MigrateAsync(Expression<Func<T, TKey>> navigationKey,
            bool checkIfExists = false,
            bool deleteEverythingBeforeStart = false,
            CancellationToken cancellationToken = default)
        {
            if (checkIfExists && _destinationAsRepository == null)
                throw new ArgumentException($"Destination '{_options.DestinationFactoryName}' is not installed as Repository pattern. But you have set checkIfExists=true, so you need to inject a service as Repository to read and not only use the command pattern.");
            List<Task> setAll = new();
            if (deleteEverythingBeforeStart)
            {
                if (_destinationAsRepository == null)
                    throw new ArgumentException($"Destination '{_options.DestinationFactoryName}' is not installed as Repository pattern. But you have set deleteEverythingBeforeStart=true, so you need to inject a service as Repository to read and not only use the command pattern.");
                var items = await _destinationAsRepository.ToListAsync(cancellationToken).NoContext();
                foreach (var item in items.Where(x => x.Key != null))
                {
                    await _destination.DeleteAsync(item.Key!, cancellationToken).NoContext();
                }
            }
            var entities = await _source.QueryAsync(IFilterExpression.Empty, cancellationToken: cancellationToken).ToListAsync().NoContext();
            foreach (var entity in entities)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;
                setAll.Add(TryToMigrate());
                if (setAll.Count > _options.NumberOfConcurrentInserts)
                {
                    await Task.WhenAll(setAll).NoContext();
                    setAll = new();
                }
                async Task TryToMigrate()
                {
                    if (checkIfExists && (await _destinationAsRepository!.ExistAsync(entity.Key!, cancellationToken).NoContext()).IsOk)
                        return;
                    await _destination.InsertAsync(entity.Key!, entity.Value!, cancellationToken).NoContext();
                }
            }
            return true;
        }

        public void SetOptions(MigrationOptions<T, TKey> options)
        {
            if (_queryFactory != null && _queryFactory.Exists(_options.SourceFactoryName))
            {
                _source = _queryFactory.Create(_options.SourceFactoryName);
            }
            else if (_repositoryFactory != null && _repositoryFactory.Exists(_options.SourceFactoryName))
            {
                _source = _repositoryFactory.Create(_options.SourceFactoryName);
            }
            else
                throw new ArgumentException($"Source '{_options.SourceFactoryName}' is not installed as Repository pattern or Query pattern.");

            if (_commandFactory != null && _commandFactory.Exists(_options.DestinationFactoryName))
            {
                _destination = _commandFactory.Create(_options.DestinationFactoryName);
            }
            else if (_repositoryFactory != null && _repositoryFactory.Exists(_options.DestinationFactoryName))
            {
                _destination = _repositoryFactory.Create(_options.DestinationFactoryName);
            }
            else
                throw new ArgumentException($"Destination '{_options.DestinationFactoryName}' is not installed as Repository pattern or Command pattern.");
            if (_destination is IRepository<T, TKey> repository)
                _destinationAsRepository = repository;
        }
    }
}
