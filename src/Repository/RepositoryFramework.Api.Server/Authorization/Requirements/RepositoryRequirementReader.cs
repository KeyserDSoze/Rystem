namespace RepositoryFramework.Api.Server.Authorization
{
    public sealed class RepositoryRequirementReader
    {
        public object? Key { get; set; }
        public object? Value { get; set; }
        public RepositoryMethods Method { get; set; }
        internal static readonly Dictionary<string, RepositoryMethods> Methods = new()
        {
            { $"/{RepositoryMethods.Get}", RepositoryMethods.Get },
            { $"/{RepositoryMethods.Exist}", RepositoryMethods.Exist },
            { $"/{RepositoryMethods.Query}", RepositoryMethods.Query },
            { $"/{RepositoryMethods.Operation}", RepositoryMethods.Operation },
            { $"/{RepositoryMethods.Insert}", RepositoryMethods.Insert },
            { $"/{RepositoryMethods.Update}", RepositoryMethods.Update },
            { $"/{RepositoryMethods.Delete}", RepositoryMethods.Delete },
            { $"/{RepositoryMethods.Batch}", RepositoryMethods.Batch },
        };
    }
}
