using Microsoft.Extensions.AI;
using System.Reflection;
using System.Text.Json;

namespace Rystem.PlayFramework;

/// <summary>
/// Tool that wraps a service method.
/// </summary>
internal sealed class ServiceMethodTool : ISceneTool
{
    private readonly ServiceToolConfiguration _config;
    private readonly IServiceProvider _serviceProvider;

    public ServiceMethodTool(ServiceToolConfiguration config, IServiceProvider serviceProvider)
    {
        _config = config;
        _serviceProvider = serviceProvider;
    }

    public string Name => _config.ToolName;
    public string Description => _config.Description;

    public AIFunction ToAIFunction()
    {
        // Build parameter schema from method parameters
        var parameters = _config.Method.GetParameters();
        var schema = AIFunctionFactory.Create(
            _config.Method,
            Name,
            Description);

        return schema;
    }

    public async Task<object?> ExecuteAsync(
        string arguments,
        SceneContext context,
        CancellationToken cancellationToken = default)
    {
        // Get service instance
        var service = _serviceProvider.GetService(_config.ServiceType);
        if (service == null)
        {
            throw new InvalidOperationException($"Service {_config.ServiceType.Name} not registered");
        }

        // Deserialize arguments
        var parameters = _config.Method.GetParameters();
        var args = DeserializeArguments(arguments, parameters);

        // Invoke method
        var result = _config.Method.Invoke(service, args);

        // Handle async methods
        if (result is Task task)
        {
            await task;
            
            // Get result from Task<T>
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty?.GetValue(task);
        }

        return result;
    }

    private static object?[] DeserializeArguments(string argumentsJson, ParameterInfo[] parameters)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson) || argumentsJson == "{}")
        {
            return parameters.Length == 0 ? Array.Empty<object>() : new object?[parameters.Length];
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var argsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson, jsonOptions);
        if (argsDict == null)
        {
            return new object?[parameters.Length];
        }

        var args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];

            if (argsDict.TryGetValue(param.Name!, out var value))
            {
                args[i] = JsonSerializer.Deserialize(value.GetRawText(), param.ParameterType, jsonOptions);
            }
            else if (param.HasDefaultValue)
            {
                args[i] = param.DefaultValue;
            }
            else if (param.ParameterType.IsValueType)
            {
                args[i] = Activator.CreateInstance(param.ParameterType);
            }
            else
            {
                args[i] = null;
            }
        }

        return args;
    }
}
