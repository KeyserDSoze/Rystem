namespace Rystem.Api.TestServer.Clients
{
    public interface ISalubry
    {
        Task<bool> GetAsync(int id);
    }
    public class Salubry : ISalubry
    {
        public Task<bool> GetAsync(int id)
            => Task.FromResult(true);
    }
    public class Salubry2 : ISalubry
    {
        public Task<bool> GetAsync(int id)
            => Task.FromResult(false);
    }
    public interface IColam
    {
        Task<bool> GetAsync([ApiParameterLocation(ApiParameterLocation.Path, 3)] string id,
            [ApiParameterLocation(ApiParameterLocation.Query)] string fol, [ApiParameterLocation(ApiParameterLocation.Header)] string cul,
            [ApiParameterLocation(ApiParameterLocation.Body)] Faul faul, [ApiParameterLocation(ApiParameterLocation.Body)] Faul faul2);
    }
    public class Faul
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    public class Comad : IColam
    {
        public Task<bool> GetAsync([ApiParameterLocation(ApiParameterLocation.Path, 3)] string id, [ApiParameterLocation(ApiParameterLocation.Query)] string fol, [ApiParameterLocation(ApiParameterLocation.Header)] string cul, [ApiParameterLocation(ApiParameterLocation.Body)] Faul faul, [ApiParameterLocation(ApiParameterLocation.Body)] Faul faul2)
        {
            return null!;
        }
    }
}
