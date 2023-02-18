using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class RowBuilder : BreakPointClassBuilder<AutoSizeClassBuilder<RowBuilder>>
    {
        internal const string RowPrefix = " row-cols";
        internal RowBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
            stringBuilder.Append(RowPrefix);
        }
        public JustifiedContentBuilder<RowBuilder> JustifyContent
        {
            get
            {
                var style = new JustifiedContentBuilder<RowBuilder>(StringBuilder);
                return style;
            }
        }
        public static RowBuilder Style => new(new());
    }
}
