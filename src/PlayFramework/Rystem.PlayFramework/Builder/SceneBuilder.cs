using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.OpenAi.Actors
{
    internal sealed class SceneBuilder : ISceneBuilder
    {
        private readonly IServiceCollection _services;
        internal IScene Scene { get; } = new Scene();
        public SceneBuilder(IServiceCollection services)
        {
            _services = services;
        }
        public ISceneBuilder WithName(string name)
        {
            Scene.Name = name;
            return this;
        }
        public ISceneBuilder WithDescription(string description)
        {
            Scene.Description = description;
            return this;
        }
        public ISceneBuilder WithOpenAi(string name)
        {
            Scene.OpenAiFactoryName = name;
            return this;
        }
        public ISceneBuilder WithHttpClient(string name)
        {
            Scene.HttpClientName = name;
            return this;
        }
        public List<Regex>? RegexForApiMapping { get; set; }
        public ISceneBuilder WithApi(Action<IScenePathBuilder> builder)
        {
            var scenePathBuilder = new ScenePathBuilder();
            builder(scenePathBuilder);
            RegexForApiMapping = scenePathBuilder.RegexForApiMapping;
            return this;
        }
        public ISceneBuilder WithActors(Action<IActorBuilder> builder)
        {
            var builderInstance = new ActorBuilder(_services, Scene.Name);
            builder(builderInstance);
            if (Scene is Scene scene)
                scene.SimpleActors = builderInstance.SimpleActors.ToString();
            return this;
        }
    }
    internal sealed class ScenePathBuilder : IScenePathBuilder
    {
        public List<Regex> RegexForApiMapping { get; set; } = new();
        public IScenePathBuilder Map(Regex regex)
        {
            RegexForApiMapping.Add(regex);
            return this;
        }
        public IScenePathBuilder Map(string startsWith)
        {
            RegexForApiMapping.Add(new Regex($"{startsWith}*"));
            return this;
        }
    }
}
