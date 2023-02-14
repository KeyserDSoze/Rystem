using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class ColorClassBuilder<T> : DefaultClassBuilder
        where T : ICssClassBuilder
    {
        internal ColorClassBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
        }

        public T Primary
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-primary");
                return CreateNew<T>();
            }
        }
        public T Secondary
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-secondary");
                return CreateNew<T>();
            }
        }
    }
}
