using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class SizeClassBuilder<T> : DefaultClassBuilder
        where T : ICssClassBuilder
    {
        internal SizeClassBuilder(StringBuilder stringBuilder, string prefix) : base(stringBuilder, prefix)
        {
        }

        public T S1
        {
            get
            {
                StringBuilder.Append("-1");
                return CreateNew<T>();
            }
        }
        public T S2
        {
            get
            {
                StringBuilder.Append("-2");
                return CreateNew<T>();
            }
        }
        public T S3
        {
            get
            {
                StringBuilder.Append("-3");
                return CreateNew<T>();
            }
        }
        public T S4
        {
            get
            {
                StringBuilder.Append("-4");
                return CreateNew<T>();
            }
        }
        public T S5
        {
            get
            {
                StringBuilder.Append("-5");
                return CreateNew<T>();
            }
        }
        public T S6
        {
            get
            {
                StringBuilder.Append("-6");
                return CreateNew<T>();
            }
        }
        public T S7
        {
            get
            {
                StringBuilder.Append("-7");
                return CreateNew<T>();
            }
        }
        public T S8
        {
            get
            {
                StringBuilder.Append("-8");
                return CreateNew<T>();
            }
        }
        public T S9
        {
            get
            {
                StringBuilder.Append("-9");
                return CreateNew<T>();
            }
        }
        public T S10
        {
            get
            {
                StringBuilder.Append("-10");
                return CreateNew<T>();
            }
        }
        public T S11
        {
            get
            {
                StringBuilder.Append("-11");
                return CreateNew<T>();
            }
        }
        public T S12
        {
            get
            {
                StringBuilder.Append("-12");
                return CreateNew<T>();
            }
        }
    }
}
