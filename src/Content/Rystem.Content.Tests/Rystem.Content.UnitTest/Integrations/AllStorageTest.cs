using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Models.ODataErrors;
using Rystem.Content;
using Rystem.Content.Abstractions.Migrations;

namespace File.UnitTest
{
    public class AllStorageTest
    {
        private readonly IFactory<IContentRepository> _contentRepositoryFactory;
        private readonly Utility _utility;
        private readonly IContentMigration _contentMigration;

        public AllStorageTest(IFactory<IContentRepository> contentRepositoryFactory,
            Utility utility,
            IContentMigration contentMigration)
        {
            _contentRepositoryFactory = contentRepositoryFactory;
            _utility = utility;
            _contentMigration = contentMigration;
        }
        [Theory]
        [InlineData("blobstorage", true)]
        [InlineData("inmemory", true)]
        [InlineData("sharepoint", true)]
        [InlineData("filestorage", false)]
        public async Task ExecuteAsync(string integrationName, bool hasTagIntegration)
        {
            var contentRepository = _contentRepositoryFactory.Create(integrationName);
            var file = await _utility.GetFileAsync();
            var name = "folder/file.png";
            var contentType = "images/png";
            var metadata = new Dictionary<string, string>()
            {
                { "name", "ale" }
            };
            var tags = new Dictionary<string, string>()
            {
                { "version", "1" }
            };
            var response = await contentRepository.ExistAsync(name).NoContext();
            if (response)
            {
                await contentRepository.DeleteAsync(name).NoContext();
                response = await contentRepository.ExistAsync(name).NoContext();
            }
            Assert.False(response);

            response = await contentRepository.UploadAsync(name, file.ToArray(), new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata,
                Tags = tags
            }, true).NoContext();

            Assert.True(response);
            response = await contentRepository.ExistAsync(name).NoContext();
            Assert.True(response);
            var options = await contentRepository.GetPropertiesAsync(name, ContentInformationType.All).NoContext();
            Assert.NotNull(options.Uri);
            foreach (var x in metadata)
            {
                Assert.Equal(x.Value, options.Options.Metadata[x.Key]);
            }
            if (hasTagIntegration)
                foreach (var x in tags)
                {
                    Assert.Equal(x.Value, options.Options.Tags[x.Key]);
                }
            Assert.Equal(contentType, options.Options.HttpHeaders.ContentType);
            metadata.Add("ale2", "single");
            response = await contentRepository.SetPropertiesAsync(name, new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata,
                Tags = tags
            }).NoContext();
            Assert.True(response);
            options = await contentRepository.GetPropertiesAsync(name, ContentInformationType.All).NoContext();
            Assert.Equal("single", options.Options.Metadata["ale2"]);
            response = await contentRepository.DeleteAsync(name).NoContext();
            Assert.True(response);
            response = await contentRepository.ExistAsync(name).NoContext();
            Assert.False(response);
        }
        [Theory]
        [InlineData("blobstorage")]
        [InlineData("inmemory")]
        [InlineData("sharepoint")]
        [InlineData("filestorage")]
        public async Task ExecuteListAsync(string integrationName)
        {
            var contentRepository = _contentRepositoryFactory.Create(integrationName);
            await foreach (var file in contentRepository.ListAsync(null, true, ContentInformationType.All))
            {
                Assert.NotNull(file.Data);
            }
        }
        [Theory]
        [InlineData("blobstorage")]
        [InlineData("inmemory")]
        [InlineData("sharepoint")]
        [InlineData("filestorage")]
        public async Task OverrideAsync(string integrationName)
        {
            var contentRepository = _contentRepositoryFactory.Create(integrationName);
            var file = await _utility.GetFileAsync();
            var biggerFile = await _utility.GetBiggerFileAsync();
            var uploadResult = await contentRepository.UploadAsync("file", file.ToArray(), overwrite: true).NoContext();
            Assert.True(uploadResult);
            uploadResult = await contentRepository.UploadAsync("file", biggerFile.ToArray(), overwrite: true).NoContext();
            Assert.True(uploadResult);
            var deleteResult = await contentRepository.DeleteAsync("file").NoContext();
            Assert.True(deleteResult);
        }
        [Theory]
        [InlineData("blobstorage")]
        [InlineData("inmemory")]
        [InlineData("sharepoint")]
        [InlineData("filestorage")]
        public async Task ExecuteOnlyListAsync(string integrationName)
        {
            var contentRepository = _contentRepositoryFactory.Create(integrationName);
            await foreach (var file in contentRepository.ListAsync())
            {
                Assert.NotNull(file.Path);
            }
        }
        [Theory]
        [InlineData("blobstorage", "inmemory", "path01")]
        [InlineData("inmemory", "sharepoint", "path02")]
        [InlineData("sharepoint", "blobstorage", "path03")]
        [InlineData("filestorage", "blobstorage", "path04")]
        [InlineData("inmemory", "filestorage", "path05")]
        [InlineData("filestorage", "blobstorage", "path06")]
        public async Task MigrateAsync(string integrationNameFrom, string integrationNameTo, string dispath)
        {
            var contentRepository = _contentRepositoryFactory.Create(integrationNameFrom);
            var contentRepositoryTo = _contentRepositoryFactory.Create(integrationNameTo);
            var prefix = $"Test/Folder1/{dispath}/Folder2/";
            for (var i = 0; i < 10; i++)
            {
                var name = $"{prefix}fileName{i}.txt";
                var file = await _utility.GetFileAsync();
                var contentType = "images/png";
                var metadata = new Dictionary<string, string>()
                {
                    { "name", "ale" }
                };
                var tags = new Dictionary<string, string>()
                {
                    { "version", "1" }
                };
                var response = await contentRepository.ExistAsync(name).NoContext();
                if (!response)
                {
                    response = await contentRepository.UploadAsync(name, file.ToArray(), new ContentRepositoryOptions
                    {
                        HttpHeaders = new ContentRepositoryHttpHeaders
                        {
                            ContentType = contentType
                        },
                        Metadata = metadata,
                        Tags = tags
                    }, true).NoContext();

                    Assert.True(response);
                }
            }
            try
            {
                var result = await _contentMigration.MigrateAsync(integrationNameFrom, integrationNameTo,
                    settings =>
                    {
                        settings.OverwriteIfExists = true;
                        settings.Prefix = prefix;
                        settings.Predicate = (x) =>
                        {
                            return x.Path?.Contains("fileName6") != true;
                        };
                        settings.ModifyDestinationPath = x =>
                        {
                            return x.Replace("Folder2", "Folder3");
                        };
                    }).NoContext();
                Assert.Equal(9, result.MigratedPaths.Count);
                Assert.Equal(9, result.MigratedPaths.Count(x => x.To.Contains("Folder3")));
                Assert.Single(result.BlockedByPredicatePaths);
                await foreach (var item in contentRepository.ListAsync(prefix))
                {
                    if (result.MigratedPaths.Any(x => x.From == item.Path!))
                    {
                        var path = result.MigratedPaths.First(x => x.From == item.Path!).To;
                        Assert.True(await contentRepositoryTo.ExistAsync(path).NoContext());
                    }
                }
            }
            catch (ODataError error)
            {
                Assert.Fail(error.Error?.Code ?? error.Message);
            }
        }
    }
}
