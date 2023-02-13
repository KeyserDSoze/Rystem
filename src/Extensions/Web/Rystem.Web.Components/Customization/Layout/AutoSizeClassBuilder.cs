using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class AutoSizeClassBuilder<T> : SizeClassBuilder<T>
        where T : ICssClassBuilder
    {
        internal AutoSizeClassBuilder(StringBuilder stringBuilder, string prefix) : base(stringBuilder, prefix)
        {
        }

        public T Auto
        {
            get
            {
                StringBuilder.Append("-auto");
                return CreateNew<T>();
            }
        }
    }
}
