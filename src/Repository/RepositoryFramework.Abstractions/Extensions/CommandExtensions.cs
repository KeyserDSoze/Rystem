namespace RepositoryFramework
{
    public static class CommandExtensions
    {
        public static BatchOperationsBuilder<T, TKey> CreateBatchOperation<T, TKey>(
            this ICommandPattern<T, TKey> command)
            where TKey : notnull
        {
            var operations = new BatchOperationsBuilder<T, TKey>(command);
            return operations;
        }
    }
}
