namespace RepositoryFramework.Web.Components
{
    internal static class Constant
    {
        public const string Key = nameof(Key);
        public const string KeyWithSeparator = $"{nameof(Key)}.";
        public const string Entity = nameof(Entity);
        public const string EditParametersBearer = nameof(EditParametersBearer);
        public const string BaseProperty = nameof(BaseProperty);
        public const string Entities = nameof(Entities);
        public const string Property = nameof(Property);
        public const string FurtherProperty = nameof(FurtherProperty);
        public const string Context = nameof(Context);
        public const string Name = nameof(Name);
        public const string Value = nameof(Value);
        public const string Label = nameof(Label);
        public const string ValueWithSeparator = $"{nameof(Value)}.";
        public const string Update = nameof(Update);
        public const string EditableKey = nameof(EditableKey);
        public const string DisableEdit = nameof(DisableEdit);
        public const string Edit = nameof(Edit);
        public const string Pagination = nameof(Pagination);
        public const string NavigationPath = nameof(NavigationPath);
        public const string PropertyUiSettings = nameof(PropertyUiSettings);
        public const string PropertiesUiSettings = nameof(PropertiesUiSettings);
        public const string Deep = nameof(Deep);
        public const string AllowDelete = nameof(AllowDelete);
        public const string None = nameof(None);
        public const string Error = nameof(Error);
        public const string DialogWidth = "80%";
        public const string NavLink = "nav-link";
        public const string NavTabPane = "tab-pane fade show";
        public const string NavLinkActive = "nav-link active";
        public const string NavTabPaneActive = "tab-pane fade show active";
        public const string PaletteKey = nameof(PaletteKey);

        public static readonly IEnumerable<LabelValueDropdownItem> BooleanState = new List<LabelValueDropdownItem>()
        {
            new LabelValueDropdownItem
            {
                Id = "None",
                Label= "None",
                Value = null
            },
            new LabelValueDropdownItem
            {
                Id = "True",
                Label= "True",
                Value = true
            },
            new LabelValueDropdownItem
            {
                Id = "False",
                Label= "False",
                Value = false
            },
        };
        public static readonly IEnumerable<LabelValueDropdownItem> BooleanTriState = new List<LabelValueDropdownItem>()
        {
            new LabelValueDropdownItem
            {
                Id = "None",
                Label= "None",
                Value = null
            },
           new LabelValueDropdownItem
            {
                Id = "True",
                Label= "True",
                Value = true
            },
            new LabelValueDropdownItem
            {
                Id = "False",
                Label= "False",
                Value = false
            },
            new LabelValueDropdownItem
            {
                Id = "Null",
                Label= "Null",
                Value = null!
            },
        };
    }
}
