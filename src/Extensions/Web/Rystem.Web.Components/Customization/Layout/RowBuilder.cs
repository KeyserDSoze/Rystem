using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class RowBuilder : BreakPointClassBuilder<AutoSizeClassBuilder<RowBuilder>>
    {
        private const string RowPrefix = " row-cols";
        internal RowBuilder(StringBuilder stringBuilder, string prefix) : base(stringBuilder, prefix)
        {
        }
        public static RowBuilder Style => new(new(), RowPrefix);
    }
}
