using Rystem.NugetHelper.Engine;

namespace Rystem.Nuget
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var parameters = new UpdateComposerParameters(args);
            var composer = new UpdateComposer();
            composer.ConfigurePackage(parameters.Package);
            composer.ConfigureVersion(parameters.Versioning);
            composer.Configure(parameters.IsAutomatic, parameters.MinutesToWait, parameters.AddingNumberToCurrentVersion, parameters.SpecificVersion, parameters.IsDebug);
            await composer.ExecuteUpdateAsync(parameters.Path, parameters.GitHubToken);
        }
    }
}
