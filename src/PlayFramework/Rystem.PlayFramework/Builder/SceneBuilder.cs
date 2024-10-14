using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework
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
        public ISceneBuilder WithService<T>(Action<ISceneServiceBuilder<T>>? builder = null)
            where T : class
        {
            var delegations = new List<Delegate>();
            var currentType = typeof(T);
            if (builder == null)
            {
                currentType.GetMethods().ToList().ForEach(x =>
                {
                    delegations.Add(x.CreateDelegate(currentType));
                });
            }
            else
            {
                var sceneServiceBuilder = new SceneServiceBuilder<T>();
                builder(sceneServiceBuilder);
                delegations.AddRange(sceneServiceBuilder.Delegates);
            }
            foreach (var delegation in delegations)
            {
                var name = $"{currentType.Name}.{delegation.Method.Name}";
                var description = delegation.Method.GetCustomAttributes(true).FirstOrDefault(x => x.GetType() == typeof(DescriptionAttribute)) as DescriptionAttribute;
                var jsonFunctionObject = new ToolNonPrimitiveProperty()
                {
                    Type = "object",
                    Description = description?.Description ?? name,
                };
                var jsonFunction = new Tool
                {
                    Name = name,
                    Description = description?.Description ?? name,
                    Parameters = jsonFunctionObject
                };
                ScenesBuilderHelper.ServiceActions.Add(name, new());
                var withoutReturn = delegation.Method.ReturnType == typeof(void) || delegation.Method.ReturnType == typeof(Task) || delegation.Method.ReturnType == typeof(ValueTask);
                var isGenericAsync = delegation.Method.ReturnType.IsGenericType &&
                    (delegation.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                    || delegation.Method.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>));

                ScenesBuilderHelper.ServiceCalls.Add(name, async (serviceProvider, bringer) =>
                {
                    var service = serviceProvider.GetRequiredService<T>();
                    var result = delegation.Method.Invoke(service, bringer.Parameters.ToArray());
                    if (result is Task task)
                        await task;
                    if (result is ValueTask valueTask)
                        await valueTask;
                    if (withoutReturn)
                        return default!;
                    else if (result is not null)
                    {
                        var response = isGenericAsync ? ((dynamic)result!).Result : result;
                        if (response is not null)
                            return response;
                        else
                            return default!;
                    }
                    else
                        return default!;
                });
                foreach (var parameter in delegation.Method.GetParameters())
                {
                    var parameterName = parameter.Name ?? parameter.ParameterType.Name;
                    ToolPropertyHelper.Add(parameterName, parameter.ParameterType, jsonFunctionObject);
                    ScenesBuilderHelper.ServiceActions[name][parameterName] = (value, bringer) =>
                    {
                        bringer.Parameters.Add(value[parameterName]);
                        return ValueTask.CompletedTask;
                    };
                }
            }
            return this;
        }
    }
    internal sealed class SceneServiceBuilder<T> : ISceneServiceBuilder<T>
        where T : class
    {
        public List<Delegate> Delegates { get; } = new();
        public ISceneServiceBuilder<T> WithMethod(Func<T, Delegate> method)
        {
            Delegates.Add(method);
            return this;
        }
    }
}
