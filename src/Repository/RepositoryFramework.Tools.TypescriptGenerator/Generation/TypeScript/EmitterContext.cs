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
            // Get base name without generic parameters for lookup
            var baseName = model.Name;
            if (baseName.Contains('`'))
            {
                baseName = baseName[..baseName.IndexOf('`')];
            }

            context.AllModels[model.Name] = model;
            if (model.RequiresRawType)
            {
                context.TypesRequiringRaw.Add(baseName); // Use base name without generics
            }
            if (model.IsEnum)
            {
                context.EnumTypes.Add(baseName); // Use base name without generics
            }

            // Process nested types
            ProcessNestedTypes(model, context);
        }

        foreach (var key in keys)
        {
            // Get base name without generic parameters for lookup
            var baseName = key.Name;
            if (baseName.Contains('`'))
            {
                baseName = baseName[..baseName.IndexOf('`')];
            }

            context.AllModels[key.Name] = key;
            if (key.RequiresRawType)
            {
                context.TypesRequiringRaw.Add(baseName); // Use base name without generics
            }
            if (key.IsEnum)
            {
                context.EnumTypes.Add(baseName); // Use base name without generics
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
            // Get base name without generic parameters for lookup
            var baseName = nested.Name;
            if (baseName.Contains('`'))
            {
                baseName = baseName[..baseName.IndexOf('`')];
            }

            // Prefer open generics (with GenericTypeParameters) over closed generics.
            // This ensures IsNestedGenericModel can correctly identify generic models
            // even when both closed (EntityVersion<ExtendedBook>) and open (EntityVersion<T>)
            // share the same Name ("EntityVersion`1").
            if (!context.AllModels.ContainsKey(nested.Name) ||
                (nested.IsGenericType && !context.AllModels[nested.Name].IsGenericType))
            {
                context.AllModels[nested.Name] = nested;
            }

            if (nested.RequiresRawType)
            {
                context.TypesRequiringRaw.Add(baseName); // Use base name without generics
            }

            if (nested.IsEnum)
            {
                context.EnumTypes.Add(baseName); // Use base name without generics
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
