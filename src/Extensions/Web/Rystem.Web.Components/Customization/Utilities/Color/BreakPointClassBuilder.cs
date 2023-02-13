using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class ColorClassBuilder<T> : DefaultClassBuilder
        where T : ICssClassBuilder
    {
        internal ColorClassBuilder(StringBuilder stringBuilder, string prefix, bool prefixIsTurnedOff) : base(stringBuilder, prefix, prefixIsTurnedOff)
        {
        }

        public T Primary
        {
            get
            {
                StringBuilder.Append($"{Prefix}-primary");
                return CreateNew<T>();
            }
        }
        public T Secondary
        {
            get
            {
                StringBuilder.Append($"{Prefix}-secondary");
                return CreateNew<T>();
            }
        }
        public T Large
        {
            get
            {
                StringBuilder.Append($"{Prefix}-lg");
                return CreateNew<T>();
            }
        }
        public T ExtraLarge
        {
            get
            {
                StringBuilder.Append($"{Prefix}-xl");
                return CreateNew<T>();
            }
        }
        public T ExtraExtraLarge
        {
            get
            {
                StringBuilder.Append($"{Prefix}-xxl");
                return CreateNew<T>();
            }
        }
    }
}
