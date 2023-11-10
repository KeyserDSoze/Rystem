### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Authentication.Social
This project would be a super project to help the api creator to have fast api behind business interfaces and services dispatched through dependency injection.

## How to use it
You need to configure the social information and token settings, for instance the duration of your token.
```
builder.Services.AddSocialLogin(x =>
{
    x.Google.ClientId = builder.Configuration["SocialLogin:Google:ClientId"];
    x.Google.ClientSecret = builder.Configuration["SocialLogin:Google:ClientSecret"];
    x.Google.RedirectDomain = builder.Configuration["SocialLogin:Google:RedirectDomain"];
    x.Microsoft.ClientId = builder.Configuration["SocialLogin:Microsoft:ClientId"];
    x.Microsoft.ClientSecret = builder.Configuration["SocialLogin:Microsoft:ClientSecret"];
    x.Microsoft.RedirectDomain = builder.Configuration["SocialLogin:Microsoft:RedirectDomain"];
    x.Facebook.ClientId = builder.Configuration["SocialLogin:Facebook:ClientId"];
    x.Facebook.ClientSecret = builder.Configuration["SocialLogin:Facebook:ClientSecret"];
    x.Facebook.RedirectDomain = builder.Configuration["SocialLogin:Facebook:RedirectDomain"];
},
x =>
{
    x.BearerTokenExpiration = TimeSpan.FromHours(1);
    x.RefreshTokenExpiration = TimeSpan.FromDays(10);
});
```

You need to add in the app builder section the endpoints

```
app.UseSocialLoginEndpoints();
```

You can add your provider for user

```
builder.Services.AddSocialUserProvider<SocialUserProvider>();
```

SocialUserProvider is a ISocialUserProvider, to call for instance a database or storage to fetch the information about the user with social username/email.

```
internal sealed class SocialUserProvider : ISocialUserProvider
{
    public Task<SocialUser> GetAsync(string username, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SuperSocialUser
        {
            Username = $"a {username}",
            Email = username
        } as SocialUser);
    }

    public async IAsyncEnumerable<Claim> GetClaimsAsync(string? username, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        yield return new Claim(ClaimTypes.Name, username!);
        yield return new Claim(ClaimTypes.Upn, "something");
    }
}

public sealed class SuperSocialUser : SocialUser
{
    public string Email { get; set; }
}
```