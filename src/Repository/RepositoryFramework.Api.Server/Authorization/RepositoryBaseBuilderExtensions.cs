namespace RepositoryFramework.Api.Server.Authorization
{
    public static class RepositoryBaseBuilderExtensions
    {
        public static IRepositoryPolicyBuilder<T, TKey> ConfigureSpecificPolicies<T, TKey, TRepositoryPattern, TRepositoryBuilder>(this IRepositoryBaseBuilder<T, TKey, TRepositoryPattern, TRepositoryBuilder> builder)
            where TKey : notnull
            where TRepositoryPattern : class
            where TRepositoryBuilder : IRepositoryBaseBuilder<T, TKey, TRepositoryPattern, TRepositoryBuilder>
            => new RepositoryPolicyBuilder<T, TKey>(builder.Services, builder.SetService());
    }
}
