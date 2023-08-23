using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RepositoryFramework.InMemory;
using RepositoryFramework.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddRepository<WeatherForecast, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder.PopulateWithRandomData();
    });
});

await builder.Build().RunAsync();
