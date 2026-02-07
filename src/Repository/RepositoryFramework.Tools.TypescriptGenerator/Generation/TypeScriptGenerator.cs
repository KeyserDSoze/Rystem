using System.Text;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.Services;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.Transformers;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using RepositoryFramework.Tools.TypescriptGenerator.Utils;

namespace RepositoryFramework.Tools.TypescriptGenerator.Generation;

/// <summary>
/// Orchestrates TypeScript code generation from analyzed models.
/// </summary>
public class TypeScriptGenerator
{
    private readonly FileWriter _fileWriter;
    private readonly EmitterContext _context;
    private readonly DependencyGraph? _dependencyGraph;

    public TypeScriptGenerator(
        string destinationPath,
        EmitterContext context,
        DependencyGraph? dependencyGraph,
        bool overwrite = true)
    {
        _fileWriter = new FileWriter(destinationPath, overwrite);
        _context = context;
        _dependencyGraph = dependencyGraph;
    }

    /// <summary>
    /// Generates all TypeScript files for the given models.
    /// </summary>
    public void Generate(
        IEnumerable<ModelDescriptor> models,
        IEnumerable<ModelDescriptor> keys)
    {
        _fileWriter.EnsureDirectories();

        var generatedFiles = new List<string>();

        // Filter out closed generic types - we only generate open generics (EntityVersions<T>)
        // Closed generics (EntityVersions<Book>) reuse the open generic definition
        // Check GenericBaseTypeName: NULL = open generic or non-generic, NON-NULL = closed generic
        var modelsToGenerate = models.Where(m => m.GenericBaseTypeName == null).ToList();

        Logger.Info($"Filtered {models.Count()} models to {modelsToGenerate.Count} for generation (excluded {models.Count() - modelsToGenerate.Count} closed generics)");

        // Generate files for main models
        foreach (var model in modelsToGenerate)
        {
            var fileName = GenerateTypeFile(model);
            if (!generatedFiles.Contains(fileName))
            {
                generatedFiles.Add(fileName);
            }
        }

        // Generate files for keys (if not already generated)
        foreach (var key in keys)
        {
            if (!generatedFiles.Any(f => f.Equals(key.GetFileName(), StringComparison.OrdinalIgnoreCase)))
            {
                var fileName = GenerateTypeFile(key);
                generatedFiles.Add(fileName);
            }
        }

        // Generate index.ts for types folder
        _fileWriter.WriteIndexFile("types", generatedFiles);
        Logger.Info("Generated types/index.ts");
    }

    /// <summary>
    /// Generates all TypeScript files including transformers and bootstrap for the given repositories.
    /// </summary>
    public void GenerateWithServices(
        IEnumerable<ModelDescriptor> models,
        IEnumerable<ModelDescriptor> keys,
        IEnumerable<RepositoryDescriptor> repositories)
    {
        // First generate types
        Generate(models, keys);

        // Use TypeScriptName as key for generics to avoid collision
        // EntityVersions<Book> and EntityVersions<Chapter> have same Name ("EntityVersions`1")
        // but different TypeScriptName ("EntityVersions<Book>" vs "EntityVersions<Chapter>")
        var modelsDict = models.ToDictionary(m => m.TypeScriptName);
        var keysDict = keys.ToDictionary(k => k.TypeScriptName);
        var repositoryList = repositories.ToList();

        // Generate transformers
        GenerateTransformers(repositoryList, modelsDict, keysDict);

        // Generate bootstrap and locator
        GenerateBootstrapAndLocator(repositoryList, modelsDict, keysDict);
    }

