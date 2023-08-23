using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RepositoryFramework.InMemory;
using RepositoryFramework.Wasm;
using RepositoryFramework.Wasm.Services;

RystemTask.WaitYourStartingThread = true;
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddTransient<ISony, Sony>();
builder.Services.AddTransient<IMilly, Milly>();
builder.Services.AddRepository<WeatherForecast, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder.PopulateWithRandomData();
    });
});

await builder.Build().RunAsync();
