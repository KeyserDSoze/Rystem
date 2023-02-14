using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class CssContainerBuilder : BreakPointWithFluidClassBuilder<CssContainerBuilder>
    {
        internal CssContainerBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
        }
        public static CssContainerBuilder Style(StringBuilder stringBuilder)
            => new(stringBuilder);
        public Bootstrap Build()
            => new(StringBuilder);
        public Bootstrap And()
            => Build();
    }
}
