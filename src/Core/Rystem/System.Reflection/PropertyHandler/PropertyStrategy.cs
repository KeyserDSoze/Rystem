using System.Collections;

namespace System.Reflection
{
    internal sealed class PropertyStrategy
    {
        public static PropertyStrategy Instance { get; } = new PropertyStrategy();
        private PropertyStrategy() { }
        public BaseProperty CreateProperty(PropertyInfo propertyInfo, BaseProperty? father, IFurtherParameter[] furtherParameters)
        {
            if (propertyInfo.PropertyType.IsPrimitive())
                return new PrimitiveProperty(propertyInfo, father, furtherParameters);
            else if (propertyInfo.PropertyType.GetInterface(nameof(IEnumerable)) != null)
                return new EnumerableProperty(propertyInfo, father, furtherParameters);
            else
                return new ComplexProperty(propertyInfo, father, furtherParameters);
        }
    }
}
