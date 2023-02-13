using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class DefaultClassBuilder : ICssClassBuilder
    {
        internal StringBuilder StringBuilder { get; }
        internal string Prefix { get; }
        private protected T CreateNew<T>()
            where T : ICssClassBuilder
        {
            var constructor = typeof(T).GetConstructors(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).First();
            var instance = constructor.Invoke(new object[2] { StringBuilder, Prefix });
            return (T)instance;
        }
        internal DefaultClassBuilder(StringBuilder stringBuilder, string prefix)
        {
            StringBuilder = stringBuilder;
            Prefix = prefix;
        }
        public override string ToString() => StringBuilder.ToString();
    }
}
