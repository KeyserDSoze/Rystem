using System.ClientModel;
using System.Text;
using System.Text.RegularExpressions;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace DocsHelper
{
    public sealed class Project
    {
        public required string Id { get; set; }
        public required List<Class> Classes { get; set; }
        public required List<TestClass> TestClasses { get; set; }
        public required List<OnlinePackageDependency> PackageDependencies { get; set; }
        public required List<OnlinePackageDependency> ProjectDependencies { get; set; }
        public required string Name { get; set; }
    }
    public sealed class TestProject
    {
        public required string Id { get; set; }
        public required List<Class> Classes { get; set; }
        public required List<OnlinePackageDependency> PackageDependencies { get; set; }
    }
    public sealed class Class
    {
        public required string Text { get; set; }
        public required string Type { get; set; }
        public required string Path { get; set; }
        public required string Namespace { get; set; }
    }
    public sealed class OnlinePackageDependency
    {
        public required string Id { get; set; }
        public required string MinimumVersion { get; set; }
    }
    public sealed class TestClass
    {
        public required string Text { get; set; }
    }
    public sealed class ProjectDocumentationMaker
    {
        private readonly ClassForProjectNavigator _classForProjectNavigator;
        private readonly IConfiguration _configuration;

        public ProjectDocumentationMaker(ClassForProjectNavigator classForProjectNavigator, IConfiguration configuration)
        {
            _classForProjectNavigator = classForProjectNavigator;
            _configuration = configuration;
        }
        private readonly Dictionary<string, string> _packageDocumentation = new();
        private readonly Dictionary<string, int> OrderBy = new Dictionary<string, int>
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
        private readonly Dictionary<string, string> MenuBy = new Dictionary<string, string>
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
        private readonly Dictionary<string, bool> NeedsSeparationByNamespace = new Dictionary<string, bool>
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

        private static readonly SystemChatMessage s_systemChatMessage = new("You are an expert in software development and documentation. Your task is to generate comprehensive usability documentation for a library based on the provided classes and their associated public methods. You will create detailed descriptions, including the purpose of each method, its parameters, return types, and possible use cases, following the input format below:\r\n\r\n1. **Classes**: \r\n   - You will receive a list of classes and their methods enclosed between triple backticks. Each class will have public methods that need documentation. Your output must be organized per class, providing:\r\n     - **Method Name**: A description of what the method does.\r\n     - **Parameters**: List and explain each parameter, including type and purpose.\r\n     - **Return Value**: What the method returns and its type.\r\n     - **Usage Example**: Provide at least one practical use case for the method, formatted as code.\r\n\r\n2. **Test Classes**:\r\n   - After the class definitions, you will receive test classes separated by three vertical bars `|||`. The tests provide further context on how the methods are used. Incorporate relevant information from the test classes to enrich your documentation, such as edge cases or additional examples of usage.\r\n\r\nFollow these principles:\r\n- Use clear, concise, and user-friendly language.\r\n- Ensure the documentation is suitable for developers who are new to the library.\r\n- Organize the content logically, with each class and its methods clearly separated.\r\n- Enhance each method’s description based on its associated tests where applicable.\r\n- Write everything in markdown");
        private static readonly string mkdocs = "site_name: Rystem\r\nsite_author: Alessandro Rapiti\r\ncopyright: © 2020\r\ntheme:\r\n  name: mkdocs\r\n  color_mode: dark\r\n  user_color_mode_toggle: true\r\nnav:\r\n  - Home: index.md";
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
            foreach (var projectGroup in _classForProjectNavigator.Projects.Where(x => OrderBy.ContainsKey(x.Name)).OrderBy(x => OrderBy[x.Name]).GroupBy(x => MenuBy[x.Name]))
            {
                mkDocsCreator.AppendLine($"  - {projectGroup.Key}:");
                foreach (var project in projectGroup)
                {
                    if (NeedsSeparationByNamespace[project.Name])
                    {
                        mkDocsCreator.AppendLine($"    - {project.Name}:");
                        foreach (var @namespace in project.Classes.GroupBy(x => x.Namespace))
                        {
                            var fileName = $"{project.Name}\\{@namespace.Key}.md";
                            mkDocsCreator.AppendLine($"      - {@namespace.Key}: {fileName}");
                        }
                    }
                    else
                    {
                        var fileName = $"{project.Name}.md";
                        mkDocsCreator.AppendLine($"    - {project.Name}: {fileName}");
                    }
                }
            }
            using (var mkDocsFileWriter = new StreamWriter(mkDocsFile.FullName, false))
            {
                await mkDocsFileWriter.WriteAsync(mkDocsCreator.ToString());
                await mkDocsFileWriter.FlushAsync();
            }
            var openAiClient = new AzureOpenAIClient(new Uri(_configuration["OpenAi:Endpoint"]), new ApiKeyCredential(_configuration["OpenAi:ApiKey"]));
            var model = _configuration["OpenAi:ModelName"];
            foreach (var project in _classForProjectNavigator.Projects.Where(x => OrderBy.ContainsKey(x.Name)).OrderBy(x => OrderBy[x.Name]))
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"##This nuget package has this name: {project.Name} and uses these libraries:");
                foreach (var library in project.PackageDependencies)
                {
                    stringBuilder.AppendLine($"- {library.Id} with minimum version: {library.MinimumVersion}");
                }
                stringBuilder.AppendLine();
                //stringBuilder.AppendLine("Here i'm putting the current documentation of libraries used by this library:");
                //foreach (var library in project.ProjectDependencies)
                //{
                //    AddFurtherDocumentation(library.Id);
                //    void AddFurtherDocumentation(string id)
                //    {
                //        var docProject = _classForProjectNavigator.Projects.First(x => x.Id == id);
                //        if (_packageDocumentation.TryGetValue(id, out var documentation))
                //        {
                //            stringBuilder.AppendLine($"## {docProject.Name}");
                //            stringBuilder.AppendLine(documentation);
                //        }
                //        foreach (var nextLibrary in docProject.ProjectDependencies)
                //        {
                //            AddFurtherDocumentation(nextLibrary.Id);
                //        }
                //    }
                //}
                //stringBuilder.AppendLine();
                var allDocumentationText = new StringBuilder();
                foreach (var @namespace in NeedsSeparationByNamespace[project.Name] ? project.Classes.GroupBy(x => x.Namespace) : project.Classes.GroupBy(x => string.Empty))
                {
                    var fileName = string.Empty;
                    if (@namespace.Key != string.Empty)
                    {
                        var directoryInfoForNewFile = new DirectoryInfo($"{docsDirectory.FullName}\\{project.Name}");
                        if (!directoryInfoForNewFile.Exists)
                        {
                            directoryInfoForNewFile.Create();
                        }
                        fileName = $"{docsDirectory.FullName}\\{project.Name}\\{@namespace.Key}.md";
                    }
                    else
                    {
                        fileName = $"{docsDirectory.FullName}\\{project.Name}.md";
                    }
                    if (File.Exists(fileName))
                    {
                        allDocumentationText.AppendLine(await File.ReadAllTextAsync(fileName));
                        continue;
                    }
                    var namespaceStringBuilder = new StringBuilder();
                    namespaceStringBuilder.AppendLine(stringBuilder.ToString());
                    namespaceStringBuilder.AppendLine($"Here i'm putting a list of classes from the namespace {@namespace}:");
                    namespaceStringBuilder.AppendLine("```");
                    List<string> namespaces = new();
                    foreach (var @class in @namespace)
                    {
                        namespaceStringBuilder.AppendLine(@class.Text);
                        if (!namespaces.Contains(@class.Namespace))
                            namespaces.Add(@class.Namespace);
                    }
                    namespaceStringBuilder.AppendLine("```");
                    if (namespaceStringBuilder.Length < 12_000)
                    {
                        var testStringBuilder = new StringBuilder();
                        testStringBuilder.AppendLine("|||");
                        foreach (var testClass in _classForProjectNavigator.TestProjects.Where(x => x.PackageDependencies.Any(t => t.Id == project.Id)).SelectMany(x => x.Classes))
                        {
                            if (namespaces.Any(x => testClass.Text.Contains($"using {x}")))
                                testStringBuilder.AppendLine(testClass.Text);
                        }
                        testStringBuilder.AppendLine("|||");
                        if (testStringBuilder.Length < 12_000)
                        {
                            namespaceStringBuilder.AppendLine(testStringBuilder.ToString());
                        }
                    }
                    var chatCompletionsOptions = new ChatCompletionOptions()
                    {
                        MaxOutputTokenCount = 16_000
                    };
                    var messages = new List<ChatMessage>
                    {
                        s_systemChatMessage,
                        new UserChatMessage(namespaceStringBuilder.ToString())
                    };
                    if (namespaceStringBuilder.Length > 40_000)
                    {
                        Console.WriteLine($"Project {project.Id} with namespace {@namespace.Key} too long: {namespaceStringBuilder.Length}");
                        continue;
                    }
                    var chatClient = openAiClient.GetChatClient(model);
                    var requests = chatClient.CompleteChatStreamingAsync(messages, chatCompletionsOptions);
                    var documentationText = new StringBuilder();
                    await foreach (var chatUpdate in requests)
                    {
                        foreach (var contentPart in chatUpdate.ContentUpdate)
                        {
                            documentationText.Append(contentPart.Text);
                        }
                    }
                    using var documentFile = new StreamWriter(fileName, false);
                    await documentFile.WriteAsync(documentationText.ToString());
                    allDocumentationText.AppendLine(documentationText.ToString());
                    await Task.Delay(30_000);
                }
                _packageDocumentation.Add(project.Id, allDocumentationText.ToString());
            }
        }
    }
    public sealed class ClassForProjectNavigator
    {
        public List<Project> Projects { get; } = new();
        public List<TestProject> TestProjects { get; } = new();
        public async Task ExecuteAsync(int? backPath)
        {
            var splittedDirectory = Directory.GetCurrentDirectory().Split('\\');
            var rootDirectory = string.Join('\\', splittedDirectory.Take(splittedDirectory.Length - (backPath ?? 4)));
            rootDirectory = $"{rootDirectory}\\src";
            foreach (var path in new DirectoryInfo(rootDirectory).GetDirectories())
            {
                await ReadInDeepAndChangeAsync(path);
            }
        }
        private static readonly Regex s_packageReference = new("<PackageReference[^>]*>");
        private static readonly Regex s_projectReference = new("<ProjectReference[^>]*>");
        private static readonly Regex s_packageId = new("<PackageId[^<]*<");
        private static readonly Regex s_namespace = new("namespace[^{]*{");
        private async Task ReadTestsAsync(DirectoryInfo directoryInfo, FileInfo csProjectFile)
        {
            var project = new TestProject()
            {
                Id = csProjectFile.Name.Replace(".csproj", string.Empty),
                Classes = [],
                PackageDependencies = [],
            };
            TestProjects.Add(project);
            using var streamReader = new StreamReader(csProjectFile.OpenRead());
            var projectText = await streamReader.ReadToEndAsync();
            var packageReferences = s_projectReference.Matches(projectText);
            foreach (var match in packageReferences)
            {
                var packageSplitted = match.ToString().Split(' ');
                var packageId = packageSplitted[1].Split('"')[1];
                packageId = packageId.Split('\\').Last().Replace(".csproj", string.Empty);
                var package = new OnlinePackageDependency
                {
                    Id = packageId,
                    MinimumVersion = string.Empty
                };
                project.PackageDependencies.Add(package);
            }
            await ReadFileAsync(directoryInfo, directoryInfo);
            async ValueTask ReadFileAsync(DirectoryInfo start, DirectoryInfo directoryInfo)
            {
                foreach (var file in directoryInfo.GetFiles())
                {
                    if (file.Name.EndsWith(".cs"))
                    {
                        var path = file.FullName.Split(start.FullName)[1].Replace(file.Name, string.Empty);
                        var classText = await File.ReadAllTextAsync(file.FullName);
                        var @namespace = s_namespace.Match(classText);
                        if (@namespace.Success)
                        {
                            var theNamespace = @namespace.Value.Split(' ')[1].Split('{')[0].Trim();
                            project.Classes.Add(new Class { Text = classText, Type = ".cs", Path = path, Namespace = theNamespace });
                        }
                    }
                }
                foreach (var directory in directoryInfo.GetDirectories().Where(x => x.Name != "bin" && x.Name != "obj"))
                {
                    await ReadFileAsync(start, directory);
                }
            }
        }
        private async Task ReadInDeepAndChangeAsync(DirectoryInfo directoryInfo)
        {
            if (directoryInfo.GetFiles().Any(t => t.Name == "package.json"))
                return;
            var csProjectFile = directoryInfo!.GetFiles().FirstOrDefault(x => x.Name.EndsWith(".csproj"));
            if (csProjectFile != null)
            {
                if (csProjectFile.FullName.ToLower().Contains("test") && csProjectFile.Name != "Rystem.Test.XUnit.csproj")
                {
                    await ReadTestsAsync(directoryInfo, csProjectFile);
                }
                else
                {
                    using var streamReader = new StreamReader(csProjectFile.OpenRead());
                    var projectText = await streamReader.ReadToEndAsync();
                    var projectNameMatch = s_packageId.Match(projectText);
                    if (projectNameMatch.Success)
                    {
                        var project = new Project()
                        {
                            Id = csProjectFile.Name.Replace(".csproj", string.Empty),
                            Name = projectNameMatch.Value.Split('>')[1].Split('<')[0].Trim('\\'),
                            Classes = [],
                            PackageDependencies = [],
                            TestClasses = [],
                            ProjectDependencies = []
                        };
                        Projects.Add(project);
                        var packageReferences = s_packageReference.Matches(projectText);
                        foreach (var match in packageReferences)
                        {
                            var packageSplitted = match.ToString().Split(' ');
                            var package = new OnlinePackageDependency
                            {
                                Id = packageSplitted[1].Split('"')[1],
                                MinimumVersion = packageSplitted[2].Split('"')[1]
                            };
                            project.PackageDependencies.Add(package);
                        }
                        var projectReferences = s_projectReference.Matches(projectText);
                        foreach (var match in projectReferences)
                        {
                            var projectId = match.ToString().Split('\\').Last().Split(".csproj").First();
                            var package = new OnlinePackageDependency
                            {
                                Id = projectId,
                                MinimumVersion = string.Empty
                            };
                            project.ProjectDependencies.Add(package);
                        }
                        await ReadFileAsync(directoryInfo, directoryInfo);
                        async ValueTask ReadFileAsync(DirectoryInfo start, DirectoryInfo directoryInfo)
                        {
                            foreach (var file in directoryInfo.GetFiles())
                            {
                                var path = file.FullName.Split(start.FullName)[1].Replace(file.Name, string.Empty);
                                if (file.Name.EndsWith(".cs"))
                                {
                                    var classText = await File.ReadAllTextAsync(file.FullName);
                                    if (!classText.Contains("<auto-generated") && !classText.Contains("<autogenerated") &&
                                        (classText.Contains("public static partial class ") ||
                                        classText.Contains("public partial class ") ||
                                        classText.Contains("public static class ") ||
                                        classText.Contains("public class ") ||
                                        classText.Contains("public sealed class ")))
                                    {
                                        var @namespace = s_namespace.Match(classText);
                                        if (@namespace.Success)
                                        {
                                            var theNamespace = @namespace.Value.Split(' ')[1].Split('{')[0].Trim();
                                            project.Classes.Add(new Class { Text = classText, Type = ".cs", Path = path, Namespace = theNamespace });
                                        }
                                    }
                                }
                                else if (file.Name.EndsWith(".cshtml"))
                                {
                                    var classText = await File.ReadAllTextAsync(file.FullName);
                                    project.Classes.Add(new Class { Text = classText, Type = ".cshtml", Path = path, Namespace = $"{project.Id.Trim('\\')}.{path.Trim('\\').Replace("\\", ".")}" });
                                }
                                else if (file.Name.EndsWith(".razor"))
                                {
                                    var classText = await File.ReadAllTextAsync(file.FullName);
                                    project.Classes.Add(new Class { Text = classText, Type = ".razor", Path = path, Namespace = $"{project.Id.Trim('\\')}.{path.Trim('\\').Replace("\\", ".")}" });
                                }
                            }
                            foreach (var directory in directoryInfo.GetDirectories())
                            {
                                await ReadFileAsync(start, directory);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var directory in directoryInfo.GetDirectories().Where(x => x.Name != "bin" && x.Name != "obj"))
                {
                    await ReadInDeepAndChangeAsync(directory);
                }
            }
        }
    }
}
