using System.CommandLine;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using RepositoryFramework.Tools.TypescriptGenerator.Utils;

namespace RepositoryFramework.Tools.TypescriptGenerator.Cli;

/// <summary>
/// Defines and handles the "generate" command for the CLI.
/// Usage: rystem-ts generate --dest ./src/api --models [{Calendar,LeagueKey,Repository,serieA}]
/// </summary>
public static class GenerateCommand
{
    public static Command Create()
    {
        var destOption = new Option<string>(
            aliases: ["--dest", "-d"],
            description: "Destination folder for generated TypeScript files")
        {
            IsRequired = true
        };

        var modelsOption = new Option<string>(
            aliases: ["--models", "-m"],
            description: "Repository definitions in format: \"{Model,Key,Type,Factory,BackendFactory},{...}\"")
        {
            IsRequired = true
        };

        var projectOption = new Option<string?>(
            aliases: ["--project", "-p"],
            description: "Path to the C# project (.csproj) or assembly (.dll). " +
                         "If not specified, searches for a .csproj in the current directory");

        var overwriteOption = new Option<bool>(
            aliases: ["--overwrite"],
            description: "Overwrite existing files (default: true)",
            getDefaultValue: () => true);

        var includeDepsOption = new Option<bool>(
            aliases: ["--include-deps"],
            description: "Include project dependencies (referenced projects and NuGet packages). " +
                         "When true, all DLLs in the output directory will be scanned for types. (default: false)",
            getDefaultValue: () => false);

        var depsPrefixOption = new Option<string?>(
            aliases: ["--deps-prefix"],
            description: "When --include-deps is true, only load dependencies whose name starts with this prefix. " +
                         "Example: 'MyCompany.' will only load 'MyCompany.Core.dll', 'MyCompany.Models.dll', etc.");

        var command = new Command("generate", "Generate TypeScript types and services from C# models")
        {
            destOption,
            modelsOption,
            projectOption,
            overwriteOption,
            includeDepsOption,
            depsPrefixOption
        };

        command.SetHandler(HandleGenerateAsync, destOption, modelsOption, projectOption, overwriteOption, includeDepsOption, depsPrefixOption);

        return command;
    }

    private static async Task HandleGenerateAsync(
        string dest,
        string models,
        string? project,
        bool overwrite,
        bool includeDeps,
        string? depsPrefix)
    {
        try
        {
            // Validate models format
            var (isValid, errorMessage) = ModelDescriptorParser.Validate(models);
            if (!isValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: {errorMessage}");
                Console.ResetColor();
                Console.WriteLine();
                PrintUsageExample();
                Environment.ExitCode = 1;
                return;
            }

            // Parse models
            var repositories = ModelDescriptorParser.Parse(models);

            // Resolve project path
            var projectPath = ResolveProjectPath(project);

            // Build context
            var context = new GenerationContext
            {
                DestinationPath = Path.GetFullPath(dest),
                Repositories = repositories,
                ProjectPath = projectPath,
                Overwrite = overwrite,
                IncludeDependencies = includeDeps,
                DependencyPrefix = depsPrefix
            };

            // Print configuration
            PrintConfiguration(context);

            // TODO: Execute generation (Phase 2+)
            await ExecuteGenerationAsync(context);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Generation completed successfully!");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            Environment.ExitCode = 1;
        }
    }

    private static string? ResolveProjectPath(string? specifiedPath)
    {
        if (!string.IsNullOrWhiteSpace(specifiedPath))
        {
            var fullPath = Path.GetFullPath(specifiedPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Project file not found: {fullPath}");
            return fullPath;
        }

        // Search for .csproj in current directory
        var csprojFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj");
        return csprojFiles.Length switch
        {
            0 => null, // Will need assembly path or other discovery
            1 => csprojFiles[0],
            _ => throw new InvalidOperationException(
                $"Multiple .csproj files found. Please specify one with --project: " +
                string.Join(", ", csprojFiles.Select(Path.GetFileName)))
        };
    }

    private static void PrintConfiguration(GenerationContext context)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         Rystem TypeScript Generator                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();

        Console.WriteLine($"📁 Destination: {context.DestinationPath}");
        Console.WriteLine($"📦 Project: {context.ProjectPath ?? "(auto-discover)"}");
        Console.WriteLine($"🔄 Overwrite: {context.Overwrite}");
        Console.WriteLine($"📚 Include Dependencies: {context.IncludeDependencies}");
        if (context.IncludeDependencies && !string.IsNullOrWhiteSpace(context.DependencyPrefix))
        {
            Console.WriteLine($"🔍 Dependency Prefix: {context.DependencyPrefix}");
        }
        Console.WriteLine();

        Console.WriteLine("📋 Repositories to generate:");
        Console.WriteLine("┌────────────────────┬────────────────────┬────────────┬────────────────────┐");
        Console.WriteLine("│ Model              │ Key                │ Type       │ Factory            │");
        Console.WriteLine("├────────────────────┼────────────────────┼────────────┼────────────────────┤");

        foreach (var repo in context.Repositories)
        {
            var model = repo.ModelName.PadRight(18)[..18];
            var key = repo.KeyName.PadRight(18)[..18];
            var kind = repo.Kind.ToString().PadRight(10)[..10];
            var factory = repo.FactoryName.PadRight(18)[..18];
            Console.WriteLine($"│ {model} │ {key} │ {kind} │ {factory} │");
        }

        Console.WriteLine("└────────────────────┴────────────────────┴────────────┴────────────────────┘");
        Console.WriteLine();
    }

