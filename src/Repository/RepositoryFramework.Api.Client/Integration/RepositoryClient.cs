using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Api.Client
{
    internal sealed class RepositoryClient<T, TKey> : IRepository<T, TKey>, IServiceWithFactoryWithOptions<ApiClientSettings<T, TKey>>
        where TKey : notnull
    {
        private readonly HttpClient _httpClient;
        private ApiClientSettings<T, TKey>? _options;
        public void SetOptions(ApiClientSettings<T, TKey> options)
        {
            _options = options;
        }
        private readonly KeySettings<TKey> _keySettings;
        private readonly IEnumerable<IRepositoryClientInterceptor>? _clientInterceptors;
        private readonly IEnumerable<IRepositoryClientInterceptor<T>>? _specificClientInterceptors;
        private readonly IEnumerable<IRepositoryClientInterceptor<T, TKey>>? _specificClientInterceptorsWithKeys;
        public RepositoryClient(IHttpClientFactory httpClientFactory,
            KeySettings<TKey> keySettings,
            IServiceProvider serviceProvider)
        {
            var name = typeof(T).Name;
            _httpClient = httpClientFactory.CreateClient($"{name}{Const.HttpClientName}");
            _keySettings = keySettings;
            _clientInterceptors = serviceProvider.GetServices<IRepositoryClientInterceptor>();
            _specificClientInterceptors = serviceProvider.GetServices<IRepositoryClientInterceptor<T>>();
            _specificClientInterceptorsWithKeys = serviceProvider.GetServices<IRepositoryClientInterceptor<T, TKey>>();
        }
        private bool _clientAlreadyEnriched = false;
        private Task<HttpClient> EnrichedClientAsync(RepositoryMethods api)
        {
            if (!_clientAlreadyEnriched)
            {
                return EnrichAsync();
            }
            else
                return Task.FromResult(_httpClient);

            async Task<HttpClient> EnrichAsync()
            {
                if (_clientInterceptors != null)
                    foreach (var interceptor in _clientInterceptors)
                        await interceptor.EnrichAsync(_httpClient, api).NoContext();
                if (_specificClientInterceptors != null)
                    foreach (var interceptor in _specificClientInterceptors)
                        await interceptor.EnrichAsync(_httpClient, api).NoContext();
                if (_specificClientInterceptorsWithKeys != null)
                    foreach (var interceptor in _specificClientInterceptorsWithKeys)
                        await interceptor.EnrichAsync(_httpClient, api).NoContext();
                _clientAlreadyEnriched = true;
                return _httpClient;
            }
        }
        private static async Task<TResult?> PostAsJson<TMessage, TResult>(HttpClient client, string path, TMessage message, CancellationToken cancellationToken)
        {
            var response = await client.PostAsJsonAsync(path, message, cancellationToken).NoContext();
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync(cancellationToken).NoContext();
            if (!string.IsNullOrWhiteSpace(result))
                return result.FromJson<TResult>(RepositoryOptions.JsonSerializerOptions);
            return default;
        }
        private static async Task<TResult?> GetAsJson<TResult>(HttpClient client, string path, CancellationToken cancellationToken)
        {
            var response = await client.GetAsync(path, cancellationToken).NoContext();
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync(cancellationToken).NoContext();
            if (!string.IsNullOrWhiteSpace(result))
                return result.FromJson<TResult>(RepositoryOptions.JsonSerializerOptions);
            return default;
        }
        private static string GetCorrectUriWithKey(string path, TKey key)
        {
            if (key is IKey keyAsIKey)
                return string.Format(path, keyAsIKey.AsString());
            else if (key is IDefaultKey defaultKey)
                return string.Format(path, defaultKey.AsString());
            else
                return string.Format(path, key.ToString());
        }

        public async Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Delete).NoContext();
            if (_keySettings.IsJsonable)
                return (await PostAsJson<TKey, State<T, TKey>>(client, _options!.DeletePath, key, cancellationToken).NoContext())!;
            else
                return (await GetAsJson<State<T, TKey>>(client, GetCorrectUriWithKey(_options!.DeletePath, key), cancellationToken).NoContext())!;
        }
        public async Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Get).NoContext();
            if (_keySettings.IsJsonable)
                return await PostAsJson<TKey, T>(client, _options!.GetPath, key, cancellationToken).NoContext();
            else
                return await GetAsJson<T>(client, GetCorrectUriWithKey(_options!.GetPath, key), cancellationToken).NoContext();
        }
        public async Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Exist).NoContext();
            if (_keySettings.IsJsonable)
                return (await PostAsJson<TKey, State<T, TKey>>(client, _options!.ExistPath, key, cancellationToken).NoContext())!;
            else
                return (await GetAsJson<State<T, TKey>>(client, GetCorrectUriWithKey(_options!.ExistPath, key), cancellationToken).NoContext())!;
        }
        public async Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Insert).NoContext();
            if (_keySettings.IsJsonable)
                return (await PostAsJson<Entity<T, TKey>, State<T, TKey>>(client, _options!.InsertPath, new(value, key), cancellationToken).NoContext())!;
            else
                return (await PostAsJson<T, State<T, TKey>>(client, GetCorrectUriWithKey(_options!.InsertPath, key), value, cancellationToken).NoContext())!;
        }
        private const string ApplicationJson = "application/json";

        public async IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Query).NoContext();
            var value = filter.Serialize();
            var jsonContent = new StringContent(value.ToJson(), Encoding.UTF8, ApplicationJson);
            var request = new HttpRequestMessage(HttpMethod.Post, _options!.QueryPath)
            {
                Content = jsonContent
            };
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).NoContext();
            await EnsureSuccessStatusCodeAsync(response, cancellationToken).NoContext();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).NoContext();
            var items = JsonSerializer.DeserializeAsyncEnumerable<Entity<T, TKey>>(stream, RepositoryOptions.JsonSerializerOptions, cancellationToken);
            if (items != null)
                await foreach (var item in items)
                {
                    if (item != null)
                        yield return item;
                }
        }
        private static async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage message, CancellationToken cancellationToken)
        {
            if (message.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException(await message.Content.ReadAsStringAsync(cancellationToken).NoContext());
        }
        public async ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation,
            IFilterExpression filter, CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Operation).NoContext();
            var value = filter.Serialize();
            var response = await client.PostAsJsonAsync(
                string.Format(_options!.OperationPath, operation.Name, GetPrimitiveNameOrAssemblyQualifiedName()),
                value, cancellationToken).NoContext();
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TProperty>(cancellationToken: cancellationToken).NoContext();
            return result!;

            string? GetPrimitiveNameOrAssemblyQualifiedName()
            {
                var name = operation.Type.AssemblyQualifiedName;
                if (name == null)
                    return null;
                if (PrimitiveMapper.Instance.FromAssemblyQualifiedNameToName.TryGetValue(name, out var value))
                    return value;
                return name;
            }
        }
        public async Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Update).NoContext();
            if (_keySettings.IsJsonable)
                return (await PostAsJson<Entity<T, TKey>, State<T, TKey>>(client, _options!.UpdatePath, new(value, key), cancellationToken).NoContext())!;
            else
                return (await PostAsJson<T, State<T, TKey>>(client, GetCorrectUriWithKey(_options!.UpdatePath, key), value, cancellationToken).NoContext())!;
        }
        public async IAsyncEnumerable<BatchResult<T, TKey>> BatchAsync(
            BatchOperations<T, TKey> operations,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Batch).NoContext();
            var request = new HttpRequestMessage(HttpMethod.Post, _options!.BatchPath)
            {
                Content = new StringContent(operations.ToJson(), Encoding.UTF8, "application/json")
            };
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).NoContext();
            await EnsureSuccessStatusCodeAsync(response, cancellationToken).NoContext();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).NoContext();
            var items = JsonSerializer.DeserializeAsyncEnumerable<BatchResult<T, TKey>>(stream, RepositoryOptions.JsonSerializerOptions, cancellationToken);
            if (items != null)
                await foreach (var item in items)
                {
                    if (item != null)
                        yield return item;
                }
        }
        public string? FactoryName { get; private set; }
        public void SetFactoryName(string name)
        {
            FactoryName = name;
        }

        public async ValueTask<bool> BootstrapAsync(CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Bootstrap).NoContext();
            return await GetAsJson<bool>(client, _options!.BootstrapPath, cancellationToken).NoContext();
        }
    }
}
