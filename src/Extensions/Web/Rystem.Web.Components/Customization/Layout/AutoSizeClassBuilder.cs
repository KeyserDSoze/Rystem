using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class AutoSizeClassBuilder<T> : SizeClassBuilder<T>
        where T : ICssClassBuilder
    {
        internal AutoSizeClassBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
        }

        public T Auto
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-auto");
                return CreateNew<T>();
            }
        }
    }
}
