namespace RepositoryFramework.Web.Components
{
    public sealed class AppMenuItem
    {
        private const string AppMenuItemDefaultUri = "#";
        public string Icon { get; set; }
        public string Name { get; set; }
        public string Uri { get; set; } = AppMenuItemDefaultUri;
        public bool IsSelected { get; set; }
        private const string SelectedCss = "nav-link dropdown-toggle active";
        private const string NotSelectedCss = "nav-link dropdown-toggle";
        private const string SelectedCssForInner = "dropdown-item active";
        private const string NotSelectedCssForInner = "dropdown-item";
        public string CssForSelected => IsSelected ? SelectedCss : NotSelectedCss;
        public string CssForSelectedForSub => IsSelected ? SelectedCssForInner : NotSelectedCssForInner;
        public List<AppMenuItem>? Items { get; set; }
    }
}
