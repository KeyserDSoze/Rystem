using System.Text;

namespace DocsHelper
{
    public static class ProjectOrdering
    {
        public static readonly Dictionary<string, int> OrderBy = new Dictionary<string, int>
        {
                { "Rystem", 0 },
                { "Rystem.DependencyInjection", 1 },
                { "Rystem.DependencyInjection.Web", 2 },
                { "Rystem.Concurrency", 2 },
                { "Rystem.RepositoryFramework.Abstractions", 2 },
                { "Rystem.Content.Abstractions", 2 },
                { "Rystem.Api", 2 },
                { "Rystem.Authentication.Social.Abstractions", 2 },
                { "Rystem.Test.XUnit", 3 },
                { "Rystem.BackgroundJob", 3 },
                { "Rystem.Concurrency.Redis", 3 },
                { "Rystem.RepositoryFramework.Api.Client", 3 },
                { "Rystem.RepositoryFramework.Api.Server", 3 },
                { "Rystem.RepositoryFramework.Infrastructure.InMemory", 3 },
                { "Rystem.RepositoryFramework.MigrationTools", 3 },
                { "Rystem.RepositoryFramework.Cache", 3 },
                { "Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql", 3 },
                { "Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob", 3 },
                { "Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table", 3 },
                { "Rystem.RepositoryFramework.Infrastructure.Dynamics.Dataverse", 3 },
                { "Rystem.RepositoryFramework.Infrastructure.EntityFramework", 3 },
                { "Rystem.RepositoryFramework.Infrastructure.MsSql", 3 },
                { "Rystem.RepositoryFramework.Web.Components", 3 },
                { "Rystem.Content.Infrastructure.Storage.Blob", 3 },
                { "Rystem.Content.Infrastructure.Storage.File", 3 },
                { "Rystem.Content.Infrastructure.InMemory", 3 },
                { "Rystem.Content.Infrastructure.M365.Sharepoint", 3 },
                { "Rystem.Api.Client", 3 },
                { "Rystem.Api.Server", 3 },
                { "Rystem.Authentication.Social", 3 },
                { "Rystem.Authentication.Social.Blazor", 3 },
                { "Rystem.Queue", 4 },
                { "Rystem.Api.Client.Authentication.BlazorServer", 4 },
                { "Rystem.Api.Client.Authentication.BlazorWasm", 4 },
                { "Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer", 4 },
                { "Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm", 4 },
                { "Rystem.RepositoryFramework.Cache.Azure.Storage.Blob", 4 },
                //{ "Rystem.Extensions.Localization.Multiple", 0 },
        };
        public static readonly Dictionary<string, string> MenuBy = new Dictionary<string, string>
        {
                { "Rystem", "Core" },
                { "Rystem.DependencyInjection", "Core" },
                { "Rystem.DependencyInjection.Web", "Core" },
                { "Rystem.Concurrency", "Core" },
                { "Rystem.RepositoryFramework.Abstractions", "Repository Framework" },
                { "Rystem.Content.Abstractions", "Content" },
                { "Rystem.Api", "Api" },
                { "Rystem.Authentication.Social.Abstractions", "Social Authentication" },
                { "Rystem.Test.XUnit", "Core" },
                { "Rystem.BackgroundJob", "Core" },
                { "Rystem.Concurrency.Redis", "Core" },
                { "Rystem.RepositoryFramework.Api.Client",  "Repository Framework" },
                { "Rystem.RepositoryFramework.Api.Server",  "Repository Framework" },
                { "Rystem.RepositoryFramework.Infrastructure.InMemory",  "Repository Framework" },
                { "Rystem.RepositoryFramework.MigrationTools",  "Repository Framework" },
                { "Rystem.RepositoryFramework.Cache",  "Repository Framework" },
                { "Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql",  "Repository Framework" },
                { "Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob",  "Repository Framework" },
                { "Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table",  "Repository Framework" },
                { "Rystem.RepositoryFramework.Infrastructure.Dynamics.Dataverse",  "Repository Framework" },
                { "Rystem.RepositoryFramework.Infrastructure.EntityFramework",  "Repository Framework" },
                { "Rystem.RepositoryFramework.Infrastructure.MsSql",  "Repository Framework" },
                { "Rystem.RepositoryFramework.Web.Components",  "Repository Framework" },
                { "Rystem.Content.Infrastructure.Storage.Blob", "Content" },
                { "Rystem.Content.Infrastructure.Storage.File", "Content" },
                { "Rystem.Content.Infrastructure.InMemory", "Content" },
                { "Rystem.Content.Infrastructure.M365.Sharepoint", "Content" },
                { "Rystem.Api.Client", "Api" },
                { "Rystem.Api.Server", "Api" },
                { "Rystem.Authentication.Social", "Social Authentication" },
                { "Rystem.Authentication.Social.Blazor", "Social Authentication" },
                { "Rystem.Queue", "Core" },
                { "Rystem.Api.Client.Authentication.BlazorServer", "Api" },
                { "Rystem.Api.Client.Authentication.BlazorWasm", "Api" },
                { "Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer", "Repository Framework" },
                { "Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm", "Repository Framework" },
                { "Rystem.RepositoryFramework.Cache.Azure.Storage.Blob", "Repository Framework" },
                //{ "Rystem.Extensions.Localization.Multiple", 0 },
        };
        public static readonly Dictionary<string, bool> NeedsSeparationByNamespace = new Dictionary<string, bool>
        {
                { "Rystem", true },
                { "Rystem.DependencyInjection", true },
                { "Rystem.DependencyInjection.Web", false },
                { "Rystem.Concurrency", false },
                { "Rystem.RepositoryFramework.Abstractions", true },
                { "Rystem.Content.Abstractions", false },
                { "Rystem.Api", false },
                { "Rystem.Authentication.Social.Abstractions", false },
                { "Rystem.Test.XUnit", false },
                { "Rystem.BackgroundJob", false },
                { "Rystem.Concurrency.Redis", false },
                { "Rystem.RepositoryFramework.Api.Client",  false },
                { "Rystem.RepositoryFramework.Api.Server",  false },
                { "Rystem.RepositoryFramework.Infrastructure.InMemory",  false },
                { "Rystem.RepositoryFramework.MigrationTools",  false },
                { "Rystem.RepositoryFramework.Cache",  false },
                { "Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql",  false},
                { "Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob",  false },
                { "Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table",  false },
                { "Rystem.RepositoryFramework.Infrastructure.Dynamics.Dataverse",  false },
                { "Rystem.RepositoryFramework.Infrastructure.EntityFramework", false },
                { "Rystem.RepositoryFramework.Infrastructure.MsSql", false },
                { "Rystem.RepositoryFramework.Web.Components", false },
                { "Rystem.Content.Infrastructure.Storage.Blob", false },
                { "Rystem.Content.Infrastructure.Storage.File",false },
                { "Rystem.Content.Infrastructure.InMemory", false },
                { "Rystem.Content.Infrastructure.M365.Sharepoint",false },
                { "Rystem.Api.Client", false },
                { "Rystem.Api.Server",false },
                { "Rystem.Authentication.Social",false },
                { "Rystem.Authentication.Social.Blazor", false },
                { "Rystem.Queue", false },
                { "Rystem.Api.Client.Authentication.BlazorServer",false },
                { "Rystem.Api.Client.Authentication.BlazorWasm", false },
                { "Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer", false},
                { "Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm", false},
                { "Rystem.RepositoryFramework.Cache.Azure.Storage.Blob", false },
                //{ "Rystem.Extensions.Localization.Multiple", 0 },
        };
    }
    public sealed class ProjectDocumentationMakerWithoutAi
    {
        private readonly ClassForProjectNavigator _classForProjectNavigator;

