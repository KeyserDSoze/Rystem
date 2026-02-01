using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Rules;
using RepositoryFramework.Tools.TypescriptGenerator.Utils;

namespace RepositoryFramework.Tools.TypescriptGenerator.Analysis;

/// <summary>
/// Orchestrates the complete analysis pipeline.
/// </summary>
public class AnalysisPipeline
{
    private readonly AssemblyLoader _assemblyLoader;
    private readonly ModelAnalyzer _modelAnalyzer;
    private readonly DependencyGraph _dependencyGraph;

    public AnalysisPipeline()
    {
        _assemblyLoader = new AssemblyLoader();
        _modelAnalyzer = new ModelAnalyzer();
        _dependencyGraph = new DependencyGraph();
    }

    /// <summary>
    /// Analyzes the specified repositories and returns the analysis result.
    /// </summary>
    public async Task<AnalysisResult> AnalyzeAsync(
        GenerationContext context,
        CancellationToken cancellationToken = default)
    {
        var result = new AnalysisResult();

        try
        {
            // Step 1: Load the assembly
            Logger.Step("Loading assembly...");

            if (context.ProjectPath != null)
            {
                var assembly = await _assemblyLoader.BuildAndLoadProjectAsync(
                    context.ProjectPath,
                    context.IncludeDependencies,
                    context.DependencyPrefix,
                    cancellationToken);

                if (assembly == null)
                {
                    result.Errors.Add("Failed to build and load the project assembly.");
                    return result;
                }
            }
            else
            {
                result.Errors.Add("No project path specified. Please use --project to specify the C# project.");
                return result;
            }

            // Step 2: Analyze each repository descriptor
            Logger.Step("Analyzing models...");

            foreach (var repo in context.Repositories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Find the model type
                var modelType = _assemblyLoader.FindType(repo.ModelName);
                if (modelType == null)
                {
                    result.Warnings.Add($"Model type '{repo.ModelName}' not found in assembly.");
                    continue;
                }

                // Analyze the model
                var modelDescriptor = _modelAnalyzer.Analyze(modelType, depth: 0);
                result.Models[repo.ModelName] = modelDescriptor;
                _dependencyGraph.AddModel(modelDescriptor);

                Logger.Info($"  ✓ Analyzed: {repo.ModelName} ({modelDescriptor.Properties.Count} properties)");

                // Find and analyze the key type (if not primitive)
                if (!PrimitiveTypeRules.IsPrimitiveName(repo.KeyName))
                {
                    var keyType = _assemblyLoader.FindType(repo.KeyName);
                    if (keyType == null)
                    {
                        result.Warnings.Add($"Key type '{repo.KeyName}' not found in assembly.");
                    }
                    else
                    {
                        var keyDescriptor = _modelAnalyzer.Analyze(keyType, depth: 0);
                        result.Keys[repo.KeyName] = keyDescriptor;
                        _dependencyGraph.AddModel(keyDescriptor);

                        Logger.Info($"  ✓ Analyzed: {repo.KeyName} (key type, {keyDescriptor.Properties.Count} properties)");
                    }
                }
            }

            // Step 3: Build dependency information
            Logger.Step("Resolving dependencies...");

            result.DependencyGraph = _dependencyGraph;
            result.TypeOwnership = _dependencyGraph.GetAllOwnership()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OwnerModel);

            // Log ownership info
            var sharedTypes = result.TypeOwnership
                .Where(kvp => !result.Models.ContainsKey(kvp.Key) && !result.Keys.ContainsKey(kvp.Key))
                .ToList();

            if (sharedTypes.Count != 0)
            {
                Logger.Info($"  Found {sharedTypes.Count} shared/nested types:");
                foreach (var (typeName, owner) in sharedTypes.Take(10))
                {
                    Logger.Info($"    • {typeName} → owned by {owner}");
                }
                if (sharedTypes.Count > 10)
                {
                    Logger.Info($"    ... and {sharedTypes.Count - 10} more");
                }
            }

            // Step 4: Get topological order for generation
            result.GenerationOrder = _dependencyGraph.GetTopologicalOrder().ToList();

            Logger.Success($"Analysis complete: {result.Models.Count} models, {result.Keys.Count} keys");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Analysis failed: {ex.Message}");
            Logger.Error(ex.Message);
        }
        finally
        {
            // Cleanup
            _assemblyLoader.Unload();
        }

        return result;
    }
}

/// <summary>
/// Contains the results of the analysis pipeline.
/// </summary>
public class AnalysisResult
{
    /// <summary>
    /// Analyzed model descriptors by name.
    /// </summary>
    public Dictionary<string, ModelDescriptor> Models { get; } = [];

    /// <summary>
    /// Analyzed key descriptors by name.
    /// </summary>
    public Dictionary<string, ModelDescriptor> Keys { get; } = [];

    /// <summary>
    /// Type ownership mapping (type name → owner model name).
    /// </summary>
    public Dictionary<string, string> TypeOwnership { get; set; } = [];

    /// <summary>
    /// The dependency graph.
    /// </summary>
    public DependencyGraph? DependencyGraph { get; set; }

    /// <summary>
    /// Order in which to generate files (dependencies first).
    /// </summary>
    public List<string> GenerationOrder { get; set; } = [];

    /// <summary>
    /// Any errors that occurred during analysis.
    /// </summary>
    public List<string> Errors { get; } = [];

    /// <summary>
    /// Any warnings that occurred during analysis.
    /// </summary>
    public List<string> Warnings { get; } = [];

    /// <summary>
    /// Whether the analysis was successful (no errors).
    /// </summary>
    public bool IsSuccess => Errors.Count == 0;
}
