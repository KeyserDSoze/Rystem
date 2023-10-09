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
    endpointBuilder.SetMethodName(x => x.GetAsync, "Cod");
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
        endpointBuilder.SetMethodName(x => x.GetAsync, "Cod");
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
    var q = services.ToList();
    services.AddClientsForEndpointApi();
    await Task.Delay(10_000);
    var provider = services.BuildServiceProvider().CreateScope().ServiceProvider;
    try
    {
        var salubry = provider.GetRequiredService<IFactory<ISalubry>>().Create();
        var response = await salubry.GetAsync(2, new MemoryStream());
    }
    catch (Exception ex)
    {
        string olaf = ex.Message;
    }
}
