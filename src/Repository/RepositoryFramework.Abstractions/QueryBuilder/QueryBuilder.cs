using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace RepositoryFramework
{
    public class QueryBuilder<T, TKey>
        where TKey : notnull
    {
        private readonly IQueryPattern<T, TKey> _query;
        private readonly FilterExpression _operations = new();
        internal QueryBuilder(IQueryPattern<T, TKey> query)
        {
            _query = query;
        }
        /// <summary>
        /// Take all elements by <paramref name="predicate"/> query.
        /// </summary>
        /// <param name="top">Number of elements to take.</param>
        /// <returns>QueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public QueryBuilder<T, TKey> Where(Expression<Func<T, bool>> predicate)
        {
            _ = _operations.Where(predicate);
            return this;
        }
        /// <summary>
        /// Take first <paramref name="top"/> elements.
        /// </summary>
        /// <param name="top">Number of elements to take.</param>
        /// <returns>QueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public QueryBuilder<T, TKey> Take(int top)
        {
            _ = _operations.Take(top);
            return this;
        }
        /// <summary>
        /// Skip first <paramref name="skip"/> elements.
        /// </summary>
        /// <param name="skip">Number of elements to skip.</param>
        /// <returns>QueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public QueryBuilder<T, TKey> Skip(int skip)
        {
            _ = _operations.Skip(skip);
            return this;
        }
        /// <summary>
        /// Order by ascending with your query.
        /// </summary>
        /// <param name="predicate">Expression query.</param>
        /// <returns>QueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public QueryBuilder<T, TKey> OrderBy(Expression<Func<T, object>> predicate)
        {
            _ = _operations.OrderBy(predicate);
            return this;
        }
        /// <summary>
        /// Order by ascending with your query.
        /// </summary>
        /// <param name="predicate">Expression query.</param>
        /// <returns>QueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public QueryBuilder<T, TKey> OrderBy<TProperty>(Expression<Func<T, TProperty>> predicate)
        {
            _ = _operations.OrderBy(predicate);
            return this;
        }
        /// <summary>
        /// Order by descending with your query.
        /// </summary>
        /// <param name="predicate">Expression query.</param>
        /// <returns>QueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public QueryBuilder<T, TKey> OrderByDescending(Expression<Func<T, object>> predicate)
        {
            _ = _operations.OrderByDescending(predicate);
            return this;
        }
        /// <summary>
        /// Order by descending with your query.
        /// </summary>
        /// <param name="predicate">Expression query.</param>
        /// <returns>QueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public QueryBuilder<T, TKey> OrderByDescending<TProperty>(Expression<Func<T, TProperty>> predicate)
        {
            _ = _operations.OrderByDescending(predicate);
            return this;
        }
        /// <summary>
        /// Then by ascending with your query.
        /// </summary>
        /// <param name="predicate">Expression query.</param>
        /// <returns>QueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public QueryBuilder<T, TKey> ThenBy(Expression<Func<T, object>> predicate)
        {
            _ = _operations.ThenBy(predicate);
            return this;
        }
        /// <summary>
        /// Then by ascending with your query.
        /// </summary>
        /// <param name="predicate">Expression query.</param>
        /// <returns>QueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public QueryBuilder<T, TKey> ThenBy<TProperty>(Expression<Func<T, TProperty>> predicate)
        {
            _ = _operations.ThenBy(predicate);
            return this;
        }
        /// <summary>
        /// Then by descending with your query.
        /// </summary>
        /// <param name="predicate">Expression query.</param>
        /// <returns>QueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public QueryBuilder<T, TKey> ThenByDescending(Expression<Func<T, object>> predicate)
        {
            _ = _operations.ThenByDescending(predicate);
            return this;
        }
        /// <summary>
        /// Then by descending with your query.
        /// </summary>
        /// <param name="predicate">Expression query.</param>
        /// <returns>QueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public QueryBuilder<T, TKey> ThenByDescending<TProperty>(Expression<Func<T, TProperty>> predicate)
        {
            _ = _operations.ThenByDescending(predicate);
            return this;
        }
        /// <summary>
        /// Group by a value your query.
        /// </summary>
        /// <typeparam name="TProperty">Grouped by this property.</typeparam>
        /// <param name="predicate">Expression query.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>IEnumerable<IGrouping<<typeparamref name="TProperty"/>, <typeparamref name="T"/>>></returns>
        public IAsyncEnumerable<IAsyncGrouping<TProperty, Entity<T, TKey>>> GroupByAsync<TProperty>(Expression<Func<T, TProperty>> predicate, CancellationToken cancellationToken = default)
        {
            _ = _operations.GroupBy(predicate);
            var compiledPredicate = predicate.Compile();
            var items = QueryAsync(cancellationToken).GroupBy(x => compiledPredicate.Invoke(x.Value!));
            return items;
        }
        /// <summary>
        /// Check if exists at least one element with the selected query.
        /// </summary>
        /// <param name="predicate">Expression query.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>bool</returns>
        public ValueTask<bool> AnyAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            if (predicate != null)
                _ = _operations.Where(predicate);
            Take(1);
            return _query.QueryAsync(_operations, cancellationToken).AnyAsync(cancellationToken);
        }
        /// <summary>
        /// Take the first value of your query or default value T.
        /// </summary>
        /// <param name="predicate">Expression query.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns><typeparamref name="T"/></returns>
        public ValueTask<Entity<T, TKey>?> FirstOrDefaultAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            if (predicate != null)
                _ = Where(predicate);
            Take(1);
            var query = _query
                .QueryAsync(_operations, cancellationToken).FirstOrDefaultAsync(cancellationToken);
            return query;
        }
        /// <summary>
        /// Take the first value of your query.
        /// </summary>
        /// <param name="predicate">Expression query.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns><typeparamref name="T"/></returns>
        public ValueTask<Entity<T, TKey>> FirstAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            if (predicate != null)
                _ = Where(predicate);
            Take(1);
            var query = _query
                .QueryAsync(_operations, cancellationToken).FirstAsync(cancellationToken);
            return query;
        }
        /// <summary>
        /// Starting from page 1 you may page your query.
        /// </summary>
        /// <param name="page">Page of your request, starting from 1.</param>
        /// <param name="pageSize">Number of elements for page. Minimum value is 1.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Paged results.</returns>
        public Task<Page<T, TKey>> PageAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (page < 1)
                throw new ArgumentException($"Page parameter with value {page} is lesser than 1");
            if (pageSize < 1)
                throw new ArgumentException($"Page size parameter with value {pageSize} is lesser than 1");
            return PageInternalAsync(page, pageSize, cancellationToken);
        }
        private async Task<Page<T, TKey>> PageInternalAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            FilterExpression operations = new();
            foreach (var where in _operations.Operations.Where(x => x.Operation == FilterOperations.Where))
                operations.Where((where as LambdaFilterOperation)!.Expression!);
            Skip((page - 1) * pageSize);
            Take(pageSize);
            var query = await ToListAsync(cancellationToken).NoContext();
            var count = await _query.OperationAsync(OperationType<long>.Count, operations, cancellationToken).NoContext();
            var pages = count / pageSize + (count % pageSize > 0 ? 1 : 0);
            return new Page<T, TKey>(query, count, pages);
        }
        /// <summary>
        /// List the query.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>List<<typeparamref name="T"/>></returns>
        public ValueTask<List<Entity<T, TKey>>> ToListAsync(CancellationToken cancellationToken = default)
            => _query.QueryAsync(_operations, cancellationToken).ToListAsync(cancellationToken);
        /// <summary>
        /// List the query without TKey.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>List<<typeparamref name="T"/>></returns>
        public async ValueTask<List<T>> ToListAsEntityAsync(CancellationToken cancellationToken = default)
        {
            List<T> entities = new();
            await foreach (var entity in _query.QueryAsync(_operations, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (entity.Value != null)
                    entities.Add(entity.Value);
            }
            return entities;
        }
        /// <summary>
        /// Call query method in your Repository and retrieve entity without TKey.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>IAsyncEnumerable<<typeparamref name="T"/>></returns>
        public async IAsyncEnumerable<T> QueryAsEntityAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var entity in _query.QueryAsync(_operations, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return entity.Value!;
            }
        }

        /// <summary>
        /// Call query method in your Repository.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>IAsyncEnumerable<<typeparamref name="T"/>></returns>
        public IAsyncEnumerable<Entity<T, TKey>> QueryAsync(CancellationToken cancellationToken = default)
            => _query.QueryAsync(_operations, cancellationToken);
        /// <summary>
        /// Call operation method in your Repository.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ValueTask<<typeparamref name="TProperty"/>></returns>
        public ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation, CancellationToken cancellationToken = default)
            => _query.OperationAsync(operation, _operations, cancellationToken);
        /// <summary>
        /// Count the items by your query.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>ValueTask<long></returns>
        public ValueTask<long> CountAsync(CancellationToken cancellationToken = default)
            => _query.OperationAsync(OperationType<long>.Count, _operations, cancellationToken);
        /// <summary>
        /// Sum the column of your items by your query.
        /// </summary>
        /// <typeparam name="TProperty">Type of column selected.</typeparam>
        /// <param name="predicate">Select the columnt to sum.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>ValueTask<decimal></returns>
        public ValueTask<TProperty> SumAsync<TProperty>(Expression<Func<T, TProperty>> predicate, CancellationToken cancellationToken = default)
        {
            _operations.Select(predicate);
            return _query.OperationAsync(OperationType<TProperty>.Sum, _operations, cancellationToken);
        }

        /// <summary>
        /// Calculate the average of your column by your query.
        /// </summary>
        /// <typeparam name="TProperty">Type of column selected.</typeparam>
        /// <param name="predicate">Select the column for average calculation.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>ValueTask<decimal></returns>
        public ValueTask<TProperty> AverageAsync<TProperty>(Expression<Func<T, TProperty>> predicate, CancellationToken cancellationToken = default)
        {
            _operations.Select(predicate);
            return _query.OperationAsync(OperationType<TProperty>.Average, _operations, cancellationToken);
        }

        /// <summary>
        /// Search the max between items by your query.
        /// </summary>
        /// <typeparam name="TProperty">Type of column selected.</typeparam>
        /// <param name="predicate">Select the column for max search.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>ValueTask<decimal></returns>
        public ValueTask<TProperty> MaxAsync<TProperty>(Expression<Func<T, TProperty>> predicate, CancellationToken cancellationToken = default)
        {
            _operations.Select(predicate);
            return _query.OperationAsync(OperationType<TProperty>.Max, _operations, cancellationToken);
        }

        /// <summary>
        /// Search the min between items by your query.
        /// </summary>
        /// <typeparam name="TProperty">Type of column selected.</typeparam>
        /// <param name="predicate">Select the column for min search.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>ValueTask<decimal></returns>
        public ValueTask<TProperty> MinAsync<TProperty>(Expression<Func<T, TProperty>> predicate, CancellationToken cancellationToken = default)
        {
            _operations.Select(predicate);
            return _query.OperationAsync(OperationType<TProperty>.Min, _operations, cancellationToken);
        }
    }
}
