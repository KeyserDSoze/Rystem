using System.Linq.Expressions;

namespace System.Reflection
{
    public interface IFurtherParameter
    {
        string Key { get; }
        public static IFurtherParameter Create<T>(string key, Expression<Func<BaseProperty, T>> creator)
            => new FurtherParameter<T>()
            {
                Key = key,
                Creator = creator
            };
    }
}
