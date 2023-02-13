using System.Reflection;

namespace Rystem.Web.Components.Customization
{
    public sealed class TableRowProperties
    {
        public string Id { get; }
        public string NavigationId { get; }
        public string NavigationSelector { get; }
        public string NavigationTabId { get; }
        public string NavigationTabContentId { get; }
        public string Title { get; }
        public TableRowProperties(BaseProperty baseProperty)
        {
            var navigationPath = baseProperty.NavigationPath;
            var selectorName = navigationPath.ToLower().Replace('.', '_');
            Id = $"id_{selectorName}";
            NavigationId = $"nav_{selectorName}";
            NavigationSelector = $"#{NavigationId}";
            NavigationTabId = $"id_{selectorName}_nav";
            //NavigationTabContentId = $"id_{selectorName}_nav_content";
            //if (navigationPath.StartsWith(Constant.ValueWithSeparator))
            //    Title = navigationPath.Replace(Constant.ValueWithSeparator, string.Empty, 1);
            //else
            //    Title = navigationPath;
        }
    }
}
