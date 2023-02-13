using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class DefaultClassBuilder : ICssClassBuilder
    {
        internal StringBuilder StringBuilder { get; set; }
        internal string Prefix { get; }
        internal bool PrefixIsTurnedOff { get; }
        private protected string GetPrefix()
        {
            if (!PrefixIsTurnedOff)
                return Prefix;
            return string.Empty;
        }
        private protected T CreateNew<T>()
            where T : ICssClassBuilder
        {
            var constructor = typeof(T).GetConstructors(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).First();
            var instance = constructor.Invoke(new object[3] { StringBuilder, Prefix, false });
            return (T)instance;
        }
        internal DefaultClassBuilder(StringBuilder stringBuilder, string prefix, bool prefixIsTurnedOff)
        {
            StringBuilder = stringBuilder;
            Prefix = prefix;
            PrefixIsTurnedOff = prefixIsTurnedOff;
        }
        public override string ToString()
            => StringBuilder.ToString();
    }
}
