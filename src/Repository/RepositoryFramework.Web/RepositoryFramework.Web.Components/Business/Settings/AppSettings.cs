using System.Reflection;

namespace RepositoryFramework.Web.Components
{
    public sealed class AppSettings
    {
        public required string Name { get; set; }
        /// <summary>
        /// Select icon from https://fonts.google.com/icons?selected=Material+Icons&icon.style=Outlined
        /// </summary>
        public string? Icon { get; set; }
        public string? Image { get; set; }
        public Assembly[] RazorPagesForRoutingAdditionalAssemblies { get; set; }
        public AppPalette Palette { get; set; } = new();
        public AppSizingSettings Sizing { get; set; } = new();
    }
}
