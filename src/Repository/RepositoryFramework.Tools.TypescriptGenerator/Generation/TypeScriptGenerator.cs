using System.Text;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.Services;
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

        // Generate files for main models
        foreach (var model in models)
        {
            var fileName = GenerateTypeFile(model);
            generatedFiles.Add(fileName);
        }

        // Generate files for keys (if not already generated)
        foreach (var key in keys)
        {
            if (!generatedFiles.Any(f => f.Equals($"{key.Name.ToLowerInvariant()}.ts", StringComparison.OrdinalIgnoreCase)))
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
    /// Generates all TypeScript files including services for the given repositories.
    /// </summary>
    public void GenerateWithServices(
        IEnumerable<ModelDescriptor> models,
        IEnumerable<ModelDescriptor> keys,
        IEnumerable<RepositoryDescriptor> repositories)
    {
        // First generate types
        Generate(models, keys);

        // Then generate services
        GenerateServices(repositories, models.ToDictionary(m => m.Name), keys.ToDictionary(k => k.Name));
    }

    /// <summary>
    /// Generates service files for the given repositories.
    /// </summary>
    private void GenerateServices(
        IEnumerable<RepositoryDescriptor> repositories,
        Dictionary<string, ModelDescriptor> models,
        Dictionary<string, ModelDescriptor> keys)
    {
        var repositoryList = repositories.ToList();
        var generatedServiceFiles = new List<string>();
        var serviceFileGenerator = new ServiceFileGenerator(_context);

        // Generate common types file
        var commonContent = CommonTypesEmitter.Emit();
        _fileWriter.WriteServiceFile(CommonTypesEmitter.GetFileName(), commonContent);

        // Generate individual service files
        foreach (var repo in repositoryList)
        {
            // Get simple name for lookup (handle fully qualified names)
            var modelSimpleName = GetSimpleName(repo.ModelName);

            if (!models.TryGetValue(modelSimpleName, out var model))
            {
                Logger.Warning($"Model '{repo.ModelName}' not found for repository '{repo.FactoryName}'");
                continue;
            }

            // Get simple name for key lookup
            var keySimpleName = GetSimpleName(repo.KeyName);
            keys.TryGetValue(keySimpleName, out var key);

            var serviceContent = serviceFileGenerator.Generate(repo, model, key);
            var fileName = ServiceFileGenerator.GetFileName(repo);
            _fileWriter.WriteServiceFile(fileName, serviceContent);
            generatedServiceFiles.Add(fileName);
        }

        // Generate service registry (index.ts)
        var registryContent = ServiceRegistryEmitter.Emit(repositoryList);
        _fileWriter.WriteServiceFile(ServiceRegistryEmitter.GetFileName(), registryContent);
        Logger.Info("Generated services/index.ts");

        // Generate bootstrap file
        var bootstrapContent = BootstrapEmitter.Emit(repositoryList, models, keys);
        _fileWriter.WriteBootstrapFile("repositorySetup.ts", bootstrapContent);
        Logger.Info("Generated bootstrap/repositorySetup.ts");
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
        var fileName = $"{model.Name.ToLowerInvariant()}.ts";

        // Header comment
        sb.AppendLine("// ============================================");
        sb.AppendLine($"// {model.Name} Types");
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
