using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class CssJustifiedContentBuilder : BreakPointClassBuilder<DirectionClassBuilder<CssContainerBuilder>>
    {
        internal CssJustifiedContentBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
        }
        public static CssJustifiedContentBuilder Style(StringBuilder stringBuilder)
            => new(stringBuilder);
        public Bootstrap Build()
            => new(StringBuilder);
        public Bootstrap And()
            => Build();
    }
}
