using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    internal sealed class ApiBuilder : IApiBuilder
    {
        public IServiceCollection Services { get; }
        public ApiBuilder(IServiceCollection services)
        {
            Services = services;
        }
        public IPolicyApiBuilder WithOpenIdAuthentication(Action<ApiIdentitySettings> settings)
        {
            var options = new ApiIdentitySettings();
            settings.Invoke(options);
            ApiSettings.Instance.OpenIdIdentity = options;
            return new PolicyApiBuilder(this);
        }

        public IApiBuilder WithDocumentation()
        {
            ApiSettings.Instance.HasDocumentation = true;
            return this;
        }

        public IApiBuilder WithDescriptiveName(string descriptiveName)
        {
            ApiSettings.Instance.DescriptiveName = descriptiveName;
            return this;
        }
        /// <summary>
        /// Override base name from typeof(T).Name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public IApiBuilder WithName<T>(string name)
        {
            if (!ApiSettings.Instance.Names.ContainsKey(typeof(T).FullName!))
                ApiSettings.Instance.Names.Add(typeof(T).FullName!, name);
            return this;
        }
        public IApiBuilder WithPath(string path)
        {
            ApiSettings.Instance.Path = path;
            return this;
        }

        public IApiBuilder WithSwagger()
        {
            ApiSettings.Instance.HasSwagger = true;
            _ = Services.AddSwaggerConfigurations(ApiSettings.Instance);
            return this;
        }

        public IApiBuilder WithVersion(string version)
        {
            ApiSettings.Instance.Version = version;
            return this;
        }
        public IApiBuilder WithDefaultCorsWithAllOrigins()
        {
            ApiSettings.Instance.HasDefaultCors = true;
            Services.AddCors(options =>
            {
                options.AddPolicy(name: ApiSettings.AllowSpecificOrigins,
                                  policy =>
                                  {
                                      policy
                                        .AllowAnyOrigin()
                                        .AllowAnyHeader()
                                        .AllowAnyMethod();
                                  });
            });
            return this;
        }
        public IApiBuilder WithDefaultCors(params string[] domains)
        {
            ApiSettings.Instance.HasDefaultCors = true;
            Services.AddCors(options =>
            {
                options.AddPolicy(name: ApiSettings.AllowSpecificOrigins,
                                  policy =>
                                  {
                                      policy.WithOrigins(domains)
                                        .AllowAnyHeader()
                                        .AllowAnyMethod();
                                  });
            });
            return this;
        }
        public IApiBuilder WithCors(Action<CorsOptions> options)
        {
            Services.AddCors(options);
            return this;
        }
    }
}
