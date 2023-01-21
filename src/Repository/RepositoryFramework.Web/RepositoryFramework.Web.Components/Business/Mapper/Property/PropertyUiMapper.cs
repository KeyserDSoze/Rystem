namespace RepositoryFramework.Web
{
    internal sealed class PropertyUiMapper<T, TKey> : IRepositoryPropertyUiMapper<T, TKey>
        where TKey : notnull
    {
        private readonly IRepositoryPropertyUiHelper<T, TKey> _propertyUiHelper;

        public PropertyUiMapper(IRepositoryPropertyUiHelper<T, TKey> propertyUiHelper, IRepositoryUiMapper<T, TKey> uiMapper)
        {
            _propertyUiHelper = propertyUiHelper;
            uiMapper.Map(_propertyUiHelper);
        }
        public Task<Dictionary<string, PropertyUiSettings>> ValuesAsync(IServiceProvider serviceProvider, Entity<T, TKey>? entity = null)
            => _propertyUiHelper.SettingsAsync(serviceProvider, entity);
    }
}
