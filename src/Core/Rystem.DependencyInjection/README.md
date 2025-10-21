### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## 📚 Resources

- **📖 Complete Documentation**: [https://rystem.net](https://rystem.net)
- **🤖 MCP Server for AI**: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- **💬 Discord Community**: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- **☕ Support the Project**: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Get Started

## Dependency injection extensions

### Warm up
When you use the DI pattern in your .Net application you could need a warm up after the build of your services. And with Rystem you can simply do it.

	builder.Services.AddWarmUp(() => somethingToDo());

and after the build use the warm up

	var app = builder.Build();
	await app.Services.WarmUpAsync();


## Population service
You can use the population service to create a list of random value of a specific Type.
An example from unit test explains how to use the service.

    IServiceCollection services = new ServiceCollection();
    services.AddPopulationService();
    var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
    var populatedModel = serviceProvider.GetService<IPopulation<PopulationModelTest>>();
    IPopulation<PopulationModelTest> allPrepopulation = populatedModel!
        .Setup()
        .WithPattern(x => x.J!.First().A, "[a-z]{4,5}")
            .WithPattern(x => x.Y!.First().Value.A, "[a-z]{4,5}")
            .WithImplementation(x => x.I, typeof(MyInnerInterfaceImplementation))
            .WithPattern(x => x.I!.A!, "[a-z]{4,5}")
            .WithPattern(x => x.II!.A!, "[a-z]{4,5}")
            .WithImplementation<IInnerInterface, MyInnerInterfaceImplementation>(x => x.I!);
    var all = allPrepopulation.Populate();

## Abstract factory
You can use this abstract factory solution when you need to setup more than one service of the same kind
and you need to distinguish them by a name.

I have an interface 

    public interface IMyService
    {
        string GetName();
    }

Some options for every service

    public class SingletonOption
    {
        public string ServiceName { get; set; }
    }
    public class TransientOption
    {
        public string ServiceName { get; set; }
    }
    public class ScopedOption
    {
        public string ServiceName { get; set; }
    }

with built options which is a IServiceOptions, a options class that ends up with another class. Used for example when you have to add a settings like a connection string but you want to use a service like a client that uses that connection string. 

    public class BuiltScopedOptions : IServiceOptions<ScopedOption>
    {
        public string ServiceName { get; set; }

        public Task<Func<ScopedOption>> BuildAsync()
        {
            return Task.FromResult(() => new ScopedOption
            {
                ServiceName = ServiceName
            });
        }
    }

And six different services

    public class SingletonService : IMyService, IServiceWithOptions<SingletonOption>
    {
        public SingletonOption Options { get; set; }
        public string Id { get; } = Guid.NewGuid().ToString();
        public string GetName()
        {
            return $"{Options.ServiceName} with id {Id}";
        }
    }
    public class TransientService : IMyService, IServiceWithOptions<TransientOption>
    {
        public TransientOption Options { get; set; }
        public string Id { get; } = Guid.NewGuid().ToString();
        public string GetName()
        {
            return $"{Options.ServiceName} with id {Id}";
        }
    }
    public class ScopedService : IMyService, IServiceWithOptions<ScopedOption>
    {
        public ScopedOption Options { get; set; }
        public string Id { get; } = Guid.NewGuid().ToString();
        public string GetName()
        {
            return $"{Options.ServiceName} with id {Id}";
        }
    }
    public class ScopedService2 : IMyService, IServiceWithOptions<ScopedOption>
    {
        public ScopedOption Options { get; set; }
        public string Id { get; } = Guid.NewGuid().ToString();

        public string GetName()
        {
            return $"{Options.ServiceName} with id {Id}";
        }
    }
    public class ScopedService3 : IMyService, IServiceWithOptions<ScopedOption>
    {
        public ScopedOption Options { get; set; }
        public string Id { get; } = Guid.NewGuid().ToString();

        public string GetName()
        {
            return $"{Options.ServiceName} with id {Id}";
        }
    }
    public class ScopedService4 : IMyService, IServiceWithOptions<ScopedOption>
    {
        public ScopedOption Options { get; set; }
        public string Id { get; } = Guid.NewGuid().ToString();

        public string GetName()
        {
            return $"{Options.ServiceName} with id {Id}";
        }
    }

I can setup them in this way

    var services = new ServiceCollection();
    services.AddFactory<IMyService, SingletonService, SingletonOption>(x =>
    {
        x.ServiceName = "singleton";
    },
    "singleton",
    ServiceLifetime.Singleton);

    services.AddFactory<IMyService, TransientService, TransientOption>(x =>
    {
        x.ServiceName = "transient";
    },
    "transient",
    ServiceLifetime.Transient);

    services.AddFactory<IMyService, ScopedService, ScopedOption>(x =>
    {
        x.ServiceName = "scoped";
    },
    "scoped",
    ServiceLifetime.Scoped);

    services.AddFactory<IMyService, ScopedService2, ScopedOption>(x =>
    {
        x.ServiceName = "scoped2";
    },
    "scoped2",
    ServiceLifetime.Scoped);

    await services.AddFactoryAsync<IMyService, ScopedService3, BuiltScopedOptions, ScopedOption>(
        x =>
        {
            x.ServiceName = "scoped3";
        },
        "scoped3"
    );

    await services.AddFactoryAsync<IMyService, ScopedService3, BuiltScopedOptions, ScopedOption>(
        x =>
        {
            x.ServiceName = "scoped3_2";
        },
        "scoped3_2"
    );

    await services.AddFactoryAsync<IMyService, ScopedService4, BuiltScopedOptions, ScopedOption>(
        x =>
        {
            x.ServiceName = "scoped4";
        },
        "scoped4"
    );

and use them in this way

    var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
    var factory = serviceProvider.GetService<IFactory<IMyService>>()!;
    var factory2 = serviceProvider.GetService<IFactory<IMyService>>()!;

    var singletonFromFactory = factory.Create("singleton").Id;
    var singletonFromFactory2 = factory2.Create("singleton").Id;
    var transientFromFactory = factory.Create("transient").Id;
    var transientFromFactory2 = factory2.Create("transient").Id;
    var scopedFromFactory = factory.Create("scoped").Id;
    var scopedFromFactory2 = factory2.Create("scoped").Id;
    var scoped2FromFactory = factory.Create("scoped2").Id;
    var scoped2FromFactory2 = factory2.Create("scoped2").Id;
    var scoped3FromFactory = factory.Create("scoped3").Id;
    var scoped3FromFactory2 = factory2.Create("scoped3").Id;
    var scoped3_2FromFactory = factory.Create("scoped3_2").Id;
    var scoped3_2FromFactory2 = factory2.Create("scoped3_2").Id;
    var scoped4FromFactory = factory.Create("scoped4").Id;
    var scoped4FromFactory2 = factory2.Create("scoped4").Id;

    Assert.Equal(singletonFromFactory, singletonFromFactory2);
    Assert.NotEqual(transientFromFactory, transientFromFactory2);
    Assert.Equal(scopedFromFactory, scopedFromFactory2);
    Assert.Equal(scoped2FromFactory, scoped2FromFactory2);
    Assert.NotEqual(scoped3FromFactory, scoped3FromFactory2);
    Assert.NotEqual(scoped3_2FromFactory, scoped3_2FromFactory2);
    Assert.NotEqual(scoped4FromFactory, scoped4FromFactory2);

## Decorator
You may add a decoration for your services, based on the abstract factory integration.
The decorator service replaces the previous version and receives it during the injection.

Setup

    services
        .AddService<ITestWithoutFactoryService, TestWithoutFactoryService>(lifetime);
    services
        .AddDecoration<ITestWithoutFactoryService, TestWithoutFactoryServiceDecorator>(null, lifetime);

Usage

    var decorator = provider.GetRequiredService<ITestWithoutFactoryService>();
    var previousService = provider.GetRequiredService<IDecoratedService<ITestWithoutFactoryService>>();

In decorator you may find the previousService in the method SetDecoratedService which runs in injection

    public class TestWithoutFactoryServiceDecorator : ITestWithoutFactoryService, IDecoratorService<ITestWithoutFactoryService>
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public ITestWithoutFactoryService Test { get; private set; }
        public void SetDecoratedService(ITestWithoutFactoryService service)
        {
            Test = service;
        }

        public void SetFactoryName(string name)
        {
            return;
        }
    }

