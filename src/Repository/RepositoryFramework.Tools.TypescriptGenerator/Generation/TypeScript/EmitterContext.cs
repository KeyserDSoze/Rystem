using RepositoryFramework.Tools.TypescriptGenerator.Domain;

namespace RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;

/// <summary>
/// Context for TypeScript emission containing shared state.
/// </summary>
public class EmitterContext
{
    /// <summary>
    /// Set of type names that require Raw interface generation.
    /// </summary>
    public HashSet<string> TypesRequiringRaw { get; } = [];

    /// <summary>
    /// Set of type names that are enums.
    /// </summary>
    public HashSet<string> EnumTypes { get; } = [];

    /// <summary>
    /// Map of type name to the model that owns it.
    /// </summary>
    public Dictionary<string, string> TypeOwnership { get; } = [];

    /// <summary>
    /// All analyzed models by name.
    /// </summary>
    public Dictionary<string, ModelDescriptor> AllModels { get; } = [];

    /// <summary>
    /// Creates an EmitterContext from analysis results.
    /// </summary>
    public static EmitterContext FromAnalysisResult(
        IEnumerable<ModelDescriptor> models,
        IEnumerable<ModelDescriptor> keys,
        Dictionary<string, string> typeOwnership)
    {
        var context = new EmitterContext();

        // Add all models and keys
        foreach (var model in models)
        {
            context.AllModels[model.Name] = model;
            if (model.RequiresRawType)
            {
                context.TypesRequiringRaw.Add(model.Name);
            }
            if (model.IsEnum)
            {
                context.EnumTypes.Add(model.Name);
            }

            // Process nested types
            ProcessNestedTypes(model, context);
        }

        foreach (var key in keys)
        {
            context.AllModels[key.Name] = key;
            if (key.RequiresRawType)
            {
                context.TypesRequiringRaw.Add(key.Name);
            }
            if (key.IsEnum)
            {
                context.EnumTypes.Add(key.Name);
            }

            ProcessNestedTypes(key, context);
        }

        // Copy ownership
        foreach (var (typeName, owner) in typeOwnership)
        {
            context.TypeOwnership[typeName] = owner;
        }

        return context;
    }

    private static void ProcessNestedTypes(ModelDescriptor model, EmitterContext context)
    {
        foreach (var nested in model.NestedTypes)
        {
            if (!context.AllModels.ContainsKey(nested.Name))
            {
                context.AllModels[nested.Name] = nested;
            }

            if (nested.RequiresRawType)
            {
                context.TypesRequiringRaw.Add(nested.Name);
            }

            if (nested.IsEnum)
            {
                context.EnumTypes.Add(nested.Name);
            }

            ProcessNestedTypes(nested, context);
        }
    }

    /// <summary>
    /// Gets the file name where a type should be defined.
    /// </summary>
    public string GetOwnerFileName(string typeName)
    {
        if (TypeOwnership.TryGetValue(typeName, out var owner))
        {
            return $"{owner.ToLowerInvariant()}.ts";
        }
        return $"{typeName.ToLowerInvariant()}.ts";
    }

    /// <summary>
    /// Checks if a type is owned by the specified model.
    /// </summary>
    public bool IsOwnedBy(string typeName, string modelName)
    {
        if (TypeOwnership.TryGetValue(typeName, out var owner))
        {
            return string.Equals(owner, modelName, StringComparison.OrdinalIgnoreCase);
        }
        return string.Equals(typeName, modelName, StringComparison.OrdinalIgnoreCase);
    }
}
