using Rystem.Content;

namespace File.UnitTest
{
    public class AllStorageTest
    {
        private readonly IContentRepositoryFactory _contentRepositoryFactory;
        private readonly Utility _utility;
        public AllStorageTest(IContentRepositoryFactory contentRepositoryFactory, Utility utility)
        {
            _contentRepositoryFactory = contentRepositoryFactory;
            _utility = utility;
        }
        [Theory]
        [InlineData("blobstorage")]
        [InlineData("inmemory")]
        [InlineData("sharepoint")]
        public async Task ExecuteAsync(string integrationName)
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
        public async Task ExecuteOnlyListAsync(string integrationName)
        {
            var contentRepository = _contentRepositoryFactory.Create(integrationName);
            await foreach (var file in contentRepository.ListAsync())
            {
                Assert.NotNull(file.Path);
            }
        }
    }
}
