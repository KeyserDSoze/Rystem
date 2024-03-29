﻿using Microsoft.Extensions.Localization;
using RepositoryFramework;
using RepositoryFramework.Web;
using RepositoryFramework.Web.Components;
using RepositoryFramework.Web.Components.Business.Language;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RepositorySettingsExtensions
    {
        public static IRepositoryBuilder<T, TKey> AddAction<T, TKey, TAction>(
            this IRepositoryBuilder<T, TKey> builder)
            where TKey : notnull
            where TAction : class, IRepositoryEditAction<T, TKey>
        {
            builder.Services.AddTransient<IRepositoryEditAction<T, TKey>, TAction>();
            return builder;
        }

        public static IRepositoryBuilder<T, TKey> SetDefaultUiRoot<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder)
            where TKey : notnull
        {
            AppInternalSettings.Instance.RootName = typeof(T).Name;
            return builder;
        }
        public static IRepositoryBuilder<T, TKey> DoNotExposeInUi<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder)
            where TKey : notnull
        {
            AppInternalSettings.Instance.NotExposableRepositories.Add(typeof(T).Name);
            return builder;
        }
        private static RepositoryAppMenuItem GetAppMenuSettings<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder)
            where TKey : notnull
        {
            var name = typeof(T).Name.ToLower();
            if (!AppInternalSettings.Instance.RepositoryAppMenuItems.ContainsKey(name))
                AppInternalSettings.Instance.RepositoryAppMenuItems.Add(name,
                    RepositoryAppMenuItem.CreateDefault(typeof(T), typeof(TKey)));
            return AppInternalSettings.Instance.RepositoryAppMenuItems[name];
        }
        public static IRepositoryBuilder<T, TKey> ExposeFor<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder, int index)
            where TKey : notnull
        {
            builder.GetAppMenuSettings().Index = index;
            return builder;
        }
        public static IRepositoryBuilder<T, TKey> WithIcon<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder, string icon)
            where TKey : notnull
        {
            builder.GetAppMenuSettings().Icon = icon;
            return builder;
        }
        public static IRepositoryBuilder<T, TKey> WithName<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder, string name)
            where TKey : notnull
        {
            builder.GetAppMenuSettings().Name = name;
            return builder;
        }
        public static IRepositoryBuilder<T, TKey> MapPropertiesForUi<T, TKey, TUiMapper>(
            this IRepositoryBuilder<T, TKey> builder)
            where TKey : notnull
            where TUiMapper : class, IRepositoryUiMapper<T, TKey>
        {
            builder.Services.AddSingleton<IRepositoryPropertyUiHelper<T, TKey>, PropertyUiHelper<T, TKey>>();
            builder.Services.AddSingleton<IRepositoryUiMapper<T, TKey>, TUiMapper>();
            builder.Services.AddSingleton<IRepositoryPropertyUiMapper<T, TKey>, PropertyUiMapper<T, TKey>>();
            return builder;
        }
        public static IRepositoryBuilder<T, TKey> WithLocalization<T, TKey, TLocalization>(
            this IRepositoryBuilder<T, TKey> builder)
            where TKey : notnull
            where TLocalization : IStringLocalizer
        {
            RepositoryLocalizationOptions.Instance.LocalizationInterfaces.Add(typeof(T).FullName!, typeof(TLocalization));
            return builder;
        }
    }
}
