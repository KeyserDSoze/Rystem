using Microsoft.AspNetCore.Components.Rendering;
using RepositoryFramework.Web.Components.Standard;

namespace RepositoryFramework.Web.Components
{
    public partial class Query
    {
        private protected override Type StandardType { get; } = typeof(Query<,>);
        private protected override Action<RenderTreeBuilder>? RenderTreeBuilderConfigurator => b =>
        {
            b.AddAttribute(2, Constant.Pagination, new PaginationState { ItemsPerPage = 10 });
        };
    }
}
