using Rystem.Api.Test.Domain;

namespace Rystem.Api.TestServer.Clients
{
    public class Comad : IColam
    {
        public Task<bool> GetAsync([Path(Index = 0)] string id,
            Stream file,
            [Query] string fol,
            [Header] string cul,
            [Cookie(Name = "xx")] string cookie,
            [Form(Name = "aa")] Faul? faul,
            [Form(Name = "bb")] Faul? faul2,
            [Form(Name = "cc")] Stream file2)
        {
            return Task.FromResult(true);
        }

        public Task<bool> GetAsync([Query] string id, IHttpFile file, [Query] string fol, [Query] string cul, [Header] string cookie)
        {
            return Task.FromResult(true);
        }
    }
}
