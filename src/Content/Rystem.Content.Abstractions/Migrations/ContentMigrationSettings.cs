namespace Rystem.Content.Abstractions.Migrations
{
    public sealed class ContentMigrationSettings
    {
        public string? Prefix { get; set; }
        /// <summary>
        /// Filter for path
        /// </summary>
        public Func<ContentRepositoryDownloadResult, bool>? Predicate { get; set; }
        public bool OnErrorContinue { get; set; } = true;
        public bool OverwriteIfExists { get; set; }
        /// <summary>
        /// Modify the path source to another one for your destination.
        /// </summary>
        public Func<string, string>? ModifyDestinationPath { get; set; }
    }
}
