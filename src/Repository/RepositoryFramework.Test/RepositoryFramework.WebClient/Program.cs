﻿using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using RepositoryFramework.Test.Domain;
using RepositoryFramework.WebClient.Data;

var builder = WebApplication.CreateBuilder(args);

var scopes = builder.Configuration["AzureAd:Scopes"];
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
               .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
               .EnableTokenAcquisitionToCallDownstreamApi(scopes.Split(' '))
               .AddInMemoryTokenCaches();
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
var retryPolicy = HttpPolicyExtensions
  .HandleTransientHttpError()
  .Or<TimeoutRejectedException>()
  .RetryAsync(3);

builder.Services
    .AddRepository<CreativeUser, int>(settings =>
    {
        settings
            .WithApiClient(apiBuilder =>
            {
                apiBuilder
                .WithHttpClient("localhost:7058")
                .ClientBuilder
                    .AddPolicyHandler(retryPolicy);
            });
    })
    .AddRepository<SuperUser, string>(settings =>
    {
        settings.WithApiClient(apiBuilder =>
        {
            apiBuilder
            .WithHttpClient("localhost:7058");
        });
    })
    .AddRepository<IperUser, string>(settings =>
    {
        settings.WithApiClient(apiBuilder =>
        {
            apiBuilder
            .WithHttpClient("localhost:7058");
        });
    })
    .AddRepository<Animal, AnimalKey>(settings =>
    {
        settings.WithApiClient(apiBuilder =>
        {
            apiBuilder
            .WithHttpClient("localhost:7058");
        });
    })
    .AddRepository<AppUser, AppUserKey>(settings =>
    {
        settings.WithApiClient(apiBuilder =>
        {
            apiBuilder
            .WithHttpClient("localhost:7058");
        });
    })
    .AddRepository<Car, Guid>(settings =>
    {
        settings.WithApiClient(apiBuilder =>
        {
            apiBuilder
            .WithHttpClient("localhost:7058");
        });
    })
    .AddRepository<Car2, Range>(settings =>
    {
        settings.WithApiClient(apiBuilder =>
        {
            apiBuilder
            .WithHttpClient("localhost:7058");
        });
    });

builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient(settings =>
{
    settings.Scopes = builder.Configuration["AzureAd:Scopes"].Split(' ');
});

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();
builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
