using Microsoft.Extensions.DependencyInjection;
using Rystem.Web.Components.Services;

namespace Rystem.Web.Components
{
    public sealed class RystemWebBuilder
    {
        public IServiceCollection Services { get; }
        public RystemWebBuilder(IServiceCollection services)
        {
            Services = services;
        }
        public RystemWebBuilder WithCopyService()
        {
            Services.AddScoped<ICopyService, CopyService>();
            return this;
        }
        public RystemWebBuilder WithLoaderService()
        {
            Services.AddScoped<ILoaderService, LoaderService>();
            return this;
        }
        public RystemWebBuilder WithAllServices()
        {
            return WithLoaderService()
                        .WithCopyService();
        }
    }
}
