using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class CssRowBuilder : BreakPointClassBuilder<AutoSizeClassBuilder<CssRowBuilder>>
    {
        internal CssRowBuilder(StringBuilder stringBuilder, string prefix, bool prefixIsTurnedOff) : base(stringBuilder, prefix, prefixIsTurnedOff)
        {
        }
        public static CssRowBuilder Style(StringBuilder stringBuilder)
            => new(stringBuilder, RowBuilder.RowPrefix, false);
        public Bootstrap Build()
            => new(StringBuilder);
        public Bootstrap And()
            => Build();
    }
}