    private static async Task ExecuteGenerationAsync(GenerationContext context)
    {
        // Step 1: Analysis Pipeline
        var analysisPipeline = new AnalysisPipeline();
        var analysisResult = await analysisPipeline.AnalyzeAsync(context);

        // Check for errors
        if (!analysisResult.IsSuccess)
        {
            foreach (var error in analysisResult.Errors)
            {
                Logger.Error(error);
            }
            throw new InvalidOperationException("Analysis failed. See errors above.");
        }

        // Print warnings
        foreach (var warning in analysisResult.Warnings)
        {
            Logger.Warning(warning);
        }

        // Step 2: Create output directories
        var typesDir = Path.Combine(context.DestinationPath, "types");
        var servicesDir = Path.Combine(context.DestinationPath, "services");

        Directory.CreateDirectory(typesDir);
        Directory.CreateDirectory(servicesDir);

        Logger.DirectoryCreated(typesDir);
        Logger.DirectoryCreated(servicesDir);

        // Step 3: Generate TypeScript code (TODO: Phase 3+)
        Logger.Step("Generating TypeScript code...");

        // Print summary of what would be generated
        Console.WriteLine();
        Console.WriteLine("📊 Analysis Summary:");
        Console.WriteLine($"   Models analyzed: {analysisResult.Models.Count}");
        Console.WriteLine($"   Keys analyzed: {analysisResult.Keys.Count}");
        Console.WriteLine($"   Types discovered: {analysisResult.TypeOwnership.Count}");
        Console.WriteLine();

        if (analysisResult.Models.Count > 0)
        {
            Console.WriteLine("📦 Models to generate:");
            foreach (var (name, model) in analysisResult.Models)
            {
                var rawNeeded = model.RequiresRawType ? " (Raw + Clean)" : " (Clean only)";
                Console.WriteLine($"   • {name}: {model.Properties.Count} properties{rawNeeded}");

                // Show properties with custom JSON names
                var customJsonProps = model.Properties.Where(p => p.HasCustomJsonName).ToList();
                if (customJsonProps.Count > 0)
                {
                    foreach (var prop in customJsonProps.Take(5))
                    {
                        Console.WriteLine($"     - {prop.CSharpName} → \"{prop.JsonName}\"");
                    }
                    if (customJsonProps.Count > 5)
                    {
                        Console.WriteLine($"     ... and {customJsonProps.Count - 5} more");
                    }
                }
            }
            Console.WriteLine();
        }

        if (analysisResult.Keys.Count > 0)
        {
            Console.WriteLine("🔑 Keys to generate:");
            foreach (var (name, key) in analysisResult.Keys)
            {
                Console.WriteLine($"   • {name}: {key.Properties.Count} properties");
            }
            Console.WriteLine();
        }

        // Step 4: Generate TypeScript files
        Logger.Step("Writing TypeScript files...");

        var emitterContext = EmitterContext.FromAnalysisResult(
            analysisResult.Models.Values,
            analysisResult.Keys.Values,
            analysisResult.TypeOwnership);

        var tsGenerator = new TypeScriptGenerator(
            context.DestinationPath,
            emitterContext,
            analysisResult.DependencyGraph,
            context.Overwrite);

        // Generate types and services
        tsGenerator.GenerateWithServices(
            analysisResult.Models.Values,
            analysisResult.Keys.Values,
            context.Repositories);

        Logger.Success("TypeScript files generated successfully!");
        Console.WriteLine();
    }

    private static void PrintUsageExample()
    {
        Console.WriteLine("Usage example:");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  rystem-ts generate \\");
        Console.WriteLine("    --dest ./src/api \\");
        Console.WriteLine("    --models \"{Calendar,LeagueKey,Repository,Calendar},{Team,string,Query,Team}\"");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("Format: \"{Model,Key,Type,Factory},{...}\"");
        Console.WriteLine();
        Console.WriteLine("  Model   - C# model class name (or fully qualified: Namespace.Model)");
        Console.WriteLine("  Key     - C# key class name or primitive (string, Guid, int, etc.)");
        Console.WriteLine("  Type    - Repository, Query, or Command");
        Console.WriteLine("  Factory - (optional) TypeScript factory name, defaults to Model");
    }
}
