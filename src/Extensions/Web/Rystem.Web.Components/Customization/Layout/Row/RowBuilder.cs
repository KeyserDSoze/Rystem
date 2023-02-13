using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class RowBuilder : BreakPointClassBuilder<AutoSizeClassBuilder<RowBuilder>>
    {
        internal const string RowPrefix = " row-cols";
        internal RowBuilder(StringBuilder stringBuilder, string prefix) : base(stringBuilder, prefix)
        {
        }
        public static RowBuilder Style => new(new(), RowPrefix);
    }
}
