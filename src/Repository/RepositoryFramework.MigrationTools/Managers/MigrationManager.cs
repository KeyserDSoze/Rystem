using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Migration
{
    internal class MigrationManager<T, TKey> : IMigrationManager<T, TKey>
         where TKey : notnull
    {
        private readonly IQuery<T, TKey> _source;
        private readonly ICommand<T, TKey> _destination;
        private readonly IRepository<T, TKey>? _destinationAsRepository;
        private readonly MigrationOptions<T, TKey> _options;
        public MigrationManager(
            MigrationOptions<T, TKey> options,
            IFactory<IRepository<T, TKey>>? repositoryFactory = null,
            IFactory<IQuery<T, TKey>>? queryFactory = null,
            IFactory<ICommand<T, TKey>>? commandFactory = null)
        {
            _options = options;
            if (queryFactory != null && queryFactory.Exists(_options.SourceFactoryName))
            {
                _source = queryFactory.Create(_options.SourceFactoryName);
            }
            else if (repositoryFactory != null && repositoryFactory.Exists(_options.SourceFactoryName))
            {
                _source = repositoryFactory.Create(_options.SourceFactoryName);
            }
            else
                throw new ArgumentException($"Source '{_options.SourceFactoryName}' is not installed as Repository pattern or Query pattern.");

            if (commandFactory != null && commandFactory.Exists(_options.DestinationFactoryName))
            {
                _destination = commandFactory.Create(_options.DestinationFactoryName);
            }
            else if (repositoryFactory != null && repositoryFactory.Exists(_options.DestinationFactoryName))
            {
                _destination = repositoryFactory.Create(_options.DestinationFactoryName);
            }
            else
                throw new ArgumentException($"Destination '{_options.DestinationFactoryName}' is not installed as Repository pattern or Command pattern.");
            if (_destination is IRepository<T, TKey> repository)
                _destinationAsRepository = repository;
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
    }
}
