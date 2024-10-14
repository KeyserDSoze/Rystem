using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.OpenAi.Actors
{
    internal sealed class ActorBuilder : IActorBuilder
    {
        private readonly IServiceCollection _services;
        private readonly string _sceneName;
        public StringBuilder SimpleActors { get; } = new();
        public ActorBuilder(IServiceCollection services, string sceneName)
        {
            _services = services;
            _sceneName = sceneName;
        }
        public IActorBuilder AddActor<T>()
            where T : class, IActor
        {
            _services.AddKeyedTransient<IActor, T>(_sceneName);
            return this;
        }
        public IActorBuilder AddActor(string role)
        {
            SimpleActors.AppendLine(role);
            return this;
        }
    }
}
