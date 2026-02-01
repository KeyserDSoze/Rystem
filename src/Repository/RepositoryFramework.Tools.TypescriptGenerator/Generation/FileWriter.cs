using RepositoryFramework.Tools.TypescriptGenerator.Utils;

namespace RepositoryFramework.Tools.TypescriptGenerator.Generation;

/// <summary>
/// Writes generated TypeScript files to disk.
/// </summary>
public class FileWriter
{
    private readonly string _basePath;
    private readonly bool _overwrite;

    public FileWriter(string basePath, bool overwrite = true)
    {
        _basePath = basePath;
        _overwrite = overwrite;
    }

    /// <summary>
    /// Writes a TypeScript file to the types folder.
    /// </summary>
    public void WriteTypeFile(string fileName, string content)
    {
        var typesDir = Path.Combine(_basePath, "types");
        Directory.CreateDirectory(typesDir);

        var filePath = Path.Combine(typesDir, fileName);
        WriteFile(filePath, content);
    }

    /// <summary>
    /// Writes a TypeScript file to the services folder.
    /// </summary>
    public void WriteServiceFile(string fileName, string content)
    {
        var servicesDir = Path.Combine(_basePath, "services");
        Directory.CreateDirectory(servicesDir);

        var filePath = Path.Combine(servicesDir, fileName);
        WriteFile(filePath, content);
    }

    /// <summary>
    /// Writes a TypeScript file to the bootstrap folder.
    /// </summary>
    public void WriteBootstrapFile(string fileName, string content)
    {
        var bootstrapDir = Path.Combine(_basePath, "bootstrap");
        Directory.CreateDirectory(bootstrapDir);

        var filePath = Path.Combine(bootstrapDir, fileName);
        WriteFile(filePath, content);
    }

    /// <summary>
    /// Writes an index.ts file to a folder.
    /// </summary>
    public void WriteIndexFile(string folder, IEnumerable<string> exports)
    {
        var dir = Path.Combine(_basePath, folder);
        Directory.CreateDirectory(dir);

        var content = string.Join(Environment.NewLine,
            exports.Select(e => $"export * from './{Path.GetFileNameWithoutExtension(e)}';"));

        var filePath = Path.Combine(dir, "index.ts");
        WriteFile(filePath, content);
    }

    /// <summary>
    /// Writes content to a file.
    /// </summary>
    private void WriteFile(string filePath, string content)
    {
        var exists = File.Exists(filePath);

        if (exists && !_overwrite)
        {
            Logger.Warning($"Skipped (exists): {filePath}");
            return;
        }

        File.WriteAllText(filePath, content);

        if (exists)
        {
            Logger.FileOverwritten(GetRelativePath(filePath));
        }
        else
        {
            Logger.FileCreated(GetRelativePath(filePath));
        }
    }

    /// <summary>
    /// Gets a path relative to the base path for logging.
    /// </summary>
    private string GetRelativePath(string fullPath)
    {
        if (fullPath.StartsWith(_basePath))
        {
            return fullPath[_basePath.Length..].TrimStart(Path.DirectorySeparatorChar);
        }
        return fullPath;
    }

    /// <summary>
    /// Ensures the output directories exist.
    /// </summary>
    public void EnsureDirectories()
    {
        var typesDir = Path.Combine(_basePath, "types");
        var servicesDir = Path.Combine(_basePath, "services");
        var bootstrapDir = Path.Combine(_basePath, "bootstrap");

        if (!Directory.Exists(typesDir))
        {
            Directory.CreateDirectory(typesDir);
            Logger.DirectoryCreated(typesDir);
        }

        if (!Directory.Exists(servicesDir))
        {
            Directory.CreateDirectory(servicesDir);
            Logger.DirectoryCreated(servicesDir);
        }

        if (!Directory.Exists(bootstrapDir))
        {
            Directory.CreateDirectory(bootstrapDir);
            Logger.DirectoryCreated(bootstrapDir);
        }
    }
}
