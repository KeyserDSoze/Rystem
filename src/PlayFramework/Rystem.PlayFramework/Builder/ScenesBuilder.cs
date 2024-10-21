using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Rystem.PlayFramework
{
    internal sealed class ScenesBuilder : IScenesBuilder
    {
        private readonly IServiceCollection _services;
        private readonly SceneManagerSettings _settings;
        public ScenesBuilder(IServiceCollection services)
        {
            _services = services;
            _settings = new();
        }
        public const string MainActor = "MainActor";
        public IScenesBuilder Configure(Action<SceneManagerSettings> settings)
        {
            settings(_settings);
            _services.TryAddSingleton(_settings);
            return this;
        }
        public IScenesBuilder AddMainActor(string role, bool playInEveryScene)
        {
            if (playInEveryScene)
                _services.AddKeyedSingleton<IPlayableActor>(MainActor, new SimpleActor { Role = role });
            else
                _services.AddKeyedSingleton<IActor>(MainActor, new SimpleActor { Role = role });
            return this;
        }
        public IScenesBuilder AddMainActor<T>(bool playInEveryScene)
            where T : class, IActor
        {
            if (playInEveryScene)
                _services.AddKeyedTransient<IPlayableActor, T>(MainActor);
            else
                _services.AddKeyedTransient<IActor, T>(MainActor);
            return this;
        }
        public IScenesBuilder AddMainActor(Func<SceneContext, string> action, bool playInEveryScene)
        {
            if (playInEveryScene)
                _services.AddKeyedSingleton<IPlayableActor>(MainActor, new ActionActor { Action = action });
            else
                _services.AddKeyedSingleton<IActor>(MainActor, new ActionActor { Action = action });
            return this;
        }
        public IScenesBuilder AddMainActor(Func<SceneContext, CancellationToken, Task<string>> action, bool playInEveryScene)
        {
            if (playInEveryScene)
                _services.AddKeyedSingleton<IPlayableActor>(MainActor, new AsyncActionActor { Action = action });
            else
                _services.AddKeyedSingleton<IActor>(MainActor, new AsyncActionActor { Action = action });
            return this;
        }
        public IScenesBuilder AddCustomDirector<T>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where T : class, IDirector
        {
            _services.RemoveAll<IDirector>();
            _services.AddService<IDirector, T>(lifetime);
            return this;
        }
        public IScenesBuilder AddScene(Action<ISceneBuilder> builder)
        {
            var sceneBuilder = new SceneBuilder(_services);
            builder(sceneBuilder);
            _services.AddKeyedSingleton(sceneBuilder.Scene.Name, sceneBuilder.Scene);
            PlayHandler.Instance[sceneBuilder.Scene.Name].Chooser = x => x.AddTool(sceneBuilder.Scene.Name, sceneBuilder.Scene.Description, new object());
            return this;
        }
    }
}
