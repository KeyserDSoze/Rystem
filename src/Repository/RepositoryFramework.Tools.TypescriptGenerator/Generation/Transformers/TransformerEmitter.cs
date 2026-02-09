using System.Text;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;

namespace RepositoryFramework.Tools.TypescriptGenerator.Generation.Transformers;

/// <summary>
/// Emits ITransformer implementations for models and keys.
/// These transformers are used by RepositorySettings to convert between Raw and Clean types.
/// </summary>
public static class TransformerEmitter
{
    /// <summary>
    /// Emits a transformer file for a model or key type.
    /// </summary>
    public static string Emit(ModelDescriptor model, bool isKey = false)
    {
        if (model.IsEnum)
            return string.Empty;

        var sb = new StringBuilder();
        var baseName = model.GetBaseTypeName();
        var isGeneric = model.GenericTypeParameters.Count > 0;
        var genericParams = isGeneric 
            ? $"<{string.Join(", ", model.GenericTypeParameters)}>" 
            : "";
        var typeName = $"{baseName}{genericParams}";
        var fileNameWithoutExt = model.GetFileName().Replace(".ts", "");

        // Header
        sb.AppendLine("// ============================================");
        sb.AppendLine($"// {baseName} Transformer");
        sb.AppendLine("// ============================================");
        sb.AppendLine();

        if (!model.RequiresRawType)
        {
            // Simple pass-through transformer — no Raw type or mappers needed
            sb.AppendLine("import type { ITransformer } from 'rystem.repository.client';");
            sb.AppendLine($"import type {{ {baseName} }} from '../types/{fileNameWithoutExt}';");
            sb.AppendLine();

            if (isGeneric)
            {
                sb.AppendLine("/**");
                sb.AppendLine($" * Creates a simple transformer for {baseName}<T>.");
                sb.AppendLine(" * The backend API returns data in the correct format, no mapping needed.");
                sb.AppendLine(" */");
                sb.AppendLine($"export function create{baseName}Transformer{genericParams}(): ITransformer<{typeName}> {{");
                sb.AppendLine("  return {");
                sb.AppendLine($"    fromPlain: (plain: any): {typeName} => plain as {typeName},");
                sb.AppendLine($"    toPlain: (instance: {typeName}): any => instance,");
                sb.AppendLine("  };");
                sb.AppendLine("}");
            }
            else
            {
                sb.AppendLine("/**");
                sb.AppendLine($" * Simple transformer for {baseName}.");
                sb.AppendLine(" * The backend API returns data in the correct format, no mapping needed.");
                sb.AppendLine(" */");
                sb.AppendLine($"export const {baseName}Transformer: ITransformer<{baseName}> = {{");
                sb.AppendLine($"  fromPlain: (plain: any): {baseName} => plain as {baseName},");
                sb.AppendLine($"  toPlain: (instance: {baseName}): any => instance,");
                sb.AppendLine("};");
            }
        }
        else
        {
            var typeNameRaw = $"{baseName}Raw{genericParams}";

            // Full transformer with Raw type and mapper imports
            sb.AppendLine("import type { ITransformer } from 'rystem.repository.client';");
            sb.AppendLine($"import type {{ {baseName}, {baseName}Raw }} from '../types/{fileNameWithoutExt}';");
            sb.AppendLine($"import {{ mapRaw{baseName}To{baseName}, map{baseName}ToRaw{baseName} }} from '../types/{fileNameWithoutExt}';");
            sb.AppendLine();

            if (isGeneric)
            {
                // Build callback parameters for each generic type parameter
                // Use 'any' types for callbacks to avoid TS2345:
                // the actual mappers take TRaw→T and T→TRaw, not T→T
                var callbackParams = new List<string>();
                foreach (var param in model.GenericTypeParameters)
                {
                    callbackParams.Add($"map{param}FromRaw: (raw: any) => any = (x: any) => x");
                    callbackParams.Add($"map{param}ToRaw: (clean: any) => any = (x: any) => x");
                }
                var paramsStr = string.Join(",\n  ", callbackParams);
                var fromRawCallbacks = string.Join(", ", model.GenericTypeParameters.Select(p => $"map{p}FromRaw"));
                var toRawCallbacks = string.Join(", ", model.GenericTypeParameters.Select(p => $"map{p}ToRaw"));

                sb.AppendLine("/**");
                sb.AppendLine($" * Creates a transformer for {baseName}<T> type.");
                sb.AppendLine(" * Converts between Raw (JSON) and Clean (TypeScript) representations.");
                sb.AppendLine(" * Pass mapper functions for generic type parameter(s) to enable deep mapping.");
                sb.AppendLine(" */");
                sb.AppendLine($"export function create{baseName}Transformer{genericParams}(");
                sb.AppendLine($"  {paramsStr}");
                sb.AppendLine($"): ITransformer<{typeName}> {{");
                sb.AppendLine("  return {");
                sb.AppendLine($"    fromPlain: (plain: {typeNameRaw}): {typeName} => mapRaw{baseName}To{baseName}{genericParams}(plain, {fromRawCallbacks}),");
                sb.AppendLine($"    toPlain: (instance: {typeName}): {typeNameRaw} => map{baseName}ToRaw{baseName}{genericParams}(instance, {toRawCallbacks}),");
                sb.AppendLine("  };");
                sb.AppendLine("}");
            }
            else
            {
                sb.AppendLine("/**");
                sb.AppendLine($" * Transformer for {baseName} type.");
                sb.AppendLine(" * Converts between Raw (JSON) and Clean (TypeScript) representations.");
                sb.AppendLine(" */");
                sb.AppendLine($"export const {baseName}Transformer: ITransformer<{baseName}> = {{");
                sb.AppendLine($"  fromPlain: (plain: {baseName}Raw): {baseName} => mapRaw{baseName}To{baseName}(plain),");
                sb.AppendLine($"  toPlain: (instance: {baseName}): {baseName}Raw => map{baseName}ToRaw{baseName}(instance),");
                sb.AppendLine("};");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets the file name for a transformer.
    /// </summary>
    public static string GetFileName(ModelDescriptor model)
    {
        return $"{model.GetBaseTypeName()}Transformer.ts";
    }
}
