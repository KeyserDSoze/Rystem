namespace Rystem.NugetHelper.Engine
{
    internal sealed class RystemStrategy : IUpdateStrategy
    {
        public int Value => 0;
        public string Label => "All packages";
        public Package GetStrategy()
        {
            var updateTree = new Package()
            .AddProject("Rystem");
            updateTree
                .CreateSon()
            .AddProject("Rystem.DependencyInjection")
            .CreateSon()
            .AddProject("Rystem.DependencyInjection.Web", "Rystem.Concurrency", "Rystem.RepositoryFramework.Abstractions", "Rystem.Content.Abstractions", "Rystem.Api", "Rystem.Authentication.Social.Abstractions")
            .CreateSon()
            .AddProject("Rystem.BackgroundJob", "Rystem.Concurrency.Redis", "Rystem.Test.XUnit",
                        "Rystem.RepositoryFramework.Api.Client", "Rystem.RepositoryFramework.Api.Server",
                        "Rystem.RepositoryFramework.Infrastructure.InMemory", "Rystem.RepositoryFramework.MigrationTools",
                        "Rystem.RepositoryFramework.Cache", "Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql",
                        "Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob", "Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table",
                        "Rystem.RepositoryFramework.Infrastructure.Dynamics.Dataverse", "Rystem.RepositoryFramework.Infrastructure.EntityFramework",
                        "Rystem.RepositoryFramework.Infrastructure.MsSql", "RepositoryFramework.Web.Components", "Rystem.Content.Infrastructure.Azure.Storage.Blob",
                        "Rystem.Content.Infrastructure.InMemory", "Rystem.Content.Infrastructure.M365.Sharepoint",
                        "Rystem.Content.Infrastructure.Azure.Storage.File", "Rystem.Api.Server", "Rystem.Api.Client",
                        "Rystem.Authentication.Social", "Rystem.Authentication.Social.Blazor", "rystem.authentication.social.react")
            .CreateSon()
            .AddProject("Rystem.Queue", "Rystem.RepositoryFramework.Cache.Azure.Storage.Blob",
                         "Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer",
                         "Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm",
                         "Rystem.Api.Client.Authentication.BlazorServer", "Rystem.Api.Client.Authentication.BlazorWasm");
            return updateTree;
        }
    }
}
