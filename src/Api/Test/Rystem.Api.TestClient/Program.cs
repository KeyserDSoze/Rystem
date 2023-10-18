using Rystem.Api.Test.Domain;
using Rystem.Api.TestClient.Components;
using Rystem.Api.TestClient.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddBusiness();
builder.Services.AddClientsForEndpointApi(x =>
{
    x.ConfigurationHttpClientForApi(t =>
    {
        t.BaseAddress = new Uri("https://localhost:7117");
    });
});
builder.Services.AddEnhancerForAllEndpoints<Enhancer>();
builder.Services.AddEnhancerForAllEndpoints<Enhancer2>();
builder.Services.AddEnhancerForEndpoint<Enhancer3, IColam>();
builder.Services.AddEnhancerForEndpoint<Enhancer4, ISalubry>();
var app = builder.Build();

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
