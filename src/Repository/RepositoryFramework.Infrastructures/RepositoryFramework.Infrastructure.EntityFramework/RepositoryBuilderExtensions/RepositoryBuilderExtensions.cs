﻿using Microsoft.EntityFrameworkCore;
using RepositoryFramework;
using RepositoryFramework.Infrastructure.EntityFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        private static void Check<T, TKey, TEntityModel, TContext>()
        where TKey : notnull
        where TEntityModel : class
        where TContext : DbContext
        {
            if (EntityFrameworkOptions<T, TKey, TEntityModel, TContext>.Instance.DbSet == null)
                throw new ArgumentException($"DbSet not configured in option during {nameof(WithEntityFramework)} method for {typeof(TEntityModel).Name} for model {typeof(T).Name} and key {typeof(TKey).Name}");
        }
        /// <summary>
        /// Add a default Entity Framework service for your repository pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <typeparam name="TEntityModel">Model user for your entity framework integration</typeparam>
        /// <typeparam name="TContext">Specify DB context to use. Please remember to configure it in DI.</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your Entity Framework.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithEntityFramework<T, TKey, TEntityModel, TContext>(
            this IRepositoryBuilder<T, TKey> builder,
            Action<EntityFrameworkOptions<T, TKey, TEntityModel, TContext>> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
            where TEntityModel : class
            where TContext : DbContext
        {
            builder.SetStorageWithOptions<EntityFrameworkRepository<T, TKey, TEntityModel, TContext>,
                EntityFrameworkOptions<T, TKey, TEntityModel, TContext>>(
                options, name, serviceLifetime);
            Check<T, TKey, TEntityModel, TContext>();
            return builder;
        }
        /// <summary>
        /// Add a default Entity Framework service for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <typeparam name="TEntityModel">Model user for your entity framework integration</typeparam>
        /// <typeparam name="TContext">Specify DB context to use. Please remember to configure it in DI.</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your Entity Framework.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static ICommandBuilder<T, TKey> WithEntityFramework<T, TKey, TEntityModel, TContext>(
            this ICommandBuilder<T, TKey> builder,
            Action<EntityFrameworkOptions<T, TKey, TEntityModel, TContext>> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
            where TEntityModel : class
            where TContext : DbContext
        {
            builder.SetStorageWithOptions<EntityFrameworkRepository<T, TKey, TEntityModel, TContext>,
                EntityFrameworkOptions<T, TKey, TEntityModel, TContext>>(
                options, name, serviceLifetime);
            Check<T, TKey, TEntityModel, TContext>();
            return builder;
        }
        /// <summary>
        /// Add a default Entity Framework service for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <typeparam name="TEntityModel">Model user for your entity framework integration</typeparam>
        /// <typeparam name="TContext">Specify DB context to use. Please remember to configure it in DI.</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your Entity Framework.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IQueryBuilder<T, TKey> WithEntityFramework<T, TKey, TEntityModel, TContext>(
            this IQueryBuilder<T, TKey> builder,
            Action<EntityFrameworkOptions<T, TKey, TEntityModel, TContext>> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
            where TEntityModel : class
            where TContext : DbContext
        {
            builder.SetStorageWithOptions<EntityFrameworkRepository<T, TKey, TEntityModel, TContext>,
                EntityFrameworkOptions<T, TKey, TEntityModel, TContext>>(
                options, name, serviceLifetime);
            Check<T, TKey, TEntityModel, TContext>();
            return builder;
        }
    }
}