    /// <summary>
    /// Generates transformer files for all models and complex keys used by repositories.
    /// </summary>
    private void GenerateTransformers(
        IEnumerable<RepositoryDescriptor> repositories,
        Dictionary<string, ModelDescriptor> models,
        Dictionary<string, ModelDescriptor> keys)
    {
        var generatedTransformers = new HashSet<string>();
        var transformerFiles = new List<string>();

        foreach (var repo in repositories)
        {
            var modelSimpleName = GetSimpleName(repo.ModelName);
            var keySimpleName = GetSimpleName(repo.KeyName);

            // Generate model transformer
            ModelDescriptor? model = null;
            if (models.TryGetValue(modelSimpleName, out model))
            {
                // If it's a closed generic, find the open generic instead
                if (model.GenericBaseTypeName != null)
                {
                    model = FindOpenGenericForClosedGeneric(modelSimpleName, models.Values) ?? model;
                }
            }
            else
            {
                // Try to find open generic for closed generic type
                // EntityVersions<Book> -> EntityVersions<T>
                model = FindOpenGenericForClosedGeneric(modelSimpleName, models.Values);
            }

            if (model != null && !model.IsEnum)
            {
                var transformerKey = model.TypeScriptName; // Use TypeScriptName as unique key
                if (!generatedTransformers.Contains(transformerKey))
                {
                    var content = TransformerEmitter.Emit(model);
                    if (!string.IsNullOrEmpty(content))
                    {
                        var fileName = TransformerEmitter.GetFileName(model);
                        _fileWriter.WriteTransformerFile(fileName, content);
                        transformerFiles.Add(fileName);
                        generatedTransformers.Add(transformerKey);
                        Logger.Info($"Generated transformers/{fileName}");
                    }
                }
            }

            // Generate key transformer (if not primitive)
            if (!repo.IsPrimitiveKey)
            {
                ModelDescriptor? key = null;
                if (keys.TryGetValue(keySimpleName, out key))
                {
                    // If it's a closed generic, find the open generic instead
                    if (key.GenericBaseTypeName != null)
                    {
                        key = FindOpenGenericForClosedGeneric(keySimpleName, keys.Values) ?? key;
                    }
                }
                else
                {
                    // Try to find open generic
                    key = FindOpenGenericForClosedGeneric(keySimpleName, keys.Values);
                }

                if (key != null && !key.IsEnum)
                {
                    var transformerKey = key.TypeScriptName;
                    if (!generatedTransformers.Contains(transformerKey))
                    {
                        var content = TransformerEmitter.Emit(key, isKey: true);
                        if (!string.IsNullOrEmpty(content))
                        {
                            var fileName = TransformerEmitter.GetFileName(key);
                            _fileWriter.WriteTransformerFile(fileName, content);
                            transformerFiles.Add(fileName);
                            generatedTransformers.Add(transformerKey);
                            Logger.Info($"Generated transformers/{fileName}");
                        }
                    }
                }
            }
        }

        // Generate index.ts for transformers folder
        if (transformerFiles.Count > 0)
        {
            _fileWriter.WriteIndexFile("transformers", transformerFiles);
            Logger.Info("Generated transformers/index.ts");
        }
    }

    /// <summary>
    /// Finds the open generic model for a closed generic type.
    /// Example: "EntityVersions<Book>" -> model with TypeScriptName "EntityVersions<T>"
    /// </summary>
    public static ModelDescriptor? FindOpenGenericForClosedGeneric(
        string closedGenericName,
        IEnumerable<ModelDescriptor> models)
    {
        // Check if it's a generic type (contains <)
        if (!closedGenericName.Contains('<'))
            return null;

        // Extract base name: "EntityVersions<Book>" -> "EntityVersions"
        var baseName = closedGenericName[..closedGenericName.IndexOf('<')];

        // Find open generic: EntityVersions<T>, EntityVersions<TKey>, etc.
        return models.FirstOrDefault(m => 
            m.IsGenericType && 
            m.GenericBaseTypeName == null && // Open generic
            m.TypeScriptName.StartsWith($"{baseName}<") &&
            m.GenericTypeParameters.Count > 0);
    }

    /// <summary>
    /// Generates bootstrap setup and repository locator files.
    /// </summary>
    private void GenerateBootstrapAndLocator(
        List<RepositoryDescriptor> repositories,
        Dictionary<string, ModelDescriptor> models,
        Dictionary<string, ModelDescriptor> keys)
    {
        // Generate bootstrap file
        var bootstrapContent = BootstrapEmitter.Emit(repositories, models, keys);
        _fileWriter.WriteBootstrapFile("repositorySetup.ts", bootstrapContent);
        Logger.Info("Generated bootstrap/repositorySetup.ts");

        // Generate repository locator file in services folder
        var locatorContent = RepositoryLocatorEmitter.Emit(repositories, models, keys);
        _fileWriter.WriteServiceFile(RepositoryLocatorEmitter.GetFileName(), locatorContent);
        Logger.Info($"Generated {RepositoryLocatorEmitter.GetFolder()}/{RepositoryLocatorEmitter.GetFileName()}");
    }

