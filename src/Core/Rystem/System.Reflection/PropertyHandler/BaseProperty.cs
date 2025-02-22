using System.Collections;
using System.Linq.Dynamic.Core;

namespace System.Reflection
{
    public abstract class BaseProperty
    {
        public BaseProperty? Father { get; }
        public PropertyInfo Self { get; }
        public PropertyType Type { get; private protected set; }
        public PropertyType GenericType { get; }
        public List<BaseProperty> Sons { get; } = [];
        public Type[]? Generics { get; private protected set; }
        public string NavigationPath { get; }
        private readonly Dictionary<string, object> _furtherProperties = [];
        public T GetProperty<T>(string key)
            => (T)_furtherProperties[key];
        public int Deep { get; }
        public int EnumerableDeep { get; private protected set; }
        public Type AssemblyType => Self.PropertyType;
        private readonly List<PropertyInfo> _valueFromContextStack = [];
        public List<BaseProperty> Primitives { get; }
        public List<BaseProperty> NonPrimitives { get; }
        private protected readonly IFurtherParameter[] FurtherParameters;
        protected BaseProperty(PropertyInfo info, BaseProperty? father, IFurtherParameter[] furtherParameters)
        {
            FurtherParameters = furtherParameters;
            Self = info;
            Father = father;
            ConstructWell();
            if (Generics?.Length > 0)
                GenericType = Generics.First().IsPrimitive() ? PropertyType.Primitive : PropertyType.Complex;
            father = Father;
            _valueFromContextStack.Add(Self);
            while (father != null)
            {
                _valueFromContextStack.Add(father.Self);
                father = father.Father;
            }
            _valueFromContextStack.Reverse();
            NavigationPath = string.Join('.', _valueFromContextStack.Select(x => x.Name));
            Deep = NavigationPath.Split('.').Length;
            foreach (var parameter in furtherParameters)
                _furtherProperties.Add(parameter.Key, ((dynamic)parameter).Creator.Compile().Invoke(this));
            Primitives = [.. Sons.Where(x => x.Type == PropertyType.Primitive || x.Type == PropertyType.Flag)];
            NonPrimitives = [.. Sons.Where(x => x.Type != PropertyType.Primitive && x.Type != PropertyType.Flag)];
        }
        protected abstract void ConstructWell();
        public abstract IEnumerable<BaseProperty> GetQueryableProperty();
        public object? Value(object? context, int[]? indexes)
        {
            if (context == null)
                return null;
            var counter = 0;
            foreach (var item in _valueFromContextStack)
            {
                context = item.GetValue(context);
                if (indexes != null && counter < indexes.Length && context is not string && context is IEnumerable enumerable)
                {
                    context = enumerable.ElementAt(indexes[counter]);
                    counter++;
                }
                if (context == null)
                    return null;
            }
            return context;
        }
        public BasePropertyNameValue NamedValue(object? context, int[]? indexes)
        {
            BasePropertyNameValue basePropertyNameValue = new();
            if (context == null)
                return basePropertyNameValue;
            var counter = 0;
            foreach (var item in _valueFromContextStack)
            {
                context = item.GetValue(context);
                basePropertyNameValue.AddName(item.Name);
                if (indexes != null && counter < indexes.Length && context is not string && context is IEnumerable enumerable)
                {
                    basePropertyNameValue.AddIndex(indexes[counter]);
                    context = enumerable.ElementAt(indexes[counter]);
                    counter++;
                }
                if (context == null)
                    return basePropertyNameValue;
            }
            basePropertyNameValue.Value = context;
            return basePropertyNameValue;
        }
        public void Set(object? context, object? value, int[]? indexes)
        {
            if (context == null)
                return;
            var counter = 0;
            foreach (var item in _valueFromContextStack.Take(_valueFromContextStack.Count - 1))
            {
                context = item.GetValue(context);
                if (indexes != null && counter < indexes.Length && context is not string && context is IEnumerable enumerable)
                {
                    context = enumerable.ElementAt(indexes[counter]);
                    counter++;
                }
                if (context == null)
                    return;
            }
            if (context != null)
                Self.SetValue(context, value);
        }
    }
}
