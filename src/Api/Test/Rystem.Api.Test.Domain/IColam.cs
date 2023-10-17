namespace Rystem.Api.Test.Domain
{
    public interface IColam
    {
        Task<bool> GetAsync([Path(Index = 0)] string id,
            Stream file,
            [Query] string fol,
            [Header] string cul,
            [Cookie(Name = "xx")] string cookie,
            [Form(Name = "aa")] Faul? faul,
            [Form(Name = "bb")] Faul? faul2,
            [Form(Name = "cc")] Stream file2);

        Task<bool> GetAsync([Query] string id, Stream file, [Query] string fol, [Query] string cul, [Header] string cookie);
    }
}
