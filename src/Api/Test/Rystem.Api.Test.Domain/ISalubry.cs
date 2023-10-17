namespace Rystem.Api.Test.Domain
{
    public interface ISalubry
    {
        Task<bool> GetAsync(int id, Stream stream);
    }
}
