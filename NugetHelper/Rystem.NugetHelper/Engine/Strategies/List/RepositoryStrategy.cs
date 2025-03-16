namespace Rystem.NugetHelper.Engine
{
    internal sealed class RepositoryStrategy : IUpdateStrategy
    {
        public int Value => 2;
        public string Label => "Repository";
        public Package GetStrategy()
        {
            var onlyRepositoryTree = new Package()
            .AddProject("Rystem.RepositoryFramework.Abstractions");
            onlyRepositoryTree
            .CreateSon()
            .AddProject(
                "Rystem.RepositoryFramework.Api.Client", "Rystem.RepositoryFramework.Api.Server",
                "Rystem.RepositoryFramework.Infrastructure.InMemory", "Rystem.RepositoryFramework.MigrationTools",
                "Rystem.RepositoryFramework.Cache", "Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql",
                "Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob", "Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table",
                "Rystem.RepositoryFramework.Infrastructure.Dynamics.Dataverse", "Rystem.RepositoryFramework.Infrastructure.EntityFramework",
                "Rystem.RepositoryFramework.Infrastructure.MsSql", "RepositoryFramework.Web.Components", "Rystem.Localization")
            .CreateSon()
            .AddProject("Rystem.RepositoryFramework.Cache.Azure.Storage.Blob");
            return onlyRepositoryTree;
        }
    }
}
