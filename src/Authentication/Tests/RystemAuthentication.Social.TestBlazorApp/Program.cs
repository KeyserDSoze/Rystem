using RystemAuthentication.Social.TestBlazorApp.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSocialLoginUI(x =>
{
    x.ApiUrl = "https://localhost:7017";
    x.Google.ClientId = "224823396805-9nih8454lspd7lkbsiv46j8i1i77sbbg.apps.googleusercontent.com";
});

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

app.UseSocialLogin()
    .MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
