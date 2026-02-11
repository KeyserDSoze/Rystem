using System.Linq.Expressions;
using System.Reflection;

namespace Rystem.PlayFramework;

/// <summary>
/// Builder for adding service methods as tools.
/// </summary>
public sealed class ServiceToolBuilder<TService> where TService : class
{
    private readonly SceneConfiguration _config;

    internal ServiceToolBuilder(SceneConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Adds a service method as a tool.
    /// </summary>
    /// <param name="methodSelector">Expression selecting the method (e.g., x => x.GetUserAsync).</param>
    /// <param name="toolName">Name for the tool (used by AI).</param>
    /// <param name="description">Description for the AI.</param>
    public ServiceToolBuilder<TService> WithMethod<TResult>(
        Expression<Func<TService, TResult>> methodSelector,
        string toolName,
        string description)
    {
        var methodInfo = ExtractMethodInfo(methodSelector);
        
        _config.ServiceTools.Add(new ServiceToolConfiguration
        {
            ServiceType = typeof(TService),
            Method = methodInfo,
            ToolName = toolName,
            Description = description
        });

        return this;
    }

    private static MethodInfo ExtractMethodInfo<TResult>(Expression<Func<TService, TResult>> methodSelector)
    {
        // Handle method call expression: x => x.Method(args)
        if (methodSelector.Body is MethodCallExpression methodCall)
        {
            return methodCall.Method;
        }

        // Handle unary expression with method call: x => (TResult)x.Method(args)
        if (methodSelector.Body is UnaryExpression { Operand: MethodCallExpression unaryMethodCall })
        {
            return unaryMethodCall.Method;
        }

        // Handle member access (property): x => x.Property
        if (methodSelector.Body is MemberExpression { Member: PropertyInfo property })
        {
            // For properties, we need to get the getter method
            return property.GetMethod 
                ?? throw new ArgumentException($"Property '{property.Name}' does not have a getter", nameof(methodSelector));
        }

        throw new ArgumentException(
            "Method selector must be a method call expression (e.g., x => x.MethodName(args)) or property access",
            nameof(methodSelector));
    }
}

/// <summary>
/// Internal configuration for a service tool.
/// </summary>
internal sealed class ServiceToolConfiguration
{
    public required Type ServiceType { get; init; }
    public required MethodInfo Method { get; init; }
    public required string ToolName { get; init; }
    public required string Description { get; init; }
}
