namespace Rystem.OpenAi.Actors
{
    public interface IActor
    {
        Task<string> GetMessageAsync();
    }
}
