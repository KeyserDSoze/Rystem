using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class CssContainerBuilder : BreakPointWithFluidClassBuilder<CssContainerBuilder>
    {
        internal CssContainerBuilder(StringBuilder stringBuilder, string prefix, bool prefixIsTurnedOff) : base(stringBuilder, prefix, prefixIsTurnedOff)
        {
        }
        public static CssContainerBuilder Style(StringBuilder stringBuilder)
            => new(stringBuilder, string.Empty, false);
        public Bootstrap Build()
            => new(StringBuilder);
        public Bootstrap And()
            => Build();
    }
}
