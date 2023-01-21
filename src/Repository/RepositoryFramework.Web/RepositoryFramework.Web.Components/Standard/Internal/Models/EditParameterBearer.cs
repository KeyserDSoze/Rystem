using System.Reflection;

namespace RepositoryFramework.Web.Components.Standard
{
    public sealed class EditParametersBearer
    {
        public required object? BaseEntity { get; set; }
        public TryResponse<object?> GetValue(BaseProperty baseProperty, int[]? indexes)
            => Try.WithDefaultOnCatch(() => baseProperty.Value(BaseEntity, indexes));
        public void SetValue(BaseProperty baseProperty, object? value)
            => baseProperty.Set(BaseEntity, value);
        public bool CanEdit(BaseProperty baseProperty)
            => !DisableEdit && baseProperty.Self.SetMethod != null;
        public PropertyUiSettings? GetSettings(BaseProperty baseProperty)
        {
            if (PropertiesRetrieved.ContainsKey(baseProperty.NavigationPath))
                return PropertiesRetrieved[baseProperty.NavigationPath];
            return null;
        }
        public Dictionary<string, PropertyUiSettings> PropertiesRetrieved { get; set; }
        public Func<object?, Task<object?>>? EntityRetrieverByKey { get; set; }
        public TypeShowcase BaseTypeShowcase { get; set; }
        public bool DisableEdit { get; set; }
        public Action StateHasChanged { get; set; }
    }
}
