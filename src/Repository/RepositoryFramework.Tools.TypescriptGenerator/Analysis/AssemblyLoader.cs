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
    /// </summary>
    public Type? FindType(string typeName)
    {
        foreach (var assembly in _loadedAssemblies)
        {
            // Try exact match first
            var type = assembly.GetType(typeName);
            if (type != null)
                return type;

            // Try to find by simple name
            type = assembly.GetTypes()
                .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

            if (type != null)
                return type;
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
