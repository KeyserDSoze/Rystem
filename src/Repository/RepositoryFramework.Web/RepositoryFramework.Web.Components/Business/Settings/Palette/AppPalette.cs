namespace RepositoryFramework.Web.Components
{
    public sealed class AppPalette
    {
        public string Primary { get; set; } = "#ff6d41";
        public string Secondary { get; set; } = "#35a0d7";
        public string Success { get; set; } = "#198754";
        public string Info { get; set; } = "#0dcaf0";
        public string Warning { get; set; } = "#ffc107";
        public string Danger { get; set; } = "#dc3545";
        public string Light { get; set; } = "#adb5bd";
        public string Dark { get; set; } = "#000";
        public string Color { get; set; } = "#000";
        public string BackgroundColor { get; set; } = "#fff";
        public LinkPalette Link { get; set; } = new();
        public ButtonPalette Button { get; set; } = new();
        public TablePalette Table { get; set; } = new();
    }
}