        public ProjectDocumentationMakerWithoutAi(ClassForProjectNavigator classForProjectNavigator)
        {
            _classForProjectNavigator = classForProjectNavigator;
        }
        private static readonly string mkdocs = "site_name: Rystem\r\nsite_author: Alessandro Rapiti\r\ncopyright: © 2020\r\ntheme:\r\n  name: mkdocs\r\n  color_mode: dark\r\n  user_color_mode_toggle: true\r\nnav:\r\n  - Home: index.md\r\n  - Concepts: \r\n    - Repository: repository.md";
        public async Task CreateAsync(int? backPath)
        {
            var splittedDirectory = Directory.GetCurrentDirectory().Split('\\');
            var rootDirectory = string.Join('\\', splittedDirectory.Take(splittedDirectory.Length - (backPath ?? 4)));
            var directoryInfo = new DirectoryInfo(rootDirectory);
            var docsDirectory = directoryInfo.GetDirectories().First(x => x.Name == "docs").GetDirectories().First(x => x.Name == "docs");
            var mainDirectory = docsDirectory.Parent;
            var mkDocsFile = mainDirectory.GetFiles().First(x => x.Name == "mkdocs.yml");
            StringBuilder mkDocsCreator = new();
            mkDocsCreator.AppendLine(mkdocs);
            foreach (var projectGroup in _classForProjectNavigator.Projects.Where(x => ProjectOrdering.OrderBy.ContainsKey(x.Name)).OrderBy(x => ProjectOrdering.OrderBy[x.Name]).GroupBy(x => ProjectOrdering.MenuBy[x.Name]))
            {
                mkDocsCreator.AppendLine($"  - {projectGroup.Key}:");
                foreach (var project in projectGroup)
                {
                    var fileName = $"{project.Name}.md";
                    mkDocsCreator.AppendLine($"    - {project.Name}: {fileName}");
                }
            }
            using (var mkDocsFileWriter = new StreamWriter(mkDocsFile.FullName, false))
            {
                await mkDocsFileWriter.WriteAsync(mkDocsCreator.ToString());
                await mkDocsFileWriter.FlushAsync();
            }
            foreach (var project in _classForProjectNavigator.Projects.Where(x => ProjectOrdering.OrderBy.ContainsKey(x.Name)).OrderBy(x => ProjectOrdering.OrderBy[x.Name]))
            {
                var fileName = $"{docsDirectory.FullName}\\{project.Name}.md";
                using var documentFile = new StreamWriter(fileName, false);
                var newProjectReadme = project.ReadMe.Replace("### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)", string.Empty);
                newProjectReadme = newProjectReadme.Replace("## [What is Rystem?](https://github.com/KeyserDSoze/Rystem)", string.Empty);
                newProjectReadme = newProjectReadme.Replace("# Get Started", string.Empty);
                newProjectReadme = newProjectReadme.Replace("## Get Started", string.Empty);
                newProjectReadme = newProjectReadme.Replace("## ", "# ");
                newProjectReadme = newProjectReadme.Replace("### ", "## ");
                newProjectReadme = newProjectReadme.Replace("#### ", "### ");
                await documentFile.WriteAsync(newProjectReadme.Trim());
            }
        }
    }
}