### Decorator with Abstract Factory integration
You may add a decoration only for one service of your factory integration.

Setup

    services.AddFactory<ITestService, TestService, TestOptions>(x =>
    {
        x.ClassicName = classicName;
    },
    factoryName,
    lifetime);
    services
        .AddDecoration<ITestService, DecoratorTestService>(factoryName, lifetime);

Usage

    var decoratorFactory = provider.GetRequiredService<IFactory<ITestService>>();
    var decorator = decoratorFactory.Create(factoryName);
    var previousService = decoratorFactory.CreateWithoutDecoration(factoryName);

## Factory Fallback
You may add a fallback for your factory integration. The fallback service is called when the factory service key is not found.

```csharp
services.AddFactoryFallback<TService, TFactoryFallback>();
```

where TFactoryFallback is class and an IFactoryFallback<TService>

You may add a fallback with an action fallback too.
    
```csharp
services.AddActionAsFallbackWithServiceProvider<TService>(Func<FallbackBuilderForServiceProvider, TService> fallbackBuilder);
```

## Scan dependency injection
You may scan your assemblies in search of types you need to add to dependency injection.
For instance I have an interface IAnything and I need to add all classes which implements it.

    public interface IAnything
    {
    }
    internal class ScanModels : IAnything
    {
    }

and in service collection I can add it.

    serviceCollection
        .Scan<IAnything>(ServiceLifetime.Scoped, typeof(IAnything).Assembly);

I can add to my class the interface IScannable of T to scan automatically. 
For instance.

    public interface IAnything
    {
    }
    internal class ScanModels : IAnything, IScannable<IAnything>
    {
    }

and in service collection I could add it in this way

    serviceCollection
        .Scan(ServiceLifetime.Scoped, typeof(IAnything).Assembly);

Furthermore with ISingletonScannable, IScopedScannable and ITransientScannable I can override the service lifetime.
For instance.

    public interface IAnything
    {
    }
    internal class ScanModels : IAnything, IScannable<IAnything>, ISingletonScannable
    {
    }

    serviceCollection
        .Scan(ServiceLifetime.Scoped, typeof(IAnything).Assembly);

ScanModels will be installed as a Singleton service, overwriting the service lifetime from Scan method.

You also automatically use different assembly sources.

    serviceCollection
        .ScanDependencyContext(ServiceLifetime.Scoped);

or

    serviceCollection
        .ScanCallingAssembly(ServiceLifetime.Scoped);

or

    serviceCollection
        .ScanCurrentDomain(ServiceLifetime.Scoped);

or

    serviceCollection
        .ScanEntryAssembly(ServiceLifetime.Scoped);

or

    serviceCollection
        .ScanExecutingAssembly(ServiceLifetime.Scoped);

or

    serviceCollection
        .ScanFromType<T>(ServiceLifetime.Scoped);

or

    serviceCollection
        .ScanFromTypes<T1, T2>(ServiceLifetime.Scoped);

Finally with ScanWithReferences you may call all the assemblies you want plus all referenced assemblies by them.