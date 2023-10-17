using Microsoft.AspNetCore.Mvc;
using Rystem.Api.Test.Domain;
using Rystem.Api.TestServer.Clients;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IColam, Comad>();
builder.Services.AddFactory<ISalubry, Salubry>();
builder.Services.AddFactory<ISalubry, Salubry2>("Doma");
builder.Services.AddServerIntegrationForRystemApi();
builder.Services.AddBusiness();

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

app.Run();
