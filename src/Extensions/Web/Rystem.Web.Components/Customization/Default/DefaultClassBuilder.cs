using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class DefaultClassBuilder : ICssClassBuilder
    {
        internal StringBuilder StringBuilder { get; set; }
        private protected string Prefix { get; set; }
        private protected string GetPrefix() => Prefix ?? string.Empty;
        private protected T CreateNew<T>()
            where T : ICssClassBuilder
        {
            var constructor = typeof(T).GetConstructors(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).First();
            var instance = constructor.Invoke(new object[1] { StringBuilder });
            return (T)instance;
        }
        internal DefaultClassBuilder(StringBuilder stringBuilder)
        {
            StringBuilder = stringBuilder;
        }
        public override string ToString()
            => StringBuilder.ToString();
    }
}
