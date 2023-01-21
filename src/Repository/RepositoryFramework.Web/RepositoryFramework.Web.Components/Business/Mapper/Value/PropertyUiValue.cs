namespace RepositoryFramework.Web
{
    public sealed class PropertyUiSettings : BasePropertyUiSettings
    {
        public IEnumerable<LabelValueDropdownItem>? Values { get; init; }
        public bool HasDefault => Default != null || DefaultKey != null;
    }
}
