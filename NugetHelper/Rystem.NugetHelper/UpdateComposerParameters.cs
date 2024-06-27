// See https://aka.ms/new-console-template for more information
using Rystem.NugetHelper;

namespace Rystem.Nuget
{
    internal sealed class UpdateComposerParameters
    {
        public int? Package { get; set; }
        public VersionType? Versioning { get; set; }
        public bool IsAutomatic { get; set; }
        public int? MinutesToWait { get; set; }
        public int? AddingNumberToCurrentVersion { get; set; }
        public string? SpecificVersion { get; set; }
        public bool? IsDebug { get; set; }
        public string? GitHubToken { get; set; }
        public string Path { get; set; }
        private const string NullString = nameof(NullString);
        public UpdateComposerParameters(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                var parametersAsDictionary = args.ToDictionary(x => x.Split("=").First(), x => x.Split("=").Last());
                if (parametersAsDictionary.ContainsKey(nameof(Package)))
                    Package = int.Parse(parametersAsDictionary[nameof(Package)]);
                if (parametersAsDictionary.ContainsKey(nameof(Versioning)))
                    Versioning = (VersionType)Enum.Parse(typeof(VersionType), parametersAsDictionary[nameof(Versioning)]);
                if (parametersAsDictionary.ContainsKey(nameof(IsAutomatic)))
                    IsAutomatic = bool.Parse(parametersAsDictionary[nameof(IsAutomatic)]);
                if (parametersAsDictionary.ContainsKey(nameof(MinutesToWait)))
                    MinutesToWait = int.Parse(parametersAsDictionary[nameof(MinutesToWait)]);
                if (parametersAsDictionary.ContainsKey(nameof(AddingNumberToCurrentVersion)))
                    AddingNumberToCurrentVersion = int.Parse(parametersAsDictionary[nameof(AddingNumberToCurrentVersion)]);
                if (parametersAsDictionary.ContainsKey(nameof(SpecificVersion)))
                    SpecificVersion = parametersAsDictionary[nameof(SpecificVersion)];
                if (parametersAsDictionary.ContainsKey(nameof(IsDebug)))
                    IsDebug = bool.Parse(parametersAsDictionary[nameof(IsDebug)]);
                if (parametersAsDictionary.ContainsKey(nameof(GitHubToken)))
                    GitHubToken = parametersAsDictionary[nameof(GitHubToken)];
                if (parametersAsDictionary.ContainsKey(nameof(Path)))
                    Path = parametersAsDictionary[nameof(Path)];
                if (SpecificVersion == NullString)
                    SpecificVersion = null;
            }
        }
    }
}
