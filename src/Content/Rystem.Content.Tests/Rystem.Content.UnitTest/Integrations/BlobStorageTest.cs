using Rystem.Content;

namespace File.UnitTest
{
    public class BlobStorageTest
    {
        private readonly IContentRepository _fileRepository;
        private readonly IContentRepositoryFactory _fileRepositoryFactory;
        private readonly Utility _utility;
        private readonly IContentRepository _blobRepository;
        public BlobStorageTest(IContentRepository fileRepository, IContentRepositoryFactory fileRepositoryFactory, Utility utility)
        {
            _fileRepository = fileRepository;
            _fileRepositoryFactory = fileRepositoryFactory;
            _utility = utility;
            _blobRepository = fileRepositoryFactory.Create("blobstorage");
        }
        [Fact]
        public async Task ExecuteAsync()
        {
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
            var response = await _blobRepository.ExistAsync(name).NoContext();
            if (response)
            {
                await _blobRepository.DeleteAsync(name).NoContext();
                response = await _blobRepository.ExistAsync(name).NoContext();
            }
            Assert.False(response);
            response = await _blobRepository.UploadAsync(name, file.ToArray(), new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata,
                Tags = tags
            }, true).NoContext();
            Assert.True(response);
            response = await _blobRepository.ExistAsync(name).NoContext();
            Assert.True(response);
            var options = await _blobRepository.GetPropertiesAsync(name, ContentInformationType.All).NoContext();
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
            response = await _blobRepository.SetPropertiesAsync(name, new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata,
                Tags = tags
            }).NoContext();
            Assert.True(response);
            options = await _blobRepository.GetPropertiesAsync(name, ContentInformationType.All).NoContext();
            Assert.Equal("single", options.Options.Metadata["ale2"]);
            response = await _blobRepository.DeleteAsync(name).NoContext();
            Assert.True(response);
            response = await _blobRepository.ExistAsync(name).NoContext();
            Assert.False(response);
        }
    }
}
