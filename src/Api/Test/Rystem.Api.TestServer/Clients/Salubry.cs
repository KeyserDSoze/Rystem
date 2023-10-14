namespace Rystem.Api.TestServer.Clients
{
    public interface ISalubry
    {
        Task<bool> GetAsync(int id, Stream stream);
    }
    public class Salubry : ISalubry
    {
        public Task<bool> GetAsync(int id, Stream stream)
            => Task.FromResult(true);
    }
    public class Salubry2 : ISalubry
    {
        public Task<bool> GetAsync(int id, Stream stream)
            => Task.FromResult(false);
    }
    public interface IColam
    {
        Task<bool> GetAsync([Path(Index = 0)] string id,
            IFormFile file,
            [Query] string fol,
            [Header] string cul,
            [Cookie(Name = "xx", IsRequired = false)] string cookie,
            [Form(Name = "aa", IsRequired = false)] Faul faul,
            [Form(Name = "bb", IsRequired = false)] Faul faul2,
            [Form(Name = "cc", IsRequired = false)] IFormFile file2);
    }
    public class Faul
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    public class Comad : IColam
    {
        public Task<bool> GetAsync([Path(Index = 0)] string id,
            IFormFile? file,
            [Query] string fol,
            [Header] string cul,
            [Cookie(Name = "xx")] string cookie,
            [Form(Name = "aa")] Faul? faul,
            [Form(Name = "bb")] Faul? faul2,
            [Form(Name = "cc")] IFormFile? file2)
        {
            return Task.FromResult(true);
        }
    }
}
