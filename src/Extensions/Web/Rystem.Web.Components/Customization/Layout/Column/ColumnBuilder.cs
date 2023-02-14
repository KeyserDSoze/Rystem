using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class ColumnBuilder : BreakPointClassBuilder<SizeClassBuilder<ColumnBuilder>>
    {
        internal const string ColPrefix = " col";
        internal ColumnBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
            Prefix = ColPrefix;
        }
        public static ColumnBuilder Style => new(new());
    }
}
