using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Api.Client
{
    internal sealed class RepositoryClient<T, TKey> : IRepository<T, TKey>, IServiceWithOptions<ApiClientSettings<T, TKey>>
        where TKey : notnull
    {
        private readonly HttpClient _httpClient;
        public ApiClientSettings<T, TKey>? Options { get; set; }
        private readonly KeySettings<TKey> _keySettings;
        private readonly IRepositoryClientInterceptor? _clientInterceptor;
        private readonly IRepositoryClientInterceptor<T>? _specificClientInterceptor;
        public RepositoryClient(IHttpClientFactory httpClientFactory,
            KeySettings<TKey> keySettings,
            IRepositoryClientInterceptor? clientInterceptor = null,
            IRepositoryClientInterceptor<T>? specificClientInterceptor = null)
        {
            var name = typeof(T).Name;
            _httpClient = httpClientFactory.CreateClient($"{name}{Const.HttpClientName}");
            _keySettings = keySettings;
            _clientInterceptor = clientInterceptor;
            _specificClientInterceptor = specificClientInterceptor;
        }
        private Task<HttpClient> EnrichedClientAsync(RepositoryMethods api)
        {
            if (_specificClientInterceptor != null)
                return _specificClientInterceptor.EnrichAsync(_httpClient, api);
            else if (_clientInterceptor != null)
                return _clientInterceptor.EnrichAsync(_httpClient, api);
            else
                return Task.FromResult(_httpClient);
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
            else
                return string.Format(path, key.ToString());
        }

        public async Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Delete).NoContext();
            if (_keySettings.IsJsonable)
                return (await PostAsJson<TKey, State<T, TKey>>(client, Options.DeletePath, key, cancellationToken).NoContext())!;
            else
                return (await GetAsJson<State<T, TKey>>(client, GetCorrectUriWithKey(Options.DeletePath, key), cancellationToken).NoContext())!;
        }
        public async Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Get).NoContext();
            if (_keySettings.IsJsonable)
                return await PostAsJson<TKey, T>(client, Options.GetPath, key, cancellationToken).NoContext();
            else
                return await GetAsJson<T>(client, GetCorrectUriWithKey(Options.GetPath, key), cancellationToken).NoContext();
        }
        public async Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Exist).NoContext();
            if (_keySettings.IsJsonable)
                return (await PostAsJson<TKey, State<T, TKey>>(client, Options.ExistPath, key, cancellationToken).NoContext())!;
            else
                return (await GetAsJson<State<T, TKey>>(client, GetCorrectUriWithKey(Options.ExistPath, key), cancellationToken).NoContext())!;
        }
        public async Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Insert).NoContext();
            if (_keySettings.IsJsonable)
                return (await PostAsJson<Entity<T, TKey>, State<T, TKey>>(client, Options.InsertPath, new(value, key), cancellationToken).NoContext())!;
            else
                return (await PostAsJson<T, State<T, TKey>>(client, GetCorrectUriWithKey(Options.InsertPath, key), value, cancellationToken).NoContext())!;
        }
        private const string ApplicationJson = "application/json";

        public async IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Query).NoContext();
            var value = filter.Serialize();
            var jsonContent = new StringContent(value.ToJson(), Encoding.UTF8, ApplicationJson);
            var request = new HttpRequestMessage(HttpMethod.Post, Options.QueryPath)
            {
                Content = jsonContent
            };
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).NoContext();
            await EnsureSuccessStatusCodeAsync(response, cancellationToken).NoContext();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
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
                string.Format(Options.OperationPath, operation.Name, GetPrimitiveNameOrAssemblyQualifiedName()),
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
                return (await PostAsJson<Entity<T, TKey>, State<T, TKey>>(client, Options.UpdatePath, new(value, key), cancellationToken).NoContext())!;
            else
                return (await PostAsJson<T, State<T, TKey>>(client, GetCorrectUriWithKey(Options.UpdatePath, key), value, cancellationToken).NoContext())!;
        }
        public async Task<BatchResults<T, TKey>> BatchAsync(BatchOperations<T, TKey> operations, CancellationToken cancellationToken = default)
        {
            var client = await EnrichedClientAsync(RepositoryMethods.Batch).NoContext();
            var response = await client.PostAsJsonAsync(Options.BatchPath, operations, cancellationToken).NoContext();
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<BatchResults<T, TKey>>(cancellationToken: cancellationToken).NoContext())!;
        }
    }
}
