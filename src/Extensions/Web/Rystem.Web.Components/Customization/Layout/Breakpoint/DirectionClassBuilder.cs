using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class DirectionClassBuilder<T> : DefaultClassBuilder
        where T : ICssClassBuilder
    {
        internal DirectionClassBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
        }

        public T Start
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-start");
                return CreateNew<T>();
            }
        }
        public T End
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-end");
                return CreateNew<T>();
            }
        }
        public T Center
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-center");
                return CreateNew<T>();
            }
        }
        public T Between
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-between");
                return CreateNew<T>();
            }
        }
        public T Around
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-around");
                return CreateNew<T>();
            }
        }
        public T Evenly
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-evenly");
                return CreateNew<T>();
            }
        }
    }
}
