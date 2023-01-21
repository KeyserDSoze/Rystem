namespace RepositoryFramework.Web.Components.Services
{
    internal sealed class LoadService : ILoaderService
    {
        private bool _isVisible = true;
        public bool IsVisible => _isVisible;

        public event Action? OnChange;

        public void Hide()
        {
            _isVisible = false;
            NotifyVisibilityChanged();
        }

        public void Show()
        {
            _isVisible = true;
            NotifyVisibilityChanged();
        }

        private void NotifyVisibilityChanged()
            => OnChange?.Invoke();
    }
}
