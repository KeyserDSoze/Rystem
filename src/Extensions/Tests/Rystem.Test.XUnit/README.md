### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Get Started with Rystem XUnitTest helpers
You have to add a startup class in your test project to initialize the Rystem XUnit helpers. 

```csharp
public class Startup : StartupHelper
{
    protected override string? AppSettingsFileName => "appsettings.test.json";
    protected override bool HasTestHost => true;
    protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
    protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => typeof(ServiceController);
    protected override IServiceCollection ConfigureCientServices(IServiceCollection services)
    {
        services.AddHttpClient("client", x =>
        {
            x.BaseAddress = new Uri("http://localhost");
        });
        return services;
    }
    protected override ValueTask ConfigureServerMiddlewaresAsync(IApplicationBuilder applicationBuilder, IServiceProvider serviceProvider)
    {
        applicationBuilder.UseTestApplication();
        return ValueTask.CompletedTask;
    }
    protected override ValueTask ConfigureServerServicesAsync(IServiceCollection services, IConfiguration configuration)
    {
        services.AddTestServices();
        return ValueTask.CompletedTask;
    }
}
```

### TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration
This property is the Type to discover the right assembly to retrieve secrets.\

### TypeToChooseTheRightAssemblyWithControllersToMap
This property is the Type to discover the right assembly to map controllers automatically.\

### AppSettingsFileName
This property is the name of the appsettings file to load the configuration.

### HasTestHost
This property allows the Test server to start. You have to override **ConfigureServerMiddlewaresAsync** and **ConfigureServerServicesAsync**.

applicationBuilder.UseTestApplication() is an example of your middlewares from your api project.

```csharp
 public static IApplicationBuilder UseTestApplication(this IApplicationBuilder app)
{
    app.UseRuntimeServiceProvider();
    app.UseRouting();
    app.UseAuthorization();
    app.UseEndpoints(x =>
    {
        x.MapControllers();
    });
    return app;
}
```

and with the same behavior services.AddTestServices(); adds the services from your api project.

```csharp
 public static IServiceCollection AddTestServices(this IServiceCollection services)
{
    services.AddRuntimeServiceProvider();
    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddSingleton<SingletonService>();
    services.AddSingleton<Singleton2Service>();
    services.AddScoped<ScopedService>();
    services.AddScoped<Scoped2Service>();
    services.AddTransient<TransientService>();
    services.AddTransient<Transient2Service>();
    return services;
}
```

### ConfigureCientServices
This method configure the DI in your XUnit test project. Usually you need to inject the http client to test your api if you need the test server.

```csharp
services.AddHttpClient("client", x =>
{
    x.BaseAddress = new Uri("http://localhost");
});
```

