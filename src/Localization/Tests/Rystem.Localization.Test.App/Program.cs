using Rystem.Localization.Test.App;
using Rystem.Localization.Test.App.Components;
using static Rystem.Localization.Test.App.ServiceCollectionExtensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddLocalizationForRystem();
builder.Services.AddTransient<LocalizationMiddleware>();
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
app.UseAntiforgery();
app.MapStaticAssets();
app.AddLocalizationMiddleware();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
