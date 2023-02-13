using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class ColumnBuilder : BreakPointClassBuilder<SizeClassBuilder<ColumnBuilder>>
    {
        private const string ColPrefix = " col";
        internal ColumnBuilder(StringBuilder stringBuilder, string prefix) : base(stringBuilder, prefix)
        {
        }
        public static ColumnBuilder Style => new(new(), ColPrefix);
    }
}
