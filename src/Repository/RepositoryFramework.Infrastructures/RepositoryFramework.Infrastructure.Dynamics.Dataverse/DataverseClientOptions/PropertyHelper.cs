using System.Reflection;
using System.Text.Json;

namespace RepositoryFramework.Infrastructure.Dynamics.Dataverse
{
    internal sealed class PropertyHelper<T>
    {
        public string Name { get; set; } = null!;
        private PropertyInfo _propertyInfo = null!;
        public Func<string> PrefixName { get; set; } = null!;
        public PropertyInfo Property
        {
            get => _propertyInfo;
            set
            {
                _propertyInfo = value;
                Type = _propertyInfo.PropertyType;
                IsPrimitive = Type.IsPrimitive();
            }
        }
        public bool IsPrimitive { get; private set; }
        public Type Type { get; private set; } = null!;
        public string LogicalName => $"{Prefix ?? PrefixName.Invoke()}{Name.ToLower()}";
        public string? Prefix { get; set; }
        public void SetDataverseEntity(Microsoft.Xrm.Sdk.Entity dataverseEntity, T entity)
        {
            var value = Property.GetValue(entity);
            if (value != null)
                dataverseEntity[LogicalName] = IsPrimitive ? value.ToString() : value.ToJson();
        }
        public void SetEntity(Microsoft.Xrm.Sdk.Entity dataverseEntity, T entity)
        {
            if (dataverseEntity.Contains(LogicalName))
            {
                var value = dataverseEntity[LogicalName];
                if (value != null)
                    Property.SetValue(entity, IsPrimitive ? value.Cast(Type) : value.ToString()!.FromJson(Type));
            }
        }
    }
}
