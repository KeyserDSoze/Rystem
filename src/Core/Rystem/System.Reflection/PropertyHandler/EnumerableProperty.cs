using System.Linq.Dynamic.Core;

namespace System.Reflection
{
    public sealed class EnumerableProperty : BaseProperty
    {
        public EnumerableProperty(PropertyInfo info, BaseProperty? father, IFurtherParameter[] furtherParameters) : base(info, father, furtherParameters)
        {
            Type = PropertyType.Enumerable;
            EnumerableDeep++;
        }

        public override IEnumerable<BaseProperty> GetQueryableProperty()
        {
            yield return this;
        }

        protected override void ConstructWell()
        {
            var enumerable = Self.PropertyType.GetInterfaces().FirstOrDefault(x => x.Name.StartsWith("IEnumerable`1"));
            Generics = enumerable?.GetGenericArguments();
            if (Generics != null)
                foreach (var generic in Generics)
                {
                    if (!generic.IsPrimitive())
                        foreach (var property in generic.FetchProperties())
                            Sons.Add(PropertyStrategy.Instance.CreateProperty(property, this, _furtherParameters));
                }
        }
    }
}
