namespace RepositoryFramework
{
    internal class Command<T, TKey> : ICommand<T, TKey>
        where TKey : notnull
    {
        private readonly ICommandPattern<T, TKey> _command;
        private readonly IRepositoryBusinessManager<T, TKey>? _businessManager;

        public Command(ICommandPattern<T, TKey> command,
            IRepositoryBusinessManager<T, TKey>? businessManager = null)
        {
            _command = command;
            _businessManager = businessManager;
        }
        public Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default)
            => _businessManager?.HasBusinessBeforeInsert == true || _businessManager?.HasBusinessAfterInsert == true ?
                _businessManager.InsertAsync(_command, key, value, cancellationToken) : _command.InsertAsync(key, value, cancellationToken);
        public Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default)
            => _businessManager?.HasBusinessBeforeUpdate == true || _businessManager?.HasBusinessAfterUpdate == true ?
                _businessManager.UpdateAsync(_command, key, value, cancellationToken) : _command.UpdateAsync(key, value, cancellationToken);
        public Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
           => _businessManager?.HasBusinessBeforeDelete == true || _businessManager?.HasBusinessAfterDelete == true ?
               _businessManager.DeleteAsync(_command, key, cancellationToken) : _command.DeleteAsync(key, cancellationToken);
        public Task<BatchResults<T, TKey>> BatchAsync(BatchOperations<T, TKey> operations, CancellationToken cancellationToken = default)
            => _businessManager?.HasBusinessBeforeBatch == true || _businessManager?.HasBusinessAfterBatch == true ?
                _businessManager.BatchAsync(_command, operations, cancellationToken) : _command.BatchAsync(operations, cancellationToken);
    }
}
