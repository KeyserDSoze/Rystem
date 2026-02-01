using System.Text;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;

namespace RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;

/// <summary>
/// Resolves and generates import statements for TypeScript files.
/// </summary>
public static class ImportResolver
{
    /// <summary>
    /// Generates import statements for a model file.
    /// </summary>
    public static string ResolveImports(
        string modelName,
        ModelDescriptor model,
        EmitterContext context,
        DependencyGraph? dependencyGraph)
    {
        var imports = new Dictionary<string, HashSet<string>>(); // file -> types

        // Collect types that need to be imported
        CollectImports(model, modelName, context, imports);

        // Generate import statements
        return GenerateImportStatements(imports, modelName);
    }

    /// <summary>
    /// Collects all types that need to be imported for a model.
    /// </summary>
    private static void CollectImports(
        ModelDescriptor model,
        string currentModelName,
        EmitterContext context,
        Dictionary<string, HashSet<string>> imports)
    {
        foreach (var property in model.Properties)
        {
            CollectTypeImports(property.Type, currentModelName, context, imports);
        }
    }

    /// <summary>
    /// Recursively collects imports for a type.
    /// </summary>
    private static void CollectTypeImports(
        TypeDescriptor type,
        string currentModelName,
        EmitterContext context,
        Dictionary<string, HashSet<string>> imports)
    {
        // Skip primitives
        if (type.IsPrimitive)
            return;

        // Handle enums
        if (type.IsEnum)
        {
            AddImportIfExternal(type.CSharpName, currentModelName, context, imports);
            return;
        }

        // Handle arrays
        if (type.IsArray && type.ElementType != null)
        {
            CollectTypeImports(type.ElementType, currentModelName, context, imports);
            return;
        }

        // Handle dictionaries
        if (type.IsDictionary)
        {
            if (type.KeyType != null)
                CollectTypeImports(type.KeyType, currentModelName, context, imports);
            if (type.ValueType != null)
                CollectTypeImports(type.ValueType, currentModelName, context, imports);
            return;
        }

        // Complex type
        AddImportIfExternal(type.CSharpName, currentModelName, context, imports);

        // Also add Raw type if needed
        if (context.TypesRequiringRaw.Contains(type.CSharpName))
        {
            AddImportIfExternal($"{type.CSharpName}Raw", currentModelName, context, imports, isRaw: true);
        }
    }

    /// <summary>
    /// Adds a type to imports if it's defined in another file.
    /// </summary>
    private static void AddImportIfExternal(
        string typeName,
        string currentModelName,
        EmitterContext context,
        Dictionary<string, HashSet<string>> imports,
        bool isRaw = false)
    {
        // Get the base type name (without Raw suffix)
        var baseTypeName = isRaw ? typeName[..^3] : typeName;

        // Check if this type is owned by another model
        if (!context.IsOwnedBy(baseTypeName, currentModelName))
        {
            var ownerFile = context.GetOwnerFileName(baseTypeName);
            var ownerName = ownerFile.Replace(".ts", "");

            if (!imports.ContainsKey(ownerName))
                imports[ownerName] = [];

            imports[ownerName].Add(typeName);

            // Also import mapper functions if needed
            if (context.TypesRequiringRaw.Contains(baseTypeName) && !isRaw)
            {
                imports[ownerName].Add($"mapRaw{baseTypeName}To{baseTypeName}");
                imports[ownerName].Add($"map{baseTypeName}ToRaw{baseTypeName}");
            }
        }
    }

    /// <summary>
    /// Generates the import statements string.
    /// </summary>
    private static string GenerateImportStatements(
        Dictionary<string, HashSet<string>> imports,
        string currentModelName)
    {
        if (imports.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();

        foreach (var (file, types) in imports.OrderBy(x => x.Key))
        {
            if (types.Count == 0)
                continue;

            var sortedTypes = types.OrderBy(t => t).ToList();
            var typesString = string.Join(", ", sortedTypes);

            // Use relative import path
            sb.AppendLine($"import {{ {typesString} }} from './{file}';");
        }

        if (sb.Length > 0)
            sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Gets all types that should be exported from a model's file.
    /// </summary>
    public static IEnumerable<string> GetExportedTypes(
        string modelName,
        ModelDescriptor model,
        EmitterContext context)
    {
        var exports = new List<string>();

        // Main type
        exports.Add(model.Name);

        // Raw type if needed
        if (model.RequiresRawType)
        {
            exports.Add($"{model.Name}Raw");
            exports.Add($"mapRaw{model.Name}To{model.Name}");
            exports.Add($"map{model.Name}ToRaw{model.Name}");
        }

        // Helper if generated
        var hasArrayOrDict = model.Properties.Any(p => p.Type.IsArray || p.Type.IsDictionary);
        if (hasArrayOrDict)
        {
            exports.Add($"{model.Name}Helper");
        }

        // Owned nested types and enums
        foreach (var nested in model.NestedTypes)
        {
            if (context.IsOwnedBy(nested.Name, modelName))
            {
                exports.Add(nested.Name);
                if (nested.RequiresRawType)
                {
                    exports.Add($"{nested.Name}Raw");
                    exports.Add($"mapRaw{nested.Name}To{nested.Name}");
                    exports.Add($"map{nested.Name}ToRaw{nested.Name}");
                }
            }
        }

        return exports;
    }
}
