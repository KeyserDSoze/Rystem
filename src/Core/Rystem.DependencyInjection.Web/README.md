### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Get Started with Runtime Dependency Injection

## Adding runtime provider

	 builder.Services.AddRuntimeServiceProvider();

and after the build use the runtime provider

	var app = builder.Build();
	app.UseRuntimeServiceProvider();


## Add service at runtime

```csharp
await RuntimeServiceProvider.GetServiceCollection()
       .AddSingleton<Service2>()
       .RebuildAsync();
```

## Add service at runtime with lock

```csharp
 await RuntimeServiceProvider
    .AddServicesToServiceCollectionWithLock(configureFurtherServices =>
    {
        configureFurtherServices.AddSingleton(service);
    })
.RebuildAsync();
```

## Add fallback for Factory and automatic rebuild of service collection
In this example Factorized is a simple class with a few parameters.

```csharp
services.AddFactory<Factorized>("1");
services.AddActionAsFallbackWithServiceCollectionRebuilding<Factorized>(async x =>
{
    //example of retrievieng something
    await Task.Delay(1); 
    //example of ServiceProvider usage during new service addition
    var singletonService = x.ServiceProvider.GetService<SingletonService>();
    if (singletonService != null)
    {
        //example of adding a new service as factory to the service collection. You need to pass a delegate.
        x.ServiceColletionBuilder = (serviceCollection => serviceCollection.AddFactory<Factorized>(x.Name));
    }
});
```