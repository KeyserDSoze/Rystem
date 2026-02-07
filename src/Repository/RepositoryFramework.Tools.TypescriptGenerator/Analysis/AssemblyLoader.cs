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
    public async Task<Assembly?> BuildAndLoadProjectAsync(
        string projectPath, 
        bool includeDependencies = false,
        string? dependencyPrefix = null,
        CancellationToken cancellationToken = default)
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

        var mainAssembly = LoadFromPath(assemblyPath);

        // Load dependencies if requested
        if (includeDependencies)
        {
            LoadDependencies(assemblyPath, dependencyPrefix);
        }

        return mainAssembly;
    }

    /// <summary>
    /// Loads all dependency assemblies from the same directory as the main assembly.
    /// </summary>
    /// <param name="mainAssemblyPath">Path to the main assembly</param>
    /// <param name="prefix">Optional prefix filter - only load DLLs starting with this</param>
    private void LoadDependencies(string mainAssemblyPath, string? prefix)
    {
        var directory = Path.GetDirectoryName(mainAssemblyPath)!;
        var mainAssemblyName = Path.GetFileName(mainAssemblyPath);
        var loadedCount = 0;

        Logger.Step($"Loading dependencies from: {directory}");

        foreach (var dllPath in Directory.GetFiles(directory, "*.dll"))
        {
            var dllName = Path.GetFileNameWithoutExtension(dllPath);

            // Skip the main assembly (already loaded)
            if (dllPath.Equals(mainAssemblyPath, StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip system assemblies
            if (IsSystemAssembly(dllName))
                continue;

            // Apply prefix filter if specified
            if (!string.IsNullOrWhiteSpace(prefix) && 
                !dllName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            // Check if already loaded
            if (_loadedAssemblies.Any(a => a.GetName().Name?.Equals(dllName, StringComparison.OrdinalIgnoreCase) == true))
                continue;

            try
            {
                LoadFromPath(dllPath);
                loadedCount++;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Could not load dependency '{dllName}': {ex.Message}");
            }
        }

        Logger.Info($"  Loaded {loadedCount} dependency assemblies");
    }

    /// <summary>
    /// Checks if an assembly name is a system/framework assembly that should be skipped.
    /// </summary>
    private static bool IsSystemAssembly(string assemblyName)
    {
        var systemPrefixes = new[]
        {
            "System.",
            "Microsoft.",
            "mscorlib",
            "netstandard",
            "WindowsBase",
            "PresentationCore",
            "PresentationFramework",
            "Newtonsoft.Json",
            "NuGet.",
            "Humanizer",
            "Azure.",
            "Google.",
            "Amazon."
        };

        return systemPrefixes.Any(prefix => 
            assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            assemblyName.Equals(prefix.TrimEnd('.'), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a type by name in the loaded assemblies.
    /// Supports both simple names (e.g., "Calendar") and fully qualified names (e.g., "Fantacalcio.Domain.Calendar").
    /// Also supports generic types in both user-friendly (EntityVersions&lt;Timeline&gt;) and reflection syntax (EntityVersions`1[[Timeline]]).
    /// If using a simple name and multiple types with the same name exist, throws an exception.
    /// </summary>
    public Type? FindType(string typeName)
    {
        // Handle generic types
        if (GenericTypeHelper.IsGenericType(typeName))
        {
            return FindGenericType(typeName);
        }

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
    /// Finds a generic type by parsing the generic syntax and constructing the closed generic type.
    /// Supports: EntityVersions&lt;Timeline&gt; or EntityVersions`1[[Timeline]]
    /// </summary>
    private Type? FindGenericType(string typeName)
    {
        var genericInfo = GenericTypeHelper.Parse(typeName);

        // If Parse determined this is NOT a generic (e.g., open generic like EntityVersions`1 without args),
        // search for it directly without trying to construct a closed generic
        if (!genericInfo.IsGeneric)
        {
            Logger.Info($"  Searching for open generic or base type: {genericInfo.BaseTypeName}");

            // Search directly in assemblies without recursion
            foreach (var assembly in _loadedAssemblies)
            {
                var type = assembly.GetType(genericInfo.BaseTypeName);
                if (type != null)
                    return type;

                // Also try simple name matching
                try
                {
                    var types = assembly.GetTypes();
                    var match = types.FirstOrDefault(t => t.Name.Equals(genericInfo.BaseTypeName, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        return match;
                }
                catch (ReflectionTypeLoadException ex)
                {
                    var types = ex.Types.Where(t => t != null).Cast<Type>();
                    var match = types.FirstOrDefault(t => t.Name.Equals(genericInfo.BaseTypeName, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        return match;
                }
            }

            Logger.Warning($"Could not find type: {genericInfo.BaseTypeName}");
            return null;
        }

        // Find the open generic type (e.g., EntityVersions`1)
        var openGenericType = FindType(genericInfo.ReflectionName);
        if (openGenericType == null)
        {
            Logger.Warning($"Could not find open generic type: {genericInfo.ReflectionName}");
            return null;
        }

        // Find each type argument
        var typeArgs = new List<Type>();
        foreach (var argName in genericInfo.TypeArguments)
        {
            var argType = FindType(argName);
            if (argType == null)
            {
                Logger.Warning($"Could not find generic type argument: {argName}");
                return null;
            }
            typeArgs.Add(argType);
        }

        // Construct the closed generic type
        try
        {
            var closedGenericType = openGenericType.MakeGenericType([.. typeArgs]);
            Logger.Info($"  Constructed generic type: {genericInfo.DisplayName}");
            return closedGenericType;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to construct generic type {genericInfo.DisplayName}: {ex.Message}");
            return null;
        }
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
