using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Web.Components
{
    public interface IAppMenuBuilder
    {
        IServiceCollection Services { get; }
    }
}
