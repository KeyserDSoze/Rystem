namespace RepositoryFramework.Web.Components
{
    public sealed class AppPaletteWrapper
    {
        public static AppPaletteWrapper Instance { get; } = new();
        private AppPaletteWrapper() { }
        public Dictionary<string, AppPalette> Skins { get; set; } = new();
    }
}
