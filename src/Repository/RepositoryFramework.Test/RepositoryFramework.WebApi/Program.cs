using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using RepositoryFramework;
using RepositoryFramework.Api.Server.Authorization;
using RepositoryFramework.InMemory;
using RepositoryFramework.Test.Domain;
using RepositoryFramework.Test.Infrastructure.EntityFramework;
using RepositoryFramework.Test.Infrastructure.EntityFramework.Models.Internal;
using RepositoryFramework.WebApi;
using RepositoryFramework.WebApi.Models;

var builder = WebApplication.CreateBuilder(args);
IdentityModelEventSource.ShowPII = true;
#pragma warning disable S125
//builder.Services.AddRepositoryInMemoryStorage<User>()
//.PopulateWithRandomData(x => x.Email!, 120, 5)
// Sections of code should not be commented out
//.WithPattern(x => x.Email, @"[a-z]{5,10}@gmail\.com");
//builder.Services.AddRepository<IperUser, string, IperRepositoryStorage>()
var configurationSection = builder.Configuration.GetSection("AzureAd");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(configurationSection);

builder.Services.AddQuery<IperUser, string>(x =>
{
    x
        .WithInMemory();
    x
        .AddBusiness()
        .AddBusinessBeforeInsert<IperRepositoryBeforeInsertBusiness>();
    x
        .WithInMemoryCache(x =>
        {
            x.ExpiringTime = TimeSpan.FromMilliseconds(100_000);
        });
});
builder.Services.AddRepository<NonPlusSuperUser, NonPlusSuperUserKey>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(builder =>
    {
        builder
            .PopulateWithRandomData(120, 5)
            .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
    });
});
builder.Services.AddRepository<NonPlusSuperUser, PlusSuperUserKey>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(builder =>
    {
        builder
            .PopulateWithRandomData(120, 5)
            .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
    }, "something");
});
builder.Services
    .AddRepository<SuperUser, string>(repositoryBuilder =>
    {
        repositoryBuilder.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(120, 5)
                .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
        });
        //repositoryBuilder
        //   .ConfigureSpecificPolicies()
        //   .WithAuthorizationHandler<PolicyHandlerForSuperUser>();
        repositoryBuilder.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(2, 5)
                .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
        }, "inmemory");
        var id = Guid.NewGuid().ToString();
        repositoryBuilder.SetExamples(new SuperUser("alisandro@gmail.com")
        {
            GroupId = Guid.NewGuid(),
            Id = id,
            IsAdmin = true,
            Name = "Test",
            Port = 123
        }, id);
        //repositoryBuilder
        //    .AddPolicies()
        //    .WithHandler<PolicyHandlerForSuperUser>();
    });

builder.Services.AddRepository<SuperiorUser, string>(settings =>
{
    settings.WithInMemory(builder =>
    {
        builder
            .PopulateWithRandomData(120, 5)
            .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com")
            .WithPattern(x => x.Value!.Port, @"[1-9]{3,4}");
    });
    settings.SetNotExposable();
});

//builder.Services.AddRepository<Animal, AnimalKey>(settings => settings.WithInMemory());
//builder.Services.AddRepository<Car, Guid>(settings => settings.WithInMemory());
//builder.Services.AddRepository<Car2, Range>(settings => settings.WithInMemory());
//builder.Services
//    .AddUserRepositoryWithDatabaseSqlAndEntityFramework(builder.Configuration);
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("Repository Api")
    .WithSwagger()
    .WithMapApi()
    .WithModelsApi()
    .WithDocumentation()
    .WithDefaultCorsWithAllOrigins();
builder.Services.ScanAuthorizationForRepositoryFramework();
////.ConfigureAzureActiveDirectory(builder.Configuration);

//builder.Services
//    .AddAuthorization()
//    .AddServerSideBlazor(opts => opts.DetailedErrors = true)
//    .AddMicrosoftIdentityConsentHandler();
////builder.Services
////    .AddRepositoryInTableStorage<User, string>(builder.Configuration["ConnectionString:Storage"]);
//builder.Services.AddRepository<BigAnimal, int>(
//    settings =>
//    {
//        settings.WithBlobStorage(x =>
//        {
//            x.Settings.ConnectionString = builder.Configuration["ConnectionString:Storage"];
//        });
//    }
//    );

//builder.Services.AddStackExchangeRedisCache(options =>
//{
//    options.Configuration = builder.Configuration["ConnectionString:Redis"];
//    options.InstanceName = "SampleInstance";
//});
//builder.Services
//    .AddRepository<ReinforcedUser, string>(repositoryBuilder =>
//    {
//        repositoryBuilder
//        .WithBlobStorage(storageBuilder =>
//        {
//            storageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionString:Storage"];
//        });
//        repositoryBuilder
//        .WithInMemoryCache(x =>
//        {
//            x.ExpiringTime = TimeSpan.FromSeconds(60);
//            x.Methods = RepositoryMethods.Get | RepositoryMethods.Insert | RepositoryMethods.Update | RepositoryMethods.Delete;
//        })
//        .WithBlobStorageCache(
//            x =>
//            {
//                x.Settings.ConnectionString = builder.Configuration["ConnectionString:Storage"];
//            }
//            , x =>
//            {
//                x.ExpiringTime = TimeSpan.FromSeconds(120);
//                x.Methods = RepositoryMethods.All;
//            });
//    });

//builder.Services
//    .AddRepository<CreativeUser, string>(settings =>
//    {
//        settings.WithCosmosSql(x =>
//        {
//            x.Settings.ConnectionString = builder.Configuration["ConnectionString:CosmosSql"];
//            x.Settings.DatabaseName = "BigDatabase";
//            x.WithId(x => x.Email!);
//        });
//    });
builder.Services.ScanBusinessForRepositoryFramework();
#pragma warning restore S125 // Sections of code should not be commented out

var app = builder.Build();
await app.Services.WarmUpAsync();
app.UseAuthentication();
app.UseAuthorization();
//app.UseHttpsRedirection();
app
    .UseApiFromRepositoryFramework<WebApplication, SuperUser, string>("inmemory")
    .SetPolicy(RepositoryMethods.All)
    .With("aa")
    .Build();
app
    .UseApiFromRepositoryFramework()
    .WithNoAuthorization();
//.WithDefaultAuthorization();

//app.UseAuthentication();
//app.UseAuthorization();
//.SetPolicy(RepositoryMethod.Query)
//.Empty()
//.SetPolicy(RepositoryMethod.Delete)
//.With("Admin")
//.With("Other")
//.And()
//.Finalize();
//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapHealthChecks("/healthz");
//    endpoints.UseApiFromRepository<SuperUser>()
//        .SetPolicyForCommand()
//        .With("SuperAdmin")
//        .Build();
//    endpoints.UseApiFromRepositoryFramework()
//        .SetPolicyForAll()
//        .With("NormalUser")
//        .And()
//        .SetPolicy(RepositoryMethods.Insert)
//        .With("SuperAdmin")
//        .And()
//        .SetPolicy(RepositoryMethods.Update)
//        .With("SuperAdmin")
//        .Build();
//    endpoints
//        .MapControllers();
//});
app.UseApiFromRepositoryFramework()
        .WithNoAuthorization();
IdentityModelEventSource.ShowPII = true;
app.Run();
