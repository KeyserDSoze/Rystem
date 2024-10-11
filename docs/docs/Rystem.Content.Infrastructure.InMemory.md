# Integration with In Memory and Content Repository

    services
    .AddContentRepository()
    .WithInMemoryIntegration("inmemory");

## How to use in a business class

    public class AllStorageTest
    {
        private readonly IContentRepositoryFactory _contentRepositoryFactory;
        private readonly Utility _utility;
        public AllStorageTest(IContentRepositoryFactory contentRepositoryFactory, Utility utility)
        {
            _contentRepositoryFactory = contentRepositoryFactory;
            _utility = utility;
        }
        
        public async Task ExecuteAsync()
        {
            var _contentRepository = _contentRepositoryFactory.Create("inmemory");
            var file = await _utility.GetFileAsync();
            var name = "file.png";
            var contentType = "images/png";
            var metadata = new Dictionary<string, string>()
            {
                { "name", "ale" }
            };
            var tags = new Dictionary<string, string>()
            {
                { "version", "1" }
            };
            var response = await _contentRepository.ExistAsync(name).NoContext();
            if (response)
            {
                await _contentRepository.DeleteAsync(name).NoContext();
                response = await _contentRepository.ExistAsync(name).NoContext();
            }
            Assert.False(response);
            response = await _contentRepository.UploadAsync(name, file.ToArray(), new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata,
                Tags = tags
            }, true).NoContext();
            Assert.True(response);
            response = await _contentRepository.ExistAsync(name).NoContext();
            Assert.True(response);
            var options = await _contentRepository.GetPropertiesAsync(name, ContentInformationType.All).NoContext();
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
            response = await _contentRepository.SetPropertiesAsync(name, new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata,
                Tags = tags
            }).NoContext();
            Assert.True(response);
            options = await _contentRepository.GetPropertiesAsync(name, ContentInformationType.All).NoContext();
            Assert.Equal("single", options.Options.Metadata["ale2"]);
            response = await _contentRepository.DeleteAsync(name).NoContext();
            Assert.True(response);
            response = await _contentRepository.ExistAsync(name).NoContext();
            Assert.False(response);
        }
    }