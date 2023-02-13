using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class RowBuilder : BreakPointClassBuilder<AutoSizeClassBuilder<RowBuilder>>
    {
        internal const string RowPrefix = " row-cols";
        internal RowBuilder(StringBuilder stringBuilder, string prefix, bool prefixIsTurnedOff) : base(stringBuilder, prefix, prefixIsTurnedOff)
        {
        }
        public JustifiedContentBuilder<RowBuilder> JustifyContent
        {
            get
            {
                StringBuilder.Append(" justify-content");
                var style = new JustifiedContentBuilder<RowBuilder>(StringBuilder, RowPrefix, true);
                return style;
            }
        }
        public static RowBuilder Style => new(new(), RowPrefix, false);
    }
}