    private static string GetSimpleName(string fullName)
    {
        var lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
    }

    /// <summary>
    /// Generates a complete TypeScript file for a model.
    /// </summary>
    private string GenerateTypeFile(ModelDescriptor model)
    {
        var sb = new StringBuilder();
        var fileName = model.GetFileName(); // Use the new method that handles generics properly

        // Header comment
        sb.AppendLine("// ============================================");
        sb.AppendLine($"// {model.TypeScriptName} Types");
        sb.AppendLine("// Auto-generated by Rystem TypeScript Generator");
        sb.AppendLine("// ============================================");
        sb.AppendLine();

        // Collect all types to include in this file
        var typesToInclude = GetTypesToIncludeInFile(model);

        // Generate imports
        var imports = ImportResolver.ResolveImports(model.Name, model, _context, _dependencyGraph);
        if (!string.IsNullOrEmpty(imports))
        {
            sb.Append(imports);
        }

        // Section: Enums
        var enums = typesToInclude.Where(t => t.IsEnum).ToList();
        if (enums.Count > 0)
        {
            sb.AppendLine("// ============================================");
            sb.AppendLine("// ENUMS");
            sb.AppendLine("// ============================================");
            sb.AppendLine();

            foreach (var enumType in enums)
            {
                sb.AppendLine(EnumEmitter.Emit(enumType));
                sb.AppendLine();
            }
        }

        // Section: Raw Interfaces
        var rawTypes = typesToInclude.Where(t => !t.IsEnum && t.RequiresRawType).ToList();
        if (rawTypes.Count > 0)
        {
            sb.AppendLine("// ============================================");
            sb.AppendLine("// RAW INTERFACES (matching JSON property names)");
            sb.AppendLine("// ============================================");
            sb.AppendLine();

            foreach (var rawType in rawTypes)
            {
                sb.AppendLine(RawTypeEmitter.Emit(rawType, _context));
                sb.AppendLine();
            }
        }

        // Section: Clean Interfaces
        var cleanTypes = typesToInclude.Where(t => !t.IsEnum).ToList();
        if (cleanTypes.Count > 0)
        {
            sb.AppendLine("// ============================================");
            sb.AppendLine("// CLEAN INTERFACES (readable property names)");
            sb.AppendLine("// ============================================");
            sb.AppendLine();

            foreach (var cleanType in cleanTypes)
            {
                sb.AppendLine(CleanTypeEmitter.Emit(cleanType, _context));
                sb.AppendLine();
            }
        }

        // Section: Mapping Functions
        if (rawTypes.Count > 0)
        {
            sb.AppendLine("// ============================================");
            sb.AppendLine("// MAPPING FUNCTIONS");
            sb.AppendLine("// ============================================");
            sb.AppendLine();

            foreach (var mappedType in rawTypes)
            {
                sb.AppendLine(MapperEmitter.Emit(mappedType, _context));
                sb.AppendLine();
            }
        }

        // Section: Helper Classes
        var helpers = HelperEmitter.EmitAll(cleanTypes, _context);
        if (!string.IsNullOrWhiteSpace(helpers))
        {
            sb.AppendLine("// ============================================");
            sb.AppendLine("// HELPER CLASSES");
            sb.AppendLine("// ============================================");
            sb.AppendLine();
            sb.AppendLine(helpers);
        }

        var content = sb.ToString().TrimEnd() + Environment.NewLine;
        _fileWriter.WriteTypeFile(fileName, content);

        return fileName;
    }

    /// <summary>
    /// Gets all types that should be included in a model's file.
    /// </summary>
    private List<ModelDescriptor> GetTypesToIncludeInFile(ModelDescriptor model)
    {
        var types = new List<ModelDescriptor> { model };

        // Add owned nested types
        foreach (var nested in model.NestedTypes)
        {
            if (_context.IsOwnedBy(nested.Name, model.Name))
            {
                types.Add(nested);
            }
        }

        return types;
    }
}
