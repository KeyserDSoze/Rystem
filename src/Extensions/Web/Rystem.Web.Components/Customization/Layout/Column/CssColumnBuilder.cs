using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class CssColumnBuilder : BreakPointClassBuilder<SizeClassBuilder<CssColumnBuilder>>
    {
        internal CssColumnBuilder(StringBuilder stringBuilder, string prefix, bool prefixIsTurnedOff) : base(stringBuilder, prefix, prefixIsTurnedOff)
        {
        }
        public static CssColumnBuilder Style(StringBuilder stringBuilder)
            => new(stringBuilder, ColumnBuilder.ColPrefix, false);
        public Bootstrap Build()
            => new(StringBuilder);
        public Bootstrap And()
            => Build();
    }
}
