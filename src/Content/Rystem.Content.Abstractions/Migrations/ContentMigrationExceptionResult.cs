namespace Rystem.Content.Abstractions.Migrations
{
    public sealed class ContentMigrationExceptionResult
    {
        public Exception Exception { get; init; } = null!;
        public ContentMigrationPath Path { get; init; } = null!;
    }
}
