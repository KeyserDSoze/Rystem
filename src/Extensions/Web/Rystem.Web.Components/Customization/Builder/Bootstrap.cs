using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class Bootstrap
    {
        public static Bootstrap Style => new();
        private readonly StringBuilder _stringBuilder;
        internal Bootstrap(StringBuilder? stringBuilder = null)
        {
            _stringBuilder = stringBuilder ?? new();
        }
        public override string ToString()
            => _stringBuilder.ToString();
        public CssContainerBuilder Container
        {
            get
            {
                _stringBuilder.Append(" container");
                var style = CssContainerBuilder.Style(_stringBuilder);
                return style;
            }
        }
        public CssColumnBuilder Column
        {
            get
            {
                _stringBuilder.Append(" col");
                var style = CssColumnBuilder.Style(_stringBuilder);
                return style;
            }
        }
        public CssRowBuilder Row
        {
            get
            {
                _stringBuilder.Append(" row");
                var style = CssRowBuilder.Style(_stringBuilder);
                return style;
            }
        }
        public CssJustifiedContentBuilder JustifyContent
        {
            get
            {
                _stringBuilder.Append(" d-flex justify-content");
                var style = CssJustifiedContentBuilder.Style(_stringBuilder);
                return style;
            }
        }
    }
}
