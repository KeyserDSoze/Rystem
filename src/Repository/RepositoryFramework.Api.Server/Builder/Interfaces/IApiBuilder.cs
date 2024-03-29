﻿using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    public interface IApiBuilder
    {
        IServiceCollection Services { get; }
        IApiBuilder WithDescriptiveName(string descriptiveName);
        IApiBuilder WithName<T>(string name);
        IApiBuilder WithPath(string path);
        IApiBuilder WithVersion(string version);
        IApiBuilder WithDocumentation();
        IApiBuilder WithMapApi();
        IApiBuilder WithModelsApi();
        IApiBuilder WithSwagger();
        IIdentityApiBuilder WithOpenIdAuthentication(Action<ApiIdentitySettings> settings);
        IApiBuilder WithDefaultCorsWithAllOrigins();
        IApiBuilder WithDefaultCors(params string[] domains);
        IApiBuilder WithCors(Action<CorsOptions> options);
    }
}
