using System.Reflection;

namespace System.Reflection
{
    public sealed class ComplexProperty : BaseProperty
    {
        public ComplexProperty(PropertyInfo info, BaseProperty? father, IFurtherParameter[] furtherParameters) : base(info, father, furtherParameters)
        {
            Type = PropertyType.Complex;
        }

        public override IEnumerable<BaseProperty> GetQueryableProperty()
        {
            foreach (var property in Sons)
            {
                if (property.Type == PropertyType.Complex)
                    foreach (var deeperProperty in property.GetQueryableProperty())
                        yield return deeperProperty;
                else
                    yield return property;
            }
        }

        protected override void ConstructWell()
        {
            foreach (var property in Self.PropertyType.FetchProperties())
            {
                Sons.Add(PropertyStrategy.Instance.CreateProperty(property, this, _furtherParameters));
            }
        }
    }
}
