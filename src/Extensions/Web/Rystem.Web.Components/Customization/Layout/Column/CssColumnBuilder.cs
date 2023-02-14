using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class CssColumnBuilder : BreakPointClassBuilder<SizeClassBuilder<CssColumnBuilder>>
    {
        internal CssColumnBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
            Prefix = ColumnBuilder.ColPrefix;
        }
        public static CssColumnBuilder Style(StringBuilder stringBuilder)
            => new(stringBuilder);
        public Bootstrap Build()
            => new(StringBuilder);
        public Bootstrap And()
            => Build();
    }
}
