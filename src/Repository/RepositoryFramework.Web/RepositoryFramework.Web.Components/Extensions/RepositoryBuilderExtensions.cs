using Microsoft.Extensions.Localization;
using RepositoryFramework;
using RepositoryFramework.Web;
using RepositoryFramework.Web.Components;
using RepositoryFramework.Web.Components.Business.Language;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RepositoryBuilderExtensions
    {
        public static RepositorySettings<T, TKey> SetDefaultUiRoot<T, TKey>(
            this RepositorySettings<T, TKey> builder)
            where TKey : notnull
        {
            AppInternalSettings.Instance.RootName = typeof(T).Name;
            return builder;
        }
        public static RepositorySettings<T, TKey> DoNotExposeInUi<T, TKey>(
            this RepositorySettings<T, TKey> builder)
            where TKey : notnull
        {
            AppInternalSettings.Instance.NotExposableRepositories.Add(typeof(T).Name);
            return builder;
        }
        private static RepositoryAppMenuItem GetAppMenuSettings<T, TKey>(this RepositorySettings<T, TKey> builder)
            where TKey : notnull
        {
            var name = typeof(T).Name.ToLower();
            if (!AppInternalSettings.Instance.RepositoryAppMenuItems.ContainsKey(name))
                AppInternalSettings.Instance.RepositoryAppMenuItems.Add(name,
                    RepositoryAppMenuItem.CreateDefault(typeof(T), typeof(TKey)));
            return AppInternalSettings.Instance.RepositoryAppMenuItems[name];
        }
        public static RepositorySettings<T, TKey> ExposeFor<T, TKey>(
            this RepositorySettings<T, TKey> builder, int index)
            where TKey : notnull
        {
            builder.GetAppMenuSettings().Index = index;
            return builder;
        }
        public static RepositorySettings<T, TKey> WithIcon<T, TKey>(
            this RepositorySettings<T, TKey> builder, string icon)
            where TKey : notnull
        {
            builder.GetAppMenuSettings().Icon = icon;
            return builder;
        }
        public static RepositorySettings<T, TKey> WithName<T, TKey>(
            this RepositorySettings<T, TKey> builder, string name)
            where TKey : notnull
        {
            builder.GetAppMenuSettings().Name = name;
            return builder;
        }
        public static RepositorySettings<T, TKey> MapPropertiesForUi<T, TKey, TUiMapper>(
            this RepositorySettings<T, TKey> builder)
            where TKey : notnull
            where TUiMapper : class, IRepositoryUiMapper<T, TKey>
        {
            builder.Services.AddSingleton<IRepositoryPropertyUiHelper<T, TKey>, PropertyUiHelper<T, TKey>>();
            builder.Services.AddSingleton<IRepositoryUiMapper<T, TKey>, TUiMapper>();
            builder.Services.AddSingleton<IRepositoryPropertyUiMapper<T, TKey>, PropertyUiMapper<T, TKey>>();
            return builder;
        }
        public static RepositorySettings<T, TKey> WithLocalization<T, TKey, TLocalization>(
            this RepositorySettings<T, TKey> builder)
            where TKey : notnull
            where TLocalization : IStringLocalizer
        {
            RepositoryLocalizationOptions.Instance.LocalizationInterfaces.Add(typeof(T).FullName!, typeof(TLocalization));
            return builder;
        }
    }
}
