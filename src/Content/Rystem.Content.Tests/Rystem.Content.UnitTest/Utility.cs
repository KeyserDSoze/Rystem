using System.Reflection;

namespace File.UnitTest
{
    public class Utility
    {
        public async Task<MemoryStream> GetFileAsync()
        {
            var location = Assembly.GetExecutingAssembly().Location;
            location = string.Join('\\', location.Split('\\').Take(location.Split('\\').Length - 1));
            using var readableStream = System.IO.File.OpenRead($"{location}\\Files\\otter.png");
            var editableFile = new MemoryStream();
            await readableStream.CopyToAsync(editableFile);
            return editableFile;
        }
    }
}
