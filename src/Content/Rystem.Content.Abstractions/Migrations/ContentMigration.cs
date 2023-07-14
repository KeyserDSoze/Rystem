using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Content.Abstractions.Migrations
{
    internal sealed class ContentMigration : IContentMigration
    {
        private readonly IFactory<IContentRepository> _factory;
        public ContentMigration(IFactory<IContentRepository> factory)
        {
            _factory = factory;
        }

        public async Task<ContentMigrationResult> MigrateAsync(string sourceName, string destinationName, Action<ContentMigrationSettings>? settings = null, CancellationToken cancellationToken = default)
        {
            var result = new ContentMigrationResult();
            var options = new ContentMigrationSettings();
            settings?.Invoke(options);
            var from = _factory.Create(sourceName);
            var to = _factory.Create(destinationName);
            await foreach (var item in from.ListAsync(options.Prefix, false, ContentInformationType.None, cancellationToken))
            {
                var path = item.Path;
                if (options.ModifyDestinationPath != null)
                    path = options.ModifyDestinationPath.Invoke(path!);
                if (path != null)
                {
                    if (options.Predicate?.Invoke(item) != false)
                    {
                        var responseFromUpload = await Try.WithDefaultOnCatchValueTaskAsync(
                            async () =>
                            {
                                var download = await from.DownloadAsync(item.Path!, ContentInformationType.All, cancellationToken).NoContext();
                                if (download?.Data != null)
                                {
                                    return await to.UploadAsync(path, download.Data, download.Options, options.OverwriteIfExists, cancellationToken).NoContext();
                                }
                                return false;
                            }).NoContext();
                        if (responseFromUpload.Exception == null && responseFromUpload.Entity)
                            result.MigratedPaths.Add(new()
                            {
                                From = item.Path!,
                                To = path
                            });
                        else if (responseFromUpload.Exception != null)
                        {
                            result.NotMigratedPathsForErrors.Add(new ContentMigrationExceptionResult
                            {
                                Exception = responseFromUpload.Exception,
                                Path = new()
                                {
                                    From = item.Path!,
                                    To = path
                                },
                            });
                            if (!options.OnErrorContinue)
                                break;
                        }
                        else
                            result.NotContentPaths.Add(new()
                            {
                                From = item.Path!,
                                To = path
                            });
                    }
                    else
                        result.BlockedByPredicatePaths.Add(new()
                        {
                            From = item.Path!,
                            To = path
                        });
                }
            }
            return result;
        }
    }
}
