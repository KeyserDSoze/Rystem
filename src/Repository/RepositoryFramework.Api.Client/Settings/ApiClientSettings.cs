namespace RepositoryFramework.Api.Client
{
    public sealed class ApiClientSettings<T, TKey>
        where TKey : notnull
    {
        private readonly KeySettings<TKey> _keySettings;
        public static ApiClientSettings<T, TKey> Instance { get; } = new(new KeySettings<TKey>());
        public string StartingPath { get; private set; } = "api";
        public string? Version { get; private set; }
        public string? Name { get; private set; }
        public string GetPath { get; private set; } = null!;
        public string ExistPath { get; private set; } = null!;
        public string QueryPath { get; private set; } = null!;
        public string OperationPath { get; private set; } = null!;
        public string InsertPath { get; private set; } = null!;
        public string UpdatePath { get; private set; } = null!;
        public string DeletePath { get; private set; } = null!;
        public string BatchPath { get; private set; } = null!;
        private ApiClientSettings(KeySettings<TKey> keySettings)
        {
            _keySettings = keySettings;
            RefreshPath();
        }

        internal void RefreshPath(string? startingPath = null, string? version = null, string? name = null)
        {
            if (startingPath != null)
                StartingPath = startingPath;
            if (version != null)
                Version = version;
            if (name != null)
                Name = name;

            var basePath = $"{StartingPath}/{(string.IsNullOrWhiteSpace(Version) ? string.Empty : $"{Version}/")}{Name ?? typeof(T).Name}/";
            if (_keySettings.IsJsonable)
                GetPath = $"{basePath}{nameof(RepositoryMethods.Get)}";
            else
                GetPath = $"{basePath}{nameof(RepositoryMethods.Get)}?key={{0}}";
            if (_keySettings.IsJsonable)
                ExistPath = $"{basePath}{nameof(RepositoryMethods.Exist)}";
            else
                ExistPath = $"{basePath}{nameof(RepositoryMethods.Exist)}?key={{0}}";
            if (_keySettings.IsJsonable)
                DeletePath = $"{basePath}{nameof(RepositoryMethods.Delete)}";
            else
                DeletePath = $"{basePath}{nameof(RepositoryMethods.Delete)}?key={{0}}";
            QueryPath = $"{basePath}{nameof(RepositoryMethods.Query)}";
            OperationPath = $"{basePath}{nameof(RepositoryMethods.Operation)}?op={{0}}&returnType={{1}}";
            if (_keySettings.IsJsonable)
                InsertPath = $"{basePath}{nameof(RepositoryMethods.Insert)}";
            else
                InsertPath = $"{basePath}{nameof(RepositoryMethods.Insert)}?key={{0}}";
            if (_keySettings.IsJsonable)
                UpdatePath = $"{basePath}{nameof(RepositoryMethods.Update)}";
            else
                UpdatePath = $"{basePath}{nameof(RepositoryMethods.Update)}?key={{0}}";
            BatchPath = $"{basePath}{nameof(RepositoryMethods.Batch)}";
        }
    }
}
