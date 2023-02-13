using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class ContainerBuilder : BreakPointWithFluidClassBuilder<ContainerBuilder>
    {
        internal ContainerBuilder(StringBuilder stringBuilder, string prefix) : base(stringBuilder, prefix)
        {
        }
        public static ContainerBuilder Style => new(new(), string.Empty);
    }
}
