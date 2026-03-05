### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Services extensions

### HttpClient to use your API (example)
You can add a client for a specific url

    builder.Services.AddRepository<User, string>(builder =>
    {
        builder
            .WithApiClient()
            .WithHttpClient("localhost:7058");
    });

### Default interceptor for Authentication with JWT
You may use the default interceptor to deal with the identity manager in .Net DI.

    builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();

This line of code inject an interceptor that works with ITokenAcquisition, injected by the framework during OpenId integration (for example AAD integration).
Automatically it adds the token to each request.

You may use the default identity interceptor not on all repositories, you can specificy them with

    builder.Services.AddRepository(builder => {
        builder
            .WithApiClient()
            .WithHttpClient("localhost:7058")
            .AddDefaultAuthorizationInterceptorForApiHttpClient<T, TKey>();
    });

Remember to add

    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAdB2C or AzureAd"))
     .EnableTokenAcquisitionToCallDownstreamApi(new string[] { "your_scope/access_as_user" })
     .AddInMemoryTokenCaches();

    builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();

    builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();