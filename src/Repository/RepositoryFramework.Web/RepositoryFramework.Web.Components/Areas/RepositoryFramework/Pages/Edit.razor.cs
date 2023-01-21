using Microsoft.AspNetCore.Components.Rendering;
using RepositoryFramework.Web.Components.Standard;

namespace RepositoryFramework.Web.Components
{
    public partial class Edit
    {
        private protected override Type StandardType { get; } = typeof(Edit<,>);
        private protected override Action<RenderTreeBuilder>? RenderTreeBuilderConfigurator => (b) =>
        {
            b.AddAttribute(2, Constant.Key, Key);
            b.AddAttribute(3, Constant.EditableKey, true);
            b.AddAttribute(4, Constant.DisableEdit, false);
            b.AddAttribute(5, Constant.AllowDelete, true);
        };
    }
}
