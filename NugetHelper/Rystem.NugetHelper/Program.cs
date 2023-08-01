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
        private static readonly Regex s_regexForVersion = new("<Version>[^<]*</Version>");
        private static readonly Dictionary<string, string> s_newVersionOfLibraries = new();
        private static VersionType s_type = VersionType.Patch;
        private static readonly Regex s_packageReference = new("<PackageReference[^>]*>");
        private static readonly Regex s_include = new("Include=");
        private static readonly Regex s_versionRegex = new(@"Version=\""[^\""]*\""");
        private static readonly Regex s_repo = new(@"\\repos\\");
        const int AddingValueForVersion = 1;
        public static async Task Main()
        {
            var splittedDirectory = Directory.GetCurrentDirectory().Split('\\');
            var path = string.Join('\\', splittedDirectory.Take(splittedDirectory.Length - 5));
            Console.WriteLine("Only Repository (1) or only Rystem (2) or only Content (3) or everything (something else) with (4) you choose every turn if go ahead or not, With (5) go in debug.");
            var line = Console.ReadLine();
            var currentUpdateTree = line == "1" ? UpdateConfiguration.OnlyRepositoryTree : (line == "2" ? UpdateConfiguration.OnlyRystemTree : (line == "3" ? UpdateConfiguration.OnlyContentTree : UpdateConfiguration.UpdateTree));
            string? specificVersion = null;
            Console.WriteLine("Do you wanna set a specific version? y for true or something else");
            if (Console.ReadLine() == "y")
            {
                specificVersion = Console.ReadLine();
                if (specificVersion != null && !specificVersion.ContainsAtLeast(2, '.'))
                    throw new ArgumentException("You set a wrong version");
                s_type = VersionType.Specific;
            }
            var checkIfGoAhead = line == "4";
            var isDebug = line == "5";
            while (currentUpdateTree != null)
            {
                var context = new LibraryContext("0.0.0");
                foreach (var updateTree in currentUpdateTree.Libraries)
                {
                    await ReadInDeepAsync(new DirectoryInfo(path), context, currentUpdateTree, isDebug, specificVersion);
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
                if (currentUpdateTree.Son != null && !isDebug)
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
                    if (!s_newVersionOfLibraries.ContainsKey(library.LibraryName!))
                    {
                        var streamReader = new StreamReader(file.OpenRead());
                        var content = await streamReader.ReadToEndAsync();
                        streamReader.Dispose();
                        if (s_regexForVersion.IsMatch(content))
                        {
                            var currentVersion = s_regexForVersion.Match(content).Value;
                            var version = new NugetHelper.Version(s_regexForVersion.Match(content).Value.Split('>').Skip(1).First().Split('<').First());
                            if (s_type != VersionType.Specific)
                                version.NextVersion(s_type, AddingValueForVersion);
                            else
                                version.SetVersion(s_type, specificVersion);
                            Console.WriteLine($"{file.Name} from {currentVersion} to {version.V}");
                            content = content.Replace(currentVersion, $"<Version>{version.V}</Version>");
                            s_newVersionOfLibraries.Add(library.LibraryName!, version.V);
                            foreach (var reference in s_packageReference.Matches(content).Select(x => x.Value))
                            {
                                var include = s_include.Split(reference).Skip(1).First().Trim('"').Split('"').First();
                                if (s_newVersionOfLibraries.TryGetValue(include, out var value) && s_versionRegex.IsMatch(reference))
                                {
                                    var newReference = reference.Replace(s_versionRegex.Match(reference).Value, $"Version=\"{value}\"");
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
                            string path = @$"{s_repo.Split(file.FullName).First()}\repos\{s_repo.Split(file.FullName).Last().Split('\\').First()}";
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
