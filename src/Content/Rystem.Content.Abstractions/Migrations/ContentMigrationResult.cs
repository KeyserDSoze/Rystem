namespace Rystem.Content.Abstractions.Migrations
{
    public sealed class ContentMigrationResult
    {
        public List<ContentMigrationPath> MigratedPaths { get; set; } = new();
        public List<ContentMigrationPath> NotMigratedPaths { get; set; } = new();
        public List<ContentMigrationPath> NotContentPaths { get; set; } = new();
        public List<ContentMigrationPath> BlockedByPredicatePaths { get; set; } = new();
        public List<ContentMigrationExceptionResult> NotMigratedPathsForErrors { get; set; } = new();
    }
}
