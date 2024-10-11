using DocsHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.DocsHelper
{
    public class Program
    {
        private sealed class ForUserSecrets
        {
        }
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Create Automatically? y/n");
            var key = Console.ReadLine();
            if (key == "y")
            {
                var services = new ServiceCollection();
                var configuration = new ConfigurationBuilder()
                   .AddJsonFile("appsettings.test.json")
                   .AddUserSecrets<ForUserSecrets>()
                   .Build();
                var classForProjectNavigator = new ClassForProjectNavigator();
                await classForProjectNavigator.ExecuteAsync(null);
                var projectDocumentationMaker = new ProjectDocumentationMaker(classForProjectNavigator, configuration);
                await projectDocumentationMaker.CreateAsync(null);
            }
            else
            {
                var classForProjectNavigator = new ClassForProjectNavigator();
                await classForProjectNavigator.ExecuteAsync(null);
                var projectDocumentationMaker = new ProjectDocumentationMakerWithoutAi(classForProjectNavigator);
                await projectDocumentationMaker.CreateAsync(null);
            }
        }
    }
}
