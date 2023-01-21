using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.XUnitTest.Builder
{
    public interface IStartupBuilder
    {
        Task ConfigureServices(IServiceCollection services);
    }
    public interface IStartupBuilderWithServer : IStartupBuilder
    {
        Task ConfigureMiddleware(IApplicationBuilder applicationBuilder);
    }
    internal sealed class UnitTestDependencyInjectionBuilder : IStartupBuilder
    {
        public Task ConfigureServices(IServiceCollection services)
        {
            throw new NotImplementedException();
        }
    }
}
