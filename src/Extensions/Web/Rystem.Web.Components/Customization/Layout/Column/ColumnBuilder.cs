using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class ColumnBuilder : BreakPointClassBuilder<SizeClassBuilder<ColumnBuilder>>
    {
        internal const string ColPrefix = " col";
        internal ColumnBuilder(StringBuilder stringBuilder, string prefix, bool prefixIsTurnedOff) : base(stringBuilder, prefix, prefixIsTurnedOff)
        {
        }
        public static ColumnBuilder Style => new(new(), ColPrefix, false);
    }
}
