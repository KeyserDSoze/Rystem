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
        Task<bool> GetAsync(string id);
    }
    public class Comad : IColam
    {
        public Task<bool> GetAsync(string id)
        {
            return Task.FromResult(true);
        }
    }
}
