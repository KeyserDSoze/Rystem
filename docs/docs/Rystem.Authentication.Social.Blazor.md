# Rystem.Authentication.Social.Blazor
This project would be a super project to help the api creator to have fast api behind business interfaces and services dispatched through dependency injection.

# Install javascript
Setup js in app.razor in head or at the end
```
   <script src="_content/Rystem.Authentication.Social.Blazor/socialauthentications.js"></script>
```


# DI - Example

```csharp
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
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();
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

app
    .MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();

```

# Example for Routes.razor

```
<SocialAuthenticationRouter AppAssembly="typeof(Program).Assembly" DefaultLayout="typeof(Layout.MainLayout)">
</SocialAuthenticationRouter>
```

# Example for logout
You may use the logout button
```
<SocialLogout></SocialLogout>
```
or create your custom logout through the SocialUser cascade parameter
```
[CascadingParameter(Name = "SocialUser")]
public SocialUserWrapper? SocialUser { get; set; }

private async ValueTask LogoutAsync()
{
    if (SocialUser != null)
        await SocialUser.LogoutAsync(false);
}
```