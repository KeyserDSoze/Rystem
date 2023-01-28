using Microsoft.AspNetCore.Components;

namespace Rystem.Web.Components.Contents
{
    public partial class DataTable
    {
        [Parameter]
        public RenderFragment<FixedHeader> FixedHeader { set => fixedHeaders.Add(value); }
        private readonly List<RenderFragment<FixedHeader>> fixedHeaders = new();
    }
}
