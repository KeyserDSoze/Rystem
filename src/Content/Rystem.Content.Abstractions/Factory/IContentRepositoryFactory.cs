namespace Rystem.Content
{
    public interface IContentRepositoryFactory
    {
        IContentRepository Create(string? name = null);
    }
}
