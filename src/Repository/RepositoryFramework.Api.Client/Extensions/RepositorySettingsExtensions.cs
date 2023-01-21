using RepositoryFramework;
using RepositoryFramework.Api.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositorySettingsExtensions
    {
        /// <summary>
        /// Add a Repository Client as IRepository<<typeparamref name="T"/>, <typeparamref name="TKey"/>> with a domain and a starting path
        /// or add a Command Client as ICommand<<typeparamref name="T"/>, <typeparamref name="TKey"/>> with a domain and a starting path
        /// or add a Command Client as IQuery<<typeparamref name="T"/>, <typeparamref name="TKey"/>> with a domain and a starting path.
        /// The final url will be https://{domain}/{startingPath}/
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="settings">IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="serviceLifetime">Service Lifetime</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryApiBuilder<T, TKey> WithApiClient<T, TKey>(this RepositorySettings<T, TKey> settings,
           ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
           where TKey : notnull
        {
            settings.Services.AddSingleton(ApiClientSettings<T, TKey>.Instance);
            settings.SetNotExposable();
            settings.SetStorage<RepositoryClient<T, TKey>>(serviceLifetime);
            return new ApiBuilder<T, TKey>(settings.Services);
        }
        /// <summary>
        /// Add specific interceptor for your <typeparamref name="T"/> client. Interceptor runs before every request.
        /// For example you can add here your JWT retrieve for authorized requests.
        /// </summary>
        /// <typeparam name="TInterceptor">Interceptor service.</typeparam>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="settings">IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="serviceLifetime">Service Lifetime.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static RepositorySettings<T, TKey> AddApiClientSpecificInterceptor<T, TKey, TInterceptor>(
            this RepositorySettings<T, TKey> settings,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TInterceptor : class, IRepositoryClientInterceptor<T>
            where TKey : notnull
        {
            settings
                .Services
                .AddService<IRepositoryClientInterceptor<T>, TInterceptor>(serviceLifetime);
            return settings;
        }
    }
}
