namespace Rystem.Localization
{
    public interface IRepositoryLocalizer<T>
    {
        T Instance { get; }
    }
}
