using RepositoryFramework;
using RepositoryFramework.InMemory;
using RepositoryFramework.Web.Test.BlazorApp.Models;
using Whistleblowing.Licensing.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApiFromRepositoryFramework()
    .WithDefaultCors()
    .WithSwagger()
    .WithDocumentation()
    .WithDescriptiveName("Api");

builder.Services
    .AddRepository<AppConfiguration, string>(settings =>
    {
        settings.WithInMemory()
        .PopulateWithRandomData(34, 2);
    });

builder.Services.AddRepository<AppGroup, string>(settings =>
{
    settings.WithInMemory()
    .PopulateWithRandomData(24, 2);
});

builder.Services.AddRepository<Weather, int>(settings =>
{
    settings.WithInMemory().PopulateWithRandomData(5, 2);
});

builder.Services
    .AddRepository<AppUser, int>(settings =>
    {
        settings.WithInMemory()
            .PopulateWithRandomData(67, 2)
            .WithRandomValue(x => x.Value.Groups, async serviceProvider =>
            {
                var repository = serviceProvider.GetService<IRepository<AppGroup, string>>()!;
                return (await repository.ToListAsync().NoContext()).Select(x => new Group()
                {
                    Id = x.Key,
                    Name = x.Value.Name
                });
            });
    });

builder.Services.AddWarmUp(async serviceProvider =>
{
    var repository = serviceProvider.GetService<IRepository<AppUser, int>>();
    if (repository != null)
    {
        await repository.InsertAsync(23, new AppUser
        {
            Email = "23 default",
            Groups = new(),
            Id = 23,
            Name = "23 default",
            Password = "23 default",
            InternalAppSettings = new InternalAppSettings
            {
                Index = 23,
                Maps = new() { "23" },
                Options = "23 default options"
            },
            Settings = new AppSettings
            {
                Color = "23 default",
                Options = "23 default",
                Maps = new() { "23" }
            }
        }).NoContext();
    }
});

var app = builder.Build();
await app.Services.WarmUpAsync();

app.UseHttpsRedirection();
app
    .UseApiFromRepositoryFramework()
    .Build();

app.Run();
