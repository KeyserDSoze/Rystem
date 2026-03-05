# Rystem.Authentication.Social

[![Version](https://img.shields.io/nuget/v/Rystem.Authentication.Social)](https://www.nuget.org/packages/Rystem.Authentication.Social)
[![Downloads](https://img.shields.io/nuget/dt/Rystem.Authentication.Social)](https://www.nuget.org/packages/Rystem.Authentication.Social)

ASP.NET Core server-side social authentication for Rystem. Exposes OAuth token exchange endpoints, manages per-provider HTTP clients, and integrates with .NET bearer tokens.

Supported providers: **Google**, **Microsoft**, **Facebook**, **GitHub**, **Amazon**, **LinkedIn**, **X (Twitter)**, **Instagram**, **Pinterest**, **TikTok**, and internal **.NET** bearer.

---

## Install

```bash
dotnet add package Rystem.Authentication.Social
```

> **Dependencies**: `Rystem.Authentication.Social.Abstractions`, `Rystem.DependencyInjection`

---

## Registration

### `AddSocialLogin<TProvider>`

```csharp
builder.Services.AddSocialLogin<MyUserProvider>(settings =>
{
    settings.Google.ClientId = "...";
    settings.Google.ClientSecret = "...";
    settings.Google.AddUri("https://app.example.com");

    settings.Microsoft.ClientId = "...";
    settings.Microsoft.ClientSecret = "...";
    settings.Microsoft.AddUri("https://app.example.com");

    settings.GitHub.ClientId = "...";
    settings.GitHub.ClientSecret = "...";

    // Facebook and Amazon use access tokens directly â€” no secrets needed
});
```

Full signature:

```csharp
public static IServiceCollection AddSocialLogin<TProvider>(
    this IServiceCollection services,
    Action<SocialLoginBuilder> settings,
    Action<BearerTokenOptions>? action = null,
    ServiceLifetime userProviderLifeTime = ServiceLifetime.Transient)
    where TProvider : class, ISocialUserProvider
```

- `action` â€” configures .NET bearer token options (e.g. token lifetime, sliding expiration)
- `userProviderLifeTime` â€” DI lifetime for your `ISocialUserProvider` implementation

A provider is only activated (its `HttpClient` registered) when its `IsActive` property returns `true`.

---

## Provider Configuration

Provider settings follow an inheritance hierarchy:

| Class | Properties added | `IsActive` when |
|---|---|---|
| `SocialDefaultLoginSettings` | _(none)_ | always `true` |
| `SocialLoginSettings` | `ClientId` | `ClientId != null` |
| `SocialLoginWithSecretsSettings` | `ClientId`, `ClientSecret` | both non-null |
| `SocialLoginWithSecretsAndRedirectSettings` | + allowed redirect domains | all of the above + â‰¥1 domain set |

### `SocialLoginBuilder`

```csharp
public sealed class SocialLoginBuilder
{
    public SocialLoginWithSecretsAndRedirectSettings Google { get; set; }
    public SocialLoginWithSecretsAndRedirectSettings Microsoft { get; set; }
    public SocialDefaultLoginSettings Facebook { get; set; }
    public SocialDefaultLoginSettings Amazon { get; set; }
    public SocialLoginWithSecretsSettings GitHub { get; set; }
    public SocialLoginWithSecretsAndRedirectSettings Linkedin { get; set; }
    public SocialLoginWithSecretsAndRedirectSettings X { get; set; }
    public SocialLoginWithSecretsAndRedirectSettings Instagram { get; set; }
    public SocialLoginWithSecretsAndRedirectSettings Pinterest { get; set; }
    public SocialLoginWithSecretsAndRedirectSettings TikTok { get; set; }
}
```

### Adding Allowed Redirect Domains

For providers that use `SocialLoginWithSecretsAndRedirectSettings`, the incoming `Origin` / `Referer` header is validated against a whitelist before the token exchange proceeds. Use the fluent API to add allowed origins:

```csharp
settings.Google.ClientId = "...";
settings.Google.ClientSecret = "...";
settings.Google.AddUri("https://app.example.com");
settings.Google.AddUri("http://localhost:5173");              // local dev
settings.Google.AddUris("https://app.example.com", "https://staging.example.com");
settings.Google.AddDomainWithProtocolAndPort("example.com", "https", 443);
```

---

## Implement `ISocialUserProvider`

```csharp
public interface ISocialUserProvider
{
    // Called when issuing the bearer token â€” yield custom claims to embed
    IAsyncEnumerable<Claim> GetClaimsAsync(TokenResponse response, CancellationToken cancellationToken);

    // Called on the /User endpoint â€” return your application user
    Task<ISocialUser> GetAsync(string username, IEnumerable<Claim> claims, CancellationToken cancellationToken);
}
```

Example:

```csharp
public class MyUserProvider : ISocialUserProvider
{
    private readonly IUserRepository _repo;
    public MyUserProvider(IUserRepository repo) => _repo = repo;

    public async IAsyncEnumerable<Claim> GetClaimsAsync(
        TokenResponse response,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var user = await _repo.GetOrCreateAsync(response.Username, cancellationToken);
        yield return new Claim(ClaimTypes.Name, user.Username!);
        yield return new Claim(ClaimTypes.Role, user.Role);
    }

    public async Task<ISocialUser> GetAsync(
        string username, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        return await _repo.GetAsync(username, cancellationToken);
    }
}
```

---

## Expose Endpoints

```csharp
app.UseSocialLoginEndpoints();
```

Calls `UseAuthentication()`, `UseAuthorization()`, and registers the two minimal API endpoints below.

### `GET/POST api/Authentication/Social/Token`

Exchanges an OAuth authorization code for a Rystem bearer token.

| Parameter | Source | Description |
|---|---|---|
| `provider` | query | `ProviderType` value (e.g. `Google`, `Microsoft`) |
| `code` | query | Authorization code from the OAuth provider |
| `redirectPath` | query (optional) | Path string for the redirect URI |
| _(body)_ | JSON body (optional) | `Dictionary<string, string>` â€” e.g. `{ "code_verifier": "..." }` for PKCE |

The domain is extracted automatically from the `Origin` or `Referer` request header and validated against the allowed-domains whitelist configured in `SocialLoginBuilder`. The final redirect URI is assembled as `{domain}{redirectPath}`.

On success, returns a signed `ClaimsPrincipal` as a bearer token (`Results.SignIn`).  
On failure, returns `401 Unauthorized` or a `Problem` with the error message.

### `GET api/Authentication/Social/User` _(requires authentication)_

Returns the authenticated user object.

- With `ISocialUserProvider` registered â†’ calls `GetAsync(username, claims)`
- Without provider â†’ returns `ISocialUser.OnlyUsername(identity.Name)`

---

## Full Setup Example

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSocialLogin<MyUserProvider>(settings =>
{
    settings.Google.ClientId     = builder.Configuration["Google:ClientId"];
    settings.Google.ClientSecret = builder.Configuration["Google:ClientSecret"];
    settings.Google.AddUri(builder.Configuration["App:Domain"]!);

    settings.Microsoft.ClientId     = builder.Configuration["Microsoft:ClientId"];
    settings.Microsoft.ClientSecret = builder.Configuration["Microsoft:ClientSecret"];
    settings.Microsoft.AddUri(builder.Configuration["App:Domain"]!);
});

var app = builder.Build();
app.UseSocialLoginEndpoints();
app.Run();
```
