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