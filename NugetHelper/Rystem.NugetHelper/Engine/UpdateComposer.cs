﻿using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rystem.NugetHelper.Engine
{
    internal sealed class UpdateComposer
    {
        private Package? _choosenStrategy;
        private VersionType _versionType;
        private bool _pauseEachStep;
        private bool _isDebug;
        private string? _specificVersion;
        private int _minutesToWait = 5;
        private int _addingValueForVersion = 1;
        private readonly Dictionary<string, string> _newVersionOfLibraries = new();
        private static readonly Regex s_regexForVersion = new("<Version>[^<]*</Version>");
        private static readonly Regex s_packageReference = new("<PackageReference[^>]*>");
        private static readonly Regex s_include = new("Include=");
        private static readonly Regex s_versionRegex = new(@"Version=\""[^\""]*\""");
        public void ConfigurePackage(int? forcedPackage)
        {
            if (forcedPackage == null)
            {
                Console.WriteLine("Do you want to update?");
                foreach (var strategy in UpdateStrategyList.Updates)
                    Console.WriteLine($"{strategy.Value}) {strategy.Label}");
                Console.WriteLine();
                forcedPackage = int.Parse(Console.ReadLine()!);
            }
            _choosenStrategy = UpdateStrategyList.Updates.Find(x => x.Value == forcedPackage)!.GetStrategy();
        }
        public void ConfigureVersion(VersionType? chosenVersion)
        {
            if (chosenVersion == null)
            {
                Console.WriteLine("Do you want to update? (as default the patch)");
                foreach (var value in Enum.GetValues(typeof(VersionType)))
                {
                    Console.WriteLine($"{(int)value}) {value}");
                }
                var chosenVersionFromInput = Console.ReadLine();
                if (int.TryParse(chosenVersionFromInput, out var number))
                    _versionType = (VersionType)number;
            }
            else
                _versionType = chosenVersion.Value;
        }
        public void Configure(bool isAutomatic, int? minutesToWait, int? addingNumberToCurrentVersion, string? specificVersion, bool? isDebug)
        {
            if (_versionType == VersionType.Specific)
            {
                if (specificVersion == null)
                {
                    Console.WriteLine("Insert your new version");
                    _specificVersion = Console.ReadLine();
                }
                else
                    _specificVersion = specificVersion;
                if (_specificVersion != null && !_specificVersion.ContainsAtLeast(2, '.'))
                    throw new ArgumentException("You set a wrong version");
            }
            if (!isAutomatic)
            {
                Console.WriteLine("Do you want to pause each step? y or everythingelse");
                _pauseEachStep = Console.ReadLine() == "y";
            }
            if (isDebug != null)
                _isDebug = isDebug.Value;
            else if (!isAutomatic)
            {
                Console.WriteLine("Do you want to run in debug? y or everythingelse");
                _isDebug = Console.ReadLine() == "y";
            }
            if (addingNumberToCurrentVersion == null && !isAutomatic)
            {
                Console.WriteLine("Do you want to change version by 1 or ? (1) is default");
                var versionAdder = Console.ReadLine();
                if (int.TryParse(versionAdder, out var number))
                    _addingValueForVersion = number;
            }
            else if (addingNumberToCurrentVersion != null)
                _addingValueForVersion = addingNumberToCurrentVersion.Value;
            if (minutesToWait == null && !isAutomatic)
            {
                Console.WriteLine("Do you want to wait for each round 5 minutes as default or?");
                var minutesToWaitFromUser = Console.ReadLine();
                if (int.TryParse(minutesToWaitFromUser, out var minutes))
                    _minutesToWait = minutes;
            }
            else if (minutesToWait != null)
                _minutesToWait = minutesToWait.Value;
        }
        public async Task ExecuteUpdateAsync(string? githubToken, int? backPath)
        {
            var library = _choosenStrategy;
            while (library != null)
            {
                var splittedDirectory = Directory.GetCurrentDirectory().Split('\\');
                var path = string.Join('\\', splittedDirectory.Take(splittedDirectory.Length - (backPath ?? 5)));
                var context = new LibraryContext("0.0.0");
                foreach (var updateTree in library.Libraries)
                {
                    await ReadInDeepAndChangeAsync(new DirectoryInfo(path), context, library, _isDebug, _specificVersion);
                }
                Console.WriteLine($"Current major version is {context.Version.V}");
                Console.WriteLine($"repo to update {path}");
                if (!_isDebug)
                    await CommitAndPushAsync(path, context.Version.V, githubToken);
                else
                    Console.WriteLine($"Commit for toUpdate: {path} with version: {context.Version.V} and token is not null: {(githubToken != null)}");
                if (_pauseEachStep)
                {
                    Console.WriteLine("Do you want to go ahead with next step of publish? (0) not ok, (other) ok");
                    if (Console.ReadLine() == "0")
                    {
                        break;
                    }
                }
                if (library.Son != null && _minutesToWait > 0)
                    await Task.Delay(_minutesToWait * 60 * 1000);
                library = library.Son;
            }
        }
        async Task ReadInDeepAndChangeAsync(DirectoryInfo directoryInfo, LibraryContext context, Package update, bool isDebug, string? specificVersion)
        {
            var fileFound = false;
            foreach (var file in directoryInfo!.GetFiles())
            {
                if (file.Name.EndsWith(".csproj") && update.Libraries.Any(x => $"{x.NormalizedName}.csproj" == file.Name))
                {
                    var library = update.Libraries.First(x => $"{x.NormalizedName}.csproj" == file.Name);
                    if (!_newVersionOfLibraries.ContainsKey(library.LibraryName!))
                    {
                        var streamReader = new StreamReader(file.OpenRead());
                        var content = await streamReader.ReadToEndAsync();
                        streamReader.Dispose();
                        if (s_regexForVersion.IsMatch(content))
                        {
                            var currentVersion = s_regexForVersion.Match(content).Value;
                            var version = new Version(s_regexForVersion.Match(content).Value.Split('>').Skip(1).First().Split('<').First());
                            if (_versionType != VersionType.Specific)
                                version.NextVersion(_versionType, _addingValueForVersion);
                            else
                                version.SetVersion(_versionType, specificVersion!);
                            Console.WriteLine($"{file.Name} from {currentVersion} to {version.V}");
                            content = content.Replace(currentVersion, $"<Version>{version.V}</Version>");
                            _newVersionOfLibraries.Add(library.LibraryName!, version.V);
                            foreach (var reference in s_packageReference.Matches(content).Select(x => x.Value))
                            {
                                var include = s_include.Split(reference).Skip(1).First().Trim('"').Split('"').First();
                                if (_newVersionOfLibraries.TryGetValue(include, out var value) && s_versionRegex.IsMatch(reference))
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
                    await ReadInDeepAndChangeAsync(directory, context, update, isDebug, specificVersion);
                }
        }
        async Task CommitAndPushAsync(string path, string newVersion, string? githubToken)
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
                if (githubToken != null)
                    writer.WriteLine($"git remote set-url origin https://KeyserDSoze:{githubToken}@github.com/KeyserDSoze/Rystem.git");
                writer.WriteLine("git add .");
                writer.WriteLine($"git commit --author=\"alessandro rapiti <alessandro.rapiti44@gmail.com>\" -m \"new version v.{newVersion}\"");
                writer.WriteLine("git push --set-upstream origin master");
            }
            await process.WaitForExitAsync();
        }
    }
}
