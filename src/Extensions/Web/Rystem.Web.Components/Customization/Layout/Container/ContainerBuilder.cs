using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class ContainerBuilder : BreakPointWithFluidClassBuilder<ContainerBuilder>
    {
        internal ContainerBuilder(StringBuilder stringBuilder, string prefix, bool prefixIsTurnedOff) : base(stringBuilder, prefix, prefixIsTurnedOff)
        {
        }
        public static ContainerBuilder Style => new(new(), string.Empty, false);
    }
}
