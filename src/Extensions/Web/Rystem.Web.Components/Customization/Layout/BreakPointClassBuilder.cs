using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class BreakPointClassBuilder<T> : DefaultClassBuilder
        where T : ICssClassBuilder
    {
        internal BreakPointClassBuilder(StringBuilder stringBuilder, string prefix) : base(stringBuilder, prefix)
        {
        }

        public T Default
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Prefix))
                    StringBuilder.Append(Prefix);
                return CreateNew<T>();
            }
        }
        public T Small
        {
            get
            {
                StringBuilder.Append($"{Prefix}-sm");
                return CreateNew<T>();
            }
        }
        public T Medium
        {
            get
            {
                StringBuilder.Append($"{Prefix}-md");
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
