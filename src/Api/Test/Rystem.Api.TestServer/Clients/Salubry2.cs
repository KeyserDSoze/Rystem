using Rystem.Api.Test.Domain;

namespace Rystem.Api.TestServer.Clients
{
    public class Salubry2 : ISalubry
    {
        public Task<bool> GetAsync(int id, Stream stream)
            => Task.FromResult(false);
    }
}
