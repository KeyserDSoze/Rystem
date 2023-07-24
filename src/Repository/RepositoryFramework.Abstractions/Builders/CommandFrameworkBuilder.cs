using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    internal sealed class CommandFrameworkBuilder<T, TKey> : RepositoryBaseBuilder<T, TKey, ICommand<T, TKey>, Command<T, TKey>, ICommandPattern<T, TKey>, ICommandBuilder<T, TKey>>, ICommandBuilder<T, TKey>
        where TKey : notnull
    {
        public CommandFrameworkBuilder(IServiceCollection services) : base(services) { }
    }
}
