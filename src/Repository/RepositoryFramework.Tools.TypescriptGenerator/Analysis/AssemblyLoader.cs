using System.Reflection;
using System.Runtime.Loader;
using RepositoryFramework.Tools.TypescriptGenerator.Utils;

namespace RepositoryFramework.Tools.TypescriptGenerator.Analysis;

/// <summary>
/// Loads assemblies and resolves types from C# projects.
/// </summary>
public class AssemblyLoader
{
    private readonly List<Assembly> _loadedAssemblies = [];
    private AssemblyLoadContext? _loadContext;

    /// <summary>
    /// Loads an assembly from a DLL file path.
    /// </summary>
    public Assembly LoadFromPath(string assemblyPath)
    {
        var fullPath = Path.GetFullPath(assemblyPath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Assembly not found: {fullPath}");

        // Create a custom load context to avoid polluting the default context
        _loadContext ??= new AssemblyLoadContext("RystemTsGenerator", isCollectible: true);

        // Load the assembly and its dependencies
        var assembly = _loadContext.LoadFromAssemblyPath(fullPath);
        _loadedAssemblies.Add(assembly);

        Logger.Info($"Loaded assembly: {assembly.GetName().Name}");

        return assembly;
    }

    /// <summary>
    /// Builds a project and loads the output assembly.
    /// </summary>
    public async Task<Assembly?> BuildAndLoadProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(projectPath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Project file not found: {fullPath}");

        Logger.Step($"Building project: {Path.GetFileName(fullPath)}");

        // Build the project
        var projectDir = Path.GetDirectoryName(fullPath)!;
        var buildResult = await BuildProjectAsync(fullPath, cancellationToken);

        if (!buildResult.Success)
        {
            Logger.Error($"Build failed: {buildResult.ErrorMessage}");
            return null;
        }

        // Find the output assembly
        var assemblyPath = FindOutputAssembly(fullPath, projectDir);

        if (assemblyPath == null)
        {
            Logger.Error("Could not find output assembly after build");
            return null;
        }

        return LoadFromPath(assemblyPath);
    }

    /// <summary>
    /// Finds a type by name in the loaded assemblies.
    /// Supports both simple names (e.g., "Calendar") and fully qualified names (e.g., "Fantacalcio.Domain.Calendar").
    /// If using a simple name and multiple types with the same name exist, throws an exception.
    /// </summary>
    public Type? FindType(string typeName)
    {
        // Check if it's a fully qualified name (contains a dot that's not at the start)
        var isFullyQualified = typeName.Contains('.') && !typeName.StartsWith('.');

        foreach (var assembly in _loadedAssemblies)
        {
            // Try exact match first (fully qualified name)
            var type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }

        // If not found by exact match and it's a simple name, search by simple name
        if (!isFullyQualified)
        {
            var matchingTypes = new List<Type>();

            foreach (var assembly in _loadedAssemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
                }

                matchingTypes.AddRange(types.Where(t =>
                    t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase)));
            }

            if (matchingTypes.Count == 0)
                return null;

            if (matchingTypes.Count == 1)
                return matchingTypes[0];

            // Multiple types found - throw exception with helpful message
            var typeNames = string.Join("\n  - ", matchingTypes.Select(t => t.FullName));
            throw new AmbiguousMatchException(
                $"Multiple types found with name '{typeName}'. Please use the fully qualified name:\n  - {typeNames}");
        }

        return null;
    }

    /// <summary>
    /// Finds all types matching a predicate.
    /// </summary>
    public IEnumerable<Type> FindTypes(Func<Type, bool> predicate)
    {
        foreach (var assembly in _loadedAssemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Some types might not load, continue with the ones that did
                types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
            }

            foreach (var type in types.Where(predicate))
            {
                yield return type;
            }
        }
    }

    /// <summary>
    /// Gets all loaded assemblies.
    /// </summary>
    public IReadOnlyList<Assembly> GetLoadedAssemblies() => _loadedAssemblies;

    /// <summary>
    /// Unloads all assemblies (if possible).
    /// </summary>
    public void Unload()
    {
        _loadedAssemblies.Clear();

        if (_loadContext != null)
        {
            _loadContext.Unload();
            _loadContext = null;
        }
    }

    private static async Task<(bool Success, string? ErrorMessage)> BuildProjectAsync(
        string projectPath,
        CancellationToken cancellationToken)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectPath}\" -c Release --no-restore -v q",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new System.Diagnostics.Process { StartInfo = startInfo };

        try
        {
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                return (false, string.IsNullOrEmpty(error) ? output : error);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string? FindOutputAssembly(string projectPath, string projectDir)
    {
        // Try to determine the assembly name from the project file
        var projectContent = File.ReadAllText(projectPath);
        var assemblyName = ExtractAssemblyName(projectContent) ??
                           Path.GetFileNameWithoutExtension(projectPath);

        // Common output paths
        var possiblePaths = new[]
        {
            Path.Combine(projectDir, "bin", "Release", "net10.0", $"{assemblyName}.dll"),
            Path.Combine(projectDir, "bin", "Release", "net9.0", $"{assemblyName}.dll"),
            Path.Combine(projectDir, "bin", "Release", "net8.0", $"{assemblyName}.dll"),
            Path.Combine(projectDir, "bin", "Debug", "net10.0", $"{assemblyName}.dll"),
            Path.Combine(projectDir, "bin", "Debug", "net9.0", $"{assemblyName}.dll"),
            Path.Combine(projectDir, "bin", "Debug", "net8.0", $"{assemblyName}.dll"),
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }

        // Try to find any DLL with the assembly name in the bin folder
        var binDir = Path.Combine(projectDir, "bin");
        if (Directory.Exists(binDir))
        {
            var dllFiles = Directory.GetFiles(binDir, $"{assemblyName}.dll", SearchOption.AllDirectories);
            if (dllFiles.Length > 0)
            {
                // Prefer Release over Debug
                var releaseDll = dllFiles.FirstOrDefault(f => f.Contains("Release"));
                return releaseDll ?? dllFiles[0];
            }
        }

        return null;
    }

    private static string? ExtractAssemblyName(string projectContent)
    {
        // Simple extraction - look for <AssemblyName> element
        var match = System.Text.RegularExpressions.Regex.Match(
            projectContent,
            @"<AssemblyName>([^<]+)</AssemblyName>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value : null;
    }
}
