using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class CssRowBuilder : BreakPointClassBuilder<AutoSizeClassBuilder<CssRowBuilder>>
    {
        internal CssRowBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
            Prefix = RowBuilder.RowPrefix;
        }
        public static CssRowBuilder Style(StringBuilder stringBuilder)
            => new(stringBuilder);
        public Bootstrap Build()
            => new(StringBuilder);
        public Bootstrap And()
            => Build();
    }
}
