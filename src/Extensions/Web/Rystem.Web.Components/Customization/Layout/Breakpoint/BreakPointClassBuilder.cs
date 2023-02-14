using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class BreakPointClassBuilder<T> : DefaultClassBuilder
        where T : ICssClassBuilder
    {
        internal BreakPointClassBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
        }

        public T Default
        {
            get
            {
                StringBuilder.Append(GetPrefix());
                return CreateNew<T>();
            }
        }
        public T Small
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-sm");
                return CreateNew<T>();
            }
        }
        public T Medium
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-md");
                return CreateNew<T>();
            }
        }
        public T Large
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-lg");
                return CreateNew<T>();
            }
        }
        public T ExtraLarge
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-xl");
                return CreateNew<T>();
            }
        }
        public T ExtraExtraLarge
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-xxl");
                return CreateNew<T>();
            }
        }
    }
}
