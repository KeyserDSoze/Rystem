using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Localization;
using Microsoft.Identity.Web;
using RepositoryFramework.Web.Test.BlazorApp.Models;
using RepositoryFramework.Web.Test.BlazorApp.Resources;
using Whistleblowing.Licensing.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services
        .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
        .EnableTokenAcquisitionToCallDownstreamApi(builder.Configuration["AzureAd:Scopes"]!.Split(' '))
        .AddInMemoryTokenCaches();
builder.Services.AddAuthorization();
builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();
builder.Services
    .AddRepositoryUi<ApiConfiguration>(x =>
    {
        x.Name = "SuperSite";
        x.Icon = "savings";
        x.Image = "https://www.pngitem.com/pimgs/m/432-4328680_crime-dashboard-navigation-icon-emblem-hd-png-download.png";
    })
    .WithAuthenticatedUi()
    .AddDefaultSkinForUi()
    .AddDefaultLocalization();

builder.Services.AddApplicationInsightsTelemetry(x =>
{
    x.ConnectionString = "in secrets";
});
builder.Services
    .AddRepository<AppConfiguration, string>(settings =>
    {
        settings
            .WithApiClient()
            .WithHttpClient("localhost:7246");
        settings
            .ExposeFor(3);
    });

builder.Services.AddRepository<AppGroup, string>(settings =>
{
    settings.WithApiClient()
    .WithHttpClient("localhost:7246");
});
builder.Services.AddRepository<Weather, int>(settings =>
{
    settings.WithApiClient()
    .WithHttpClient("localhost:7246");
});

builder.Services
    .AddRepository<AppUser, int>(settings =>
    {
        settings.WithApiClient().WithHttpClient("localhost:7246");
        settings
            .MapPropertiesForUi<AppUser, int, AppUserDesignMapper>()
            .WithIcon("manage_accounts")
            .WithName("User")
            .ExposeFor(2)
            .SetDefaultUiRoot()
            .WithLocalization<AppUser, int, IStringLocalizer<SharedResource>>();
    });

//.WithLocalization<AppUser, int, IStringLocalizer<SharedResource2>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapBlazorHub();
app.AddDefaultRepositoryEndpoints();

app.Run();
