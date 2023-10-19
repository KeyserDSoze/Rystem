// See https://aka.ms/new-console-template for more information
using Rystem.NugetHelper.Engine;

namespace Rystem.Nuget
{
    public class Program
    {
        public static async Task Main()
        {
            var composer = new UpdateComposer();
            composer.ConfigurePackage();
            composer.ConfigureVersion();
            composer.Configure();
            await composer.ExecuteUpdateAsync();
        }
    }
}
