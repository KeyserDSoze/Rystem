namespace Rystem.Content.Abstractions.Migrations
{
    public sealed class ContentMigrationPath
    {
        public string From { get; set; } = null!;
        public string To { get; set; } = null!;
        public bool IsTheSame => From == To;
    }
}
