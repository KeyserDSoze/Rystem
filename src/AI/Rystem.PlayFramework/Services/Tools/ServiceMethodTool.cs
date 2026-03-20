using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Rystem.PlayFramework.Domain.Wrappers;
using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework;

/// <summary>
/// Tool that wraps a service method.
/// </summary>
internal sealed class ServiceMethodTool : ISceneTool, ISceneToolMetadata
{
    private readonly ServiceToolConfiguration _config;
    private readonly bool _isGenericAsync;
    private readonly bool _withoutReturn;
    private readonly IJsonService _jsonService;

    public ServiceMethodTool(ServiceToolConfiguration config, IJsonService? jsonService = null)
    {
        _jsonService = jsonService ?? new DefaultJsonService();
        _config = config;
        Name = _config.ToolName;
        Description = _config.Description;
        var target = new LazyServiceTarget(_config.ServiceType);
        var aiFunction = AIFunctionFactory.Create(
            _config.Method,
            target,
            Name,
            Description,
            JsonHelper.JsonSerializerOptions);
        ToolDescription = aiFunction;
        //var schema = AIJsonUtilities.CreateFunctionJsonSchema(_config.Method, Name, Description, JsonHelper.JsonSerializerOptions);
        //ToolDescription = AIFunctionFactory.CreateDeclaration(Name, Description, schema, null);
        _withoutReturn = _config.Method.ReturnType == typeof(void) || _config.Method.ReturnType == typeof(Task) || _config.Method.ReturnType == typeof(ValueTask);
        _isGenericAsync = _config.Method.ReturnType.IsGenericType &&
            (_config.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
            || _config.Method.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>));
    }

    public string Name { get; }
    public string Description { get; }
    public AITool ToolDescription { get; }
    public PlayFrameworkToolSourceType SourceType => PlayFrameworkToolSourceType.Service;
    public string? SourceName => _config.ServiceType.Name;
    public string? MemberName => _config.Method.Name;
    public bool IsCommand => false;
    public string? JsonSchema => null;

    

    public async Task<object?> ExecuteAsync(
        string arguments,
        SceneContext context,
        CancellationToken cancellationToken)
    {
        // Get service instance
        var service = context.ServiceProvider.GetService(_config.ServiceType) ?? throw new InvalidOperationException($"Service {_config.ServiceType.Name} not registered");

        // Deserialize arguments
        var parameters = _config.Method.GetParameters();
        var args = DeserializeArguments(arguments, parameters, cancellationToken);

        var result = _config.Method.Invoke(service, args);
        if (result is Task task)
            await task;
        else if (result is ValueTask valueTask)
            await valueTask;
        if (_withoutReturn)
            return default!;
        else if (result is not null)
        {
            var response = _isGenericAsync ? ((dynamic)result!).Result : result;
            if (response is not null)
                return response;
            else
                return default!;
        }
        else
            return default!;
    }

    private object?[] DeserializeArguments(string argumentsJson, ParameterInfo[] parameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson) || argumentsJson == "{}")
        {
            return parameters.Length == 0 ? [] : new object?[parameters.Length];
        }

        var argsDict = _jsonService.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
        if (argsDict == null)
        {
            return new object?[parameters.Length];
        }

        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];

            if (param.ParameterType == typeof(CancellationToken))
            {
                args[i] = cancellationToken;
            }
            else if (argsDict.TryGetValue(param.Name!, out var value))
            {
                // If the LLM explicitly sends null for this parameter, honour it:
                // - for nullable types (Guid?, string, etc.) → null
                // - for non-nullable value types → fall through to default/Activator
                if (value.ValueKind == System.Text.Json.JsonValueKind.Null)
                {
                    var isNullable = !param.ParameterType.IsValueType
                        || Nullable.GetUnderlyingType(param.ParameterType) != null;
                    args[i] = isNullable ? null : Activator.CreateInstance(param.ParameterType);
                }
                else
                {
                    args[i] = _jsonService.Deserialize(value.GetRawText(), param.ParameterType);
                }
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
