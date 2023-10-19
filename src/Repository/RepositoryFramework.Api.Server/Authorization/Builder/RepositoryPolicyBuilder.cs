using System.Linq.Dynamic.Core;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Api.Server.Authorization
{
    internal sealed class RepositoryPolicyBuilder<T, TKey> : IRepositoryPolicyBuilder<T, TKey>
        where TKey : notnull
    {
        private readonly IServiceCollection _services;
        private readonly RepositoryFrameworkService _service;
        public RepositoryPolicyBuilder(IServiceCollection services, RepositoryFrameworkService service)
        {
            _services = services;
            _service = service;
        }
        public IRepositoryPolicyBuilder<T, TKey> WithAuthorizationHandler<THandler>(ServiceLifetime lifetime = ServiceLifetime.Transient)
            where THandler : class, IRepositoryAuthorization<T, TKey>
        {
            if (!_services.Any(x => !x.IsKeyedService && x.ImplementationType == typeof(RepositoryRequirementHandler)))
                _services.AddTransient<IAuthorizationHandler, RepositoryRequirementHandler>();
            if (!_services.Any(x => !x.IsKeyedService && x.ServiceType == typeof(IRepositoryAuthorization<T, TKey>) && x.ImplementationType == typeof(THandler)))
                _services.AddService<IRepositoryAuthorization<T, TKey>, THandler>(lifetime);
            var policyName = $"{typeof(THandler).FullName}_{typeof(TKey).FullName}_{typeof(T).FullName}";
            _service.Policies.Add(policyName);
            _services.AddAuthorization(o =>
            {
                o.AddPolicy(policyName,
                    p => p.AddRequirements(
                        new RepositoryRequirement(
                            policyName,
                            typeof(IRepositoryAuthorization<T, TKey>),
                            typeof(TKey),
                            typeof(T),
                            typeof(THandler),
                            ReadKeyValue)));
            });
            return this;
        }
        private static async Task<RepositoryRequirementReader> ReadKeyValue(IHttpContextAccessor httpContextAccessor)
        {
            var method = RepositoryRequirementReader.Methods.First(x => httpContextAccessor.HttpContext!.Request.Path.Value!.Contains(x.Key)).Value;
            var reader = new RepositoryRequirementReader()
            {
                Method = method,
            };
            if (!KeySettings<TKey>.Instance.IsJsonable)
            {
                if (httpContextAccessor.HttpContext!.Request.Query.TryGetValue("key", out var key))
                {
                    reader.Key = KeySettings<TKey>.Instance.Parse(key.ToString());
                }
                if (method == RepositoryMethods.Insert || method == RepositoryMethods.Update)
                {
                    var entityAsString = await ReadAsync().NoContext();
                    reader.Value = entityAsString.FromJson<T>();
                }
            }
            else
            {
                if (method == RepositoryMethods.Insert || method == RepositoryMethods.Update)
                {
                    var entityAsString = await ReadAsync().NoContext();
                    var entity = entityAsString.FromJson<Entity<T, TKey>>();
                    reader.Key = entity.Key;
                    reader.Value = entity.Value;
                }
                else if (method == RepositoryMethods.Exist || method == RepositoryMethods.Get || method == RepositoryMethods.Delete)
                {
                    var keyAsString = await ReadAsync().NoContext();
                    reader.Key = keyAsString.FromJson<TKey>();
                }
            }
            async Task<string> ReadAsync()
            {
                var memoryStream = new MemoryStream();
                httpContextAccessor!.HttpContext!.Request.EnableBuffering();
                await httpContextAccessor.HttpContext.Request.Body.CopyToAsync(memoryStream).NoContext();
                memoryStream.Position = 0;
                httpContextAccessor.HttpContext.Request.Body.Position = 0;
                var body = await memoryStream.ConvertToStringAsync().NoContext();
                return body;
            }
            return reader;
        }
    }
}
