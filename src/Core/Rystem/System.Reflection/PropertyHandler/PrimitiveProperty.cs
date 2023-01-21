namespace System.Reflection
{
    public sealed class PrimitiveProperty : BaseProperty
    {
        public PrimitiveProperty(PropertyInfo info, BaseProperty? father, IFurtherParameter[] furtherParameters) : base(info, father, furtherParameters)
        {
            Type = PropertyType.Primitive;
        }

        public override IEnumerable<BaseProperty> GetQueryableProperty()
        {
            yield return this;
        }

        protected override void ConstructWell()
        {

        }
    }
}
