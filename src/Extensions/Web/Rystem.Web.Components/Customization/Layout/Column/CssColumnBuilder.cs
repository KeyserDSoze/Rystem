using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class CssColumnBuilder : BreakPointClassBuilder<SizeClassBuilder<CssColumnBuilder>>
    {
        internal CssColumnBuilder(StringBuilder stringBuilder, string prefix) : base(stringBuilder, prefix)
        {
        }
        public static CssColumnBuilder Style(StringBuilder stringBuilder)
            => new(stringBuilder, ColumnBuilder.ColPrefix);
        public Bootstrap Build()
            => new(StringBuilder);
        public Bootstrap And()
            => Build();
    }
}
