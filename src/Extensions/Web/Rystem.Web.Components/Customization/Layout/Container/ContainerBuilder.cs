using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class ContainerBuilder : BreakPointWithFluidClassBuilder<ContainerBuilder>
    {
        internal ContainerBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
        }
        public static ContainerBuilder Style => new(new());
    }
}
