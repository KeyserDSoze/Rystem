namespace RepositoryFramework.Tools.TypescriptGenerator.Utils;

/// <summary>
/// Simple console logger with colors and icons.
/// </summary>
public static class Logger
{
    public static void Info(string message)
    {
        Console.WriteLine($"  {message}");
    }

    public static void Success(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {message}");
        Console.ResetColor();
    }

    public static void Warning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ {message}");
        Console.ResetColor();
    }

    public static void Error(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"✗ {message}");
        Console.ResetColor();
    }

    public static void Step(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"→ {message}");
        Console.ResetColor();
    }

    public static void Debug(string message)
    {
#if DEBUG
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  [DEBUG] {message}");
        Console.ResetColor();
#endif
    }

    public static void FileCreated(string path)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  📄 Created: {path}");
        Console.ResetColor();
    }

    public static void FileOverwritten(string path)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  📄 Overwritten: {path}");
        Console.ResetColor();
    }

    public static void DirectoryCreated(string path)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"  📂 Created: {path}");
        Console.ResetColor();
    }
}
