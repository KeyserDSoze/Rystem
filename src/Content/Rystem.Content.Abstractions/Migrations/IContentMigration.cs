namespace Rystem.Content.Abstractions.Migrations
{
    public interface IContentMigration
    {
        Task<ContentMigrationResult> MigrateAsync(string sourceName, string destinationName, Action<ContentMigrationSettings>? settings = default, CancellationToken cancellationToken = default);
    }
}
