namespace System.Reflection
{
    public sealed class TypeShowcase
    {
        public List<BaseProperty> Properties { get; } = new();
        public List<BaseProperty> Primitives { get; }
        public List<BaseProperty> NonPrimitives { get; }
        public List<BaseProperty> FlatProperties { get; }
        public TypeShowcase(Type type, IFurtherParameter[] furtherParameters)
        {
            foreach (var property in type.FetchProperties())
                Properties.Add(PropertyStrategy.Instance.CreateProperty(property, null, furtherParameters));
            Primitives = GetPrimitives().ToList();
            NonPrimitives = GetNonPrimitives().ToList();
            FlatProperties = GetAllPropertiesAsFlat().ToList();
        }
        private IEnumerable<BaseProperty> GetPrimitives()
        {
            foreach (var property in Properties.Where(x => x.Type == PropertyType.Primitive))
                yield return property;
        }
        private IEnumerable<BaseProperty> GetNonPrimitives()
        {
            foreach (var property in Properties.Where(x => x.Type != PropertyType.Primitive))
                yield return property;
        }
        private IEnumerable<BaseProperty> GetAllPropertiesAsFlat()
        {
            foreach (var property in Properties)
                foreach (var x in property.GetQueryableProperty())
                    yield return x;
        }
    }
}
