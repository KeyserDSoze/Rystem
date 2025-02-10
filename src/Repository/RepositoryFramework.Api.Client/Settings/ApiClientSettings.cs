using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Api.Client
{
    public sealed class ApiClientSettings<T, TKey> : IFactoryOptions
        where TKey : notnull
    {
        public string StartingPath { get; }
        public string? Version { get; }
        public string? Name { get; }
        public string? FactoryName { get; }
        public string GetPath { get; }
        public string ExistPath { get; }
        public string QueryPath { get; }
        public string OperationPath { get; }
        public string InsertPath { get; }
        public string UpdatePath { get; }
        public string DeletePath { get; }
        public string BatchPath { get; }
        public string BootstrapPath { get; }
        public ApiClientSettings(string? startingPath, string? version, string? name, string? factoryName)
        {
            StartingPath = startingPath ?? "api";
            Version = version;
            Name = name;
            FactoryName = factoryName;
            var basePath = $"{StartingPath}/{(string.IsNullOrWhiteSpace(Version) ? string.Empty : $"{Version}/")}{Name ?? typeof(T).Name}/{(string.IsNullOrWhiteSpace(FactoryName) ? string.Empty : $"{FactoryName}/")}";
            if (KeySettings<TKey>.Instance.IsJsonable)
                GetPath = $"{basePath}{nameof(RepositoryMethods.Get)}";
            else
                GetPath = $"{basePath}{nameof(RepositoryMethods.Get)}?key={{0}}";
            if (KeySettings<TKey>.Instance.IsJsonable)
                ExistPath = $"{basePath}{nameof(RepositoryMethods.Exist)}";
            else
                ExistPath = $"{basePath}{nameof(RepositoryMethods.Exist)}?key={{0}}";
            if (KeySettings<TKey>.Instance.IsJsonable)
                DeletePath = $"{basePath}{nameof(RepositoryMethods.Delete)}";
            else
                DeletePath = $"{basePath}{nameof(RepositoryMethods.Delete)}?key={{0}}";
            QueryPath = $"{basePath}{nameof(RepositoryMethods.Query)}/Stream";
            OperationPath = $"{basePath}{nameof(RepositoryMethods.Operation)}?op={{0}}&returnType={{1}}";
            if (KeySettings<TKey>.Instance.IsJsonable)
                InsertPath = $"{basePath}{nameof(RepositoryMethods.Insert)}";
            else
                InsertPath = $"{basePath}{nameof(RepositoryMethods.Insert)}?key={{0}}";
            if (KeySettings<TKey>.Instance.IsJsonable)
                UpdatePath = $"{basePath}{nameof(RepositoryMethods.Update)}";
            else
                UpdatePath = $"{basePath}{nameof(RepositoryMethods.Update)}?key={{0}}";
            BatchPath = $"{basePath}{nameof(RepositoryMethods.Batch)}/Stream";
            BootstrapPath = $"{basePath}{nameof(RepositoryMethods.Bootstrap)}";
        }
    }
}
