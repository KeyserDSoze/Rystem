﻿using Microsoft.Extensions.DependencyInjection;
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
        public IScenesBuilder Configure(Action<SceneManagerSettings> settings)
        {
            settings(_settings);
            _services.TryAddSingleton(_settings);
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
