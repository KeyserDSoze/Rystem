using System.Text;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;

namespace RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;

/// <summary>
/// Emits TypeScript enum declarations from C# enums.
/// </summary>
public static class EnumEmitter
{
    /// <summary>
    /// Generates a TypeScript enum from a ModelDescriptor.
    /// </summary>
    public static string Emit(ModelDescriptor enumDescriptor)
    {
        if (!enumDescriptor.IsEnum || enumDescriptor.EnumValues == null)
            throw new ArgumentException("ModelDescriptor must be an enum type.", nameof(enumDescriptor));

        var sb = new StringBuilder();

        sb.AppendLine($"export enum {enumDescriptor.Name} {{");

        for (var i = 0; i < enumDescriptor.EnumValues.Count; i++)
        {
            var value = enumDescriptor.EnumValues[i];
            var comma = i < enumDescriptor.EnumValues.Count - 1 ? "," : "";
            sb.AppendLine($"  {value.Name} = {value.Value}{comma}");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates multiple enums from a list of ModelDescriptors.
    /// </summary>
    public static string EmitAll(IEnumerable<ModelDescriptor> enumDescriptors)
    {
        var sb = new StringBuilder();

        foreach (var descriptor in enumDescriptors.Where(d => d.IsEnum))
        {
            sb.AppendLine(Emit(descriptor));
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
