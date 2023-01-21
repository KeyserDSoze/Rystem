using System.Linq.Expressions;

namespace System.Reflection
{
    internal sealed class FurtherParameter<T> : IFurtherParameter
    {
        public required string Key { get; set; }
        public required Expression<Func<BaseProperty, T>> Creator { get; set; }
    }
}
