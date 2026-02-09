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
    /// Collects imports from ALL types included in the file (root model + owned nested types)
    /// so that nested types' dependencies on external enums/classes are properly imported.
    /// </summary>
    public static string ResolveImports(
        string modelName,
        IEnumerable<ModelDescriptor> typesInFile,
        EmitterContext context,
        DependencyGraph? dependencyGraph)
    {
        var imports = new Dictionary<string, HashSet<string>>(); // file -> types

        // Collect types that need to be imported from ALL types in this file
        foreach (var typeInFile in typesInFile)
        {
            // Enums don't have properties — nothing to import
            if (!typeInFile.IsEnum)
            {
                CollectImports(typeInFile, modelName, context, imports);
            }
        }

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
        // Primitives - check for date types that need utility imports
        if (type.IsPrimitive)
        {
            if (type.IsDate)
            {
                CollectDateImports(type.DateKind!.Value, imports);
            }
            return;
        }

        // Handle enums
        if (type.IsEnum)
        {
            AddImportIfExternal(type.CSharpName, currentModelName, context, imports);
            return;
        }

        // Handle union types (AnyOf<T0, T1, ...>) - recurse into each member
        if (type.IsUnion && type.UnionTypes != null)
        {
            foreach (var unionMember in type.UnionTypes)
                CollectTypeImports(unionMember, currentModelName, context, imports);
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
    /// Collects date parse/format function imports from DateMappers utility.
    /// </summary>
    private static void CollectDateImports(DateTypeKind dateKind, Dictionary<string, HashSet<string>> imports)
    {
        const string dateMapperFile = "DateMappers";
        if (!imports.ContainsKey(dateMapperFile))
            imports[dateMapperFile] = [];

        var (parseFn, formatFn) = dateKind switch
        {
            DateTypeKind.DateTime => ("parseDateTime", "formatDateTime"),
            DateTypeKind.DateTimeOffset => ("parseDateTimeOffset", "formatDateTimeOffset"),
            DateTypeKind.DateOnly => ("parseDateOnly", "formatDateOnly"),
            _ => throw new ArgumentOutOfRangeException(nameof(dateKind))
        };

        imports[dateMapperFile].Add(parseFn);
        imports[dateMapperFile].Add(formatFn);
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
    /// Generates the import statements string with proper verbatimModuleSyntax support.
    /// Types use "import type", functions use "import".
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

            // Separate types from functions (mappers and date utilities)
            var typeImports = types.Where(t => !IsFunction(t)).OrderBy(t => t).ToList();
            var functionImports = types.Where(t => IsFunction(t)).OrderBy(t => t).ToList();

            // Emit type imports with "import type"
            if (typeImports.Count > 0)
            {
                var typesString = string.Join(", ", typeImports);
                sb.AppendLine($"import type {{ {typesString} }} from './{file}';");
            }

            // Emit function imports with "import"
            if (functionImports.Count > 0)
            {
                var functionsString = string.Join(", ", functionImports);
                sb.AppendLine($"import {{ {functionsString} }} from './{file}';");
            }
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

    /// <summary>
    /// Determines if an import name represents a function (mapper or date utility)
    /// rather than a type.
    /// </summary>
    private static bool IsFunction(string name)
        => name.StartsWith("map") || name.StartsWith("parse") || name.StartsWith("format");
}
