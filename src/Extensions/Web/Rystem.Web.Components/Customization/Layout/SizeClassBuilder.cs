using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class SizeClassBuilder<T> : DefaultClassBuilder
        where T : ICssClassBuilder
    {
        internal SizeClassBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
        }

        public T S1
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-1");
                return CreateNew<T>();
            }
        }
        public T S2
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-2");
                return CreateNew<T>();
            }
        }
        public T S3
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-3");
                return CreateNew<T>();
            }
        }
        public T S4
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-4");
                return CreateNew<T>();
            }
        }
        public T S5
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-5");
                return CreateNew<T>();
            }
        }
        public T S6
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-6");
                return CreateNew<T>();
            }
        }
        public T S7
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-7");
                return CreateNew<T>();
            }
        }
        public T S8
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-8");
                return CreateNew<T>();
            }
        }
        public T S9
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-9");
                return CreateNew<T>();
            }
        }
        public T S10
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-10");
                return CreateNew<T>();
            }
        }
        public T S11
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-11");
                return CreateNew<T>();
            }
        }
        public T S12
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-12");
                return CreateNew<T>();
            }
        }
    }
}
