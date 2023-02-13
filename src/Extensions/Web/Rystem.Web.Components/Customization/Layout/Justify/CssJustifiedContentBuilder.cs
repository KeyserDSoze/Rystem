using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class CssJustifiedContentBuilder : BreakPointClassBuilder<DirectionClassBuilder<CssContainerBuilder>>
    {
        internal CssJustifiedContentBuilder(StringBuilder stringBuilder, string prefix, bool prefixIsTurnedOff) : base(stringBuilder, prefix, prefixIsTurnedOff)
        {
        }
        public static CssJustifiedContentBuilder Style(StringBuilder stringBuilder)
            => new(stringBuilder, string.Empty, true);
        public Bootstrap Build()
            => new(StringBuilder);
        public Bootstrap And()
            => Build();
    }
}
