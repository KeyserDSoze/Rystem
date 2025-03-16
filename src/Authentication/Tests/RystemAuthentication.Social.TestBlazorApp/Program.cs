using Rystem.Authentication.Social.TestApi.Models;
using Rystem.Localization.Test.App;
using RystemAuthentication.Social.TestBlazorApp.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSocialLoginUI(x =>
{
    x.ApiUrl = "https://localhost:7017";
    x.Google.ClientId = builder.Configuration["SocialLogin:Google:ClientId"];
    x.Microsoft.ClientId = builder.Configuration["SocialLogin:Microsoft:ClientId"];
});
builder.Services.AddRepository<SocialRole, string>(repositoryBuilder =>
{
    repositoryBuilder.WithApiClient(apiBuilder =>
    {
        apiBuilder.WithHttpClient("https://localhost:7017").WithDefaultRetryPolicy();
    });
});
builder.Services.AddDefaultSocialLoginAuthorizationInterceptorForApiHttpClient();
builder.Services.AddLocalizationFromRystem();
var app = builder.Build();
await app.Services.WarmUpAsync();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
app.UseSocialLoginAuthorization();
app
    .MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();
