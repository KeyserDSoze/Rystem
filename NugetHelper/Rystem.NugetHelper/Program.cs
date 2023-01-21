// See https://aka.ms/new-console-template for more information
using Rystem.NugetHelper;
using Rystem.NugetHelper.Engine;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Rystem.Nuget
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1118:Utility classes should not have public constructors", Justification = "Test purpose.")]
    public class Program
    {
        static readonly Regex regexForVersion = new("<Version>[^<]*</Version>");
        static readonly Dictionary<string, string> newVersionOfLibraries = new();
        static VersionType Type = VersionType.Patch;
        static readonly Regex PackageReference = new("<PackageReference[^>]*>");
        static readonly Regex Include = new("Include=");
        static readonly Regex VersionRegex = new(@"Version=\""[^\""]*\""");
        static readonly Regex Repo = new(@"\\repos\\");
        const int AddingValueForVersion = 1;
        public static async Task Main()
        {
            string path = @$"{Repo.Split(Directory.GetCurrentDirectory()).First()}\repos";
            List<string> projectNames = new() { "RepositoryFramework", "RystemV3", "Rystem.Concurrency", "Rystem.BackgroundJob", "Rystem.Queue" };
            var rystemDirectories = new DirectoryInfo(path).GetDirectories().Where(x => projectNames.Contains(x.Name)).ToList();
            Console.WriteLine("Only repository (1) or only Rystem (2) or everything (something else) with (3) you choose every turn if go ahead or not, With (4) go in debug.");
            var line = Console.ReadLine();
            Update? currentUpdateTree = line == "1" ? UpdateConfiguration.OnlyRepositoryTree : (line == "2" ? UpdateConfiguration.OnlyRystemTree : UpdateConfiguration.UpdateTree);
            string? specificVersion = null;
            Console.WriteLine("Do you wanna set a specific version? y for true or something else");
            if (Console.ReadLine() == "y")
            {
                specificVersion = Console.ReadLine();
                if (!specificVersion.ContainsAtLeast(2, '.'))
                    throw new ArgumentException("You set a wrong version");
                Type = VersionType.Specific;
            }
            bool checkIfGoAhead = line == "3";
            bool isDebug = line == "4";
            while (currentUpdateTree != null)
            {
                var context = new LibraryContext("0.0.0");
                foreach (var updateTree in currentUpdateTree.Libraries)
                {
                    foreach (var rystemDirectory in rystemDirectories)
                    {
                        await ReadInDeepAsync(rystemDirectory, context, currentUpdateTree, isDebug, specificVersion);
                    }
                }
                Console.WriteLine($"Current major version is {context.Version.V}");
                foreach (var toUpdate in context.RepoToUpdate)
                {
                    Console.WriteLine($"repo to update {toUpdate}");
                    if (!isDebug)
                        await CommitAndPushAsync(toUpdate, context.Version.V);
                }
                if (checkIfGoAhead)
                {
                    Console.WriteLine("Do you want to go ahead with next step of publish? (0) not ok, (other) ok");
                    if (Console.ReadLine() == "0")
                    {
                        break;
                    }
                }
                if (currentUpdateTree.Son != null)
                    if (!isDebug)
                        await Task.Delay(6 * 60 * 1000);
                currentUpdateTree = currentUpdateTree.Son;
            }
        }
        static async Task ReadInDeepAsync(DirectoryInfo directoryInfo, LibraryContext context, Update update, bool isDebug, string? specificVersion)
        {
            bool fileFound = false;
            foreach (var file in directoryInfo!.GetFiles())
            {
                if (file.Name.EndsWith(".csproj") && update.Libraries.Any(x => $"{x.NormalizedName}.csproj" == file.Name))
                {
                    var library = update.Libraries.First(x => $"{x.NormalizedName}.csproj" == file.Name);
                    if (!newVersionOfLibraries.ContainsKey(library.LibraryName!))
                    {
                        var streamReader = new StreamReader(file.OpenRead());
                        string content = await streamReader.ReadToEndAsync();
                        streamReader.Dispose();
                        if (regexForVersion.IsMatch(content))
                        {
                            var currentVersion = regexForVersion.Match(content).Value;
                            var version = new NugetHelper.Version(regexForVersion.Match(content).Value.Split('>').Skip(1).First().Split('<').First());
                            if (Type != VersionType.Specific)
                                version.NextVersion(Type, AddingValueForVersion);
                            else
                                version.SetVersion(Type, specificVersion);
                            Console.WriteLine($"{file.Name} from {currentVersion} to {version.V}");
                            content = content.Replace(currentVersion, $"<Version>{version.V}</Version>");
                            newVersionOfLibraries.Add(library.LibraryName!, version.V);
                            foreach (var reference in PackageReference.Matches(content).Select(x => x.Value))
                            {
                                var include = Include.Split(reference).Skip(1).First().Trim('"').Split('"').First();
                                if (newVersionOfLibraries.ContainsKey(include) && VersionRegex.IsMatch(reference))
                                {
                                    var newReference = reference.Replace(VersionRegex.Match(reference).Value, $"Version=\"{newVersionOfLibraries[include]}\"");
                                    content = content.Replace(reference, newReference);
                                }
                            }
                            if (context.Version.IsGreater(version))
                                context.Version = version;
                            Console.WriteLine($"{file.FullName} replaced with");
                            Console.WriteLine("------------------------");
                            Console.WriteLine("------------------------");
                            Console.WriteLine(content);
                            Console.WriteLine("------------------------");
                            Console.WriteLine("------------------------");
                            string path = @$"{Repo.Split(file.FullName).First()}\repos\{Repo.Split(file.FullName).Last().Split('\\').First()}";
                            if (!context.RepoToUpdate.Contains(path))
                                context.RepoToUpdate.Add(path);
                            if (!isDebug)
                                await File.WriteAllTextAsync(file.FullName, content);
                        }
                    }
                    fileFound = true;
                    break;
                }
            }
            if (!fileFound)
                foreach (var directory in directoryInfo.GetDirectories().Where(x => x.Name != "bin" && x.Name != "obj"))
                {
                    await ReadInDeepAsync(directory, context, update, isDebug, specificVersion);
                }
        }
        static async Task CommitAndPushAsync(string path, string newVersion)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    WorkingDirectory = path
                },
            };

            process.Start();
            using (var writer = process.StandardInput)
            {
                writer.WriteLine("git init");
                writer.WriteLine("git add .");
                writer.WriteLine($"git commit --author=\"alessandro rapiti <alessandro.rapiti44@gmail.com>\" -m \"new version v.{newVersion}\"");
                writer.WriteLine("git push");
            }
            await process.WaitForExitAsync();
        }
    }
}