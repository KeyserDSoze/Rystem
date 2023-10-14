using Microsoft.AspNetCore.Mvc;
using Rystem.Api.TestServer.Clients;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IColam, Comad>();
builder.Services.AddFactory<ISalubry, Salubry>();
builder.Services.AddFactory<ISalubry, Salubry2>("Doma");
builder.Services.AddServerIntegrationForRystemApi();
builder.Services.AddEndpoint<ISalubry>(endpointBuilder =>
{
    endpointBuilder.SetEndpointName("Salubriend");
    endpointBuilder.SetMethodName(x => x.GetAsync, "Gimme");
})
.AddEndpoint<IColam>(endpointBuilder =>
{
    endpointBuilder.SetEndpointName("Comator");
    endpointBuilder.SetMethodName(typeof(IColam).GetMethods().First(), "Cod");
})
.AddEndpoint<ISalubry>(endpointBuilder =>
{
    endpointBuilder
        .SetEndpointName("E")
        .SetMethodName(x => x.GetAsync, "Ra")
        .SetupParameter(x => x.GetAsync, "id", x =>
        {
            x.Location = ApiParameterLocation.Body;
            x.Example = 56;
        });
}, "Doma");
var app = builder.Build();

app.UseHttpsRedirection();
app.UseEndpointApi();
app.MapPost("/handle-file", async ([FromForm] IFormFile myFile, [FromForm] IFormFile myFile2) =>
{
    var tempfile = Path.GetTempFileName();
    await using var stream = File.OpenWrite(tempfile);
    await myFile.CopyToAsync(stream);
});
app.MapGet("/handle2/{param:int}", async (int param) =>
{
    return true;
});
ExecuteAsync();
app.Run();

async Task ExecuteAsync()
{
    var services = new ServiceCollection();
    services.AddEndpoint<ISalubry>(endpointBuilder =>
    {
        endpointBuilder.SetEndpointName("Salubriend");
        endpointBuilder.SetMethodName(x => x.GetAsync, "Gimme");
    })
    .AddEndpoint<IColam>(endpointBuilder =>
    {
        endpointBuilder.SetEndpointName("Comator");
        endpointBuilder.SetMethodName(typeof(IColam).GetMethods().First(), "Cod");
    })
    .AddEndpoint<ISalubry>(endpointBuilder =>
    {
        endpointBuilder
            .SetEndpointName("E")
            .SetMethodName(x => x.GetAsync, "Ra")
            .SetupParameter(x => x.GetAsync, "id", x =>
            {
                x.Location = ApiParameterLocation.Body;
                x.Example = 56;
            });
    }, "Doma");
    services.AddClientsForEndpointApi(x =>
    {
        x.ConfigurationHttpClientForApi(t =>
        {
            t.BaseAddress = new Uri("https://localhost:7117");
        });
    });
    await Task.Delay(10_000);
    var provider = services.BuildServiceProvider().CreateScope().ServiceProvider;
    try
    {
        var salubry = provider.GetRequiredService<IFactory<ISalubry>>().Create();
        var response = await salubry.GetAsync(2, new MemoryStream());
        var colam = provider.GetRequiredService<IColam>();
        var file = new FormFile(new MemoryStream(), 0, 0, "a", "a");
        file.Headers = new HeaderDictionary();
        file.Headers.ContentType = "application/pdf";
        var response2 = await colam.GetAsync("dasdsa", file, "fol", "cul", "cookie", new Faul { Id = "a", Name = "a" }, new Faul { Id = "b", Name = "b" }, new FormFile(new MemoryStream(), 0, 0, "cd", "cd"));
        file = new FormFile(new MemoryStream(), 0, 0, "a", "a");
        file.Headers = new HeaderDictionary();
        file.Headers.ContentType = "application/pdf";
        var response3 = await colam.GetAsync("dasdsa", file, "fol", "cul", "cookie");
    }
    catch (Exception ex)
    {
        string olaf = ex.Message;
    }
}
