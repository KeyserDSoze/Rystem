﻿using RepositoryFramework.InMemory;
using Rystem.Authentication.Social.TestApi.Models;
using Rystem.Authentication.Social.TestApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSocialLogin(x =>
{
    x.Google.ClientId = builder.Configuration["SocialLogin:Google:ClientId"];
    x.Google.ClientSecret = builder.Configuration["SocialLogin:Google:ClientSecret"];
    x.Google.RedirectDomain = builder.Configuration["SocialLogin:Google:RedirectDomain"];
    x.Microsoft.ClientId = builder.Configuration["SocialLogin:Microsoft:ClientId"];
    x.Microsoft.ClientSecret = builder.Configuration["SocialLogin:Microsoft:ClientSecret"];
    x.Microsoft.RedirectDomain = builder.Configuration["SocialLogin:Microsoft:RedirectDomain"];
    x.GitHub.ClientId = builder.Configuration["SocialLogin:GitHub:ClientId"];
    x.GitHub.ClientSecret = builder.Configuration["SocialLogin:GitHub:ClientSecret"];
    x.Linkedin.ClientId = builder.Configuration["SocialLogin:Linkedin:ClientId"];
    x.Linkedin.ClientSecret = builder.Configuration["SocialLogin:Linkedin:ClientSecret"];
    x.Linkedin.RedirectDomain = builder.Configuration["SocialLogin:Linkedin:RedirectDomain"];
    x.X.ClientId = builder.Configuration["SocialLogin:X:ClientId"];
    x.X.ClientSecret = builder.Configuration["SocialLogin:X:ClientSecret"];
    x.X.RedirectDomain = builder.Configuration["SocialLogin:X:RedirectDomain"];
    x.Instagram.ClientId = builder.Configuration["SocialLogin:Instagram:ClientId"];
    x.Instagram.ClientSecret = builder.Configuration["SocialLogin:Instagram:ClientSecret"];
    x.Instagram.RedirectDomain = builder.Configuration["SocialLogin:Instagram:RedirectDomain"];
    x.Pinterest.ClientId = builder.Configuration["SocialLogin:Pinterest:ClientId"];
    x.Pinterest.ClientSecret = builder.Configuration["SocialLogin:Pinterest:ClientSecret"];
    x.Pinterest.RedirectDomain = builder.Configuration["SocialLogin:Pinterest:RedirectDomain"];
},
x =>
{
    x.BearerTokenExpiration = TimeSpan.FromHours(1);
    x.RefreshTokenExpiration = TimeSpan.FromDays(10);
});
builder.Services.AddSocialUserProvider<SocialUserProvider>();
builder.Services.AddRepository<SocialRole, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(x =>
    {
        x.PopulateWithRandomData(100);
    });
});
builder.Services.AddApiFromRepositoryFramework()
    .WithSwagger()
    .WithModelsApi()
    .WithMapApi()
    .WithDocumentation()
    .WithDescriptiveName("Social Api");
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(x =>
{
    x.AddPolicy("all", t =>
    {
        t.AllowAnyHeader().AllowAnyOrigin();
    });
});
builder.Services.AddAuthorization(x =>
{
    x.AddPolicy("all", t => { t.RequireClaim("name"); });
});
var app = builder.Build();
await app.Services.WarmUpAsync();

app.UseCors("all");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseApiFromRepositoryFramework().WithDefaultAuthorization();
app.UseSocialLoginEndpoints();

app.Run();
