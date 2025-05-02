using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using RepositoryFramework.InMemory;
using Rystem.Api.Test.Domain;
using Rystem.Api.TestServer.Clients;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
     .AddMicrosoftIdentityWebApi(options =>
     {
         builder.Configuration.Bind("AzureAd", options);
         options.TokenValidationParameters.NameClaimType = "name";
     },
    options =>
    {
        builder.Configuration.Bind("AzureAd", options);
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IColam, Comad>();
builder.Services.AddFactory<ISalubry, Salubry>();
builder.Services.AddFactory<ISalubry, Salubry2>("Doma");
builder.Services.AddFactory<ITeamCalculator, TeamCalculator>();
builder.Services.AddFactory<IEmbeddingService, EmbeddingService1>(EmbeddingType.First);
builder.Services.AddFactory<IEmbeddingService, EmbeddingService2>(EmbeddingType.Second);
builder.Services.AddRepository<Container, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();
});
builder.Services.AddServerIntegrationForRystemApi(x =>
{
    x.HasScalar = true;
    x.HasSwagger = true;
});
builder.Services.AddBusiness();
builder.Services.AddAuthorization(x =>
{
    x.AddPolicy("policy", t =>
    {
        t.RequireClaim("name");
    });
});
builder.Services.AddApiFromRepositoryFramework()
    .WithSwagger()
    .WithDefaultCorsWithAllOrigins();
var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app
    .UseEndpointApi()
    .UseEndpointApiModels();
app.UseApiFromRepositoryFramework()
    .SetPolicyForAll()
    .With("policy")
    .Build();
//app.MapPost("/handle-file", async ([FromForm] IFormFile myFile, [FromForm] IFormFile myFile2) =>
//{
//    var tempfile = Path.GetTempFileName();
//    await using var stream = File.OpenWrite(tempfile);
//    await myFile.CopyToAsync(stream);
//});
app.MapGet("/handle2/{param:int}", async (int param) =>
{
    return true;
});

app.Run();
