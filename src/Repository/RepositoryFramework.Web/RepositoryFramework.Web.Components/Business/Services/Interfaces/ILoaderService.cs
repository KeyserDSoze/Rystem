namespace RepositoryFramework.Web.Components.Services
{
    public interface ILoaderService
    {
        bool IsVisible { get; }
        void Show();
        void Hide();
        event Action? OnChange;
    }
}
