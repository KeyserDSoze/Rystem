using System.Text;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using RepositoryFramework.Tools.TypescriptGenerator.Utils;

namespace RepositoryFramework.Tools.TypescriptGenerator.Generation.Services;

/// <summary>
/// Emits TypeScript service classes for Repository/Query/Command patterns.
/// </summary>
public static class ServiceEmitter
{
    /// <summary>
    /// Generates a complete TypeScript service for a repository.
    /// </summary>
    public static string Emit(
        RepositoryDescriptor repository,
        ModelDescriptor model,
        ModelDescriptor? key,
        EmitterContext context)
    {
        var sb = new StringBuilder();
        var modelName = model.Name;
        var keyTypeName = GetKeyTypeName(repository, key);
        var serviceName = $"{repository.FactoryName}Service";
        var requiresRaw = model.RequiresRawType;
        var keyRequiresRaw = key?.RequiresRawType ?? false;

        // Generate service class
        sb.AppendLine($"export class {serviceName} {{");
        sb.AppendLine("  private baseUrl: string;");
        sb.AppendLine("  private headers: () => Promise<HeadersInit>;");
        sb.AppendLine();

        // Constructor
        sb.AppendLine($"  constructor(baseUrl: string, headers?: () => Promise<HeadersInit>) {{");
        sb.AppendLine("    this.baseUrl = baseUrl;");
        sb.AppendLine("    this.headers = headers ?? (async () => ({}));");
        sb.AppendLine("  }");
        sb.AppendLine();

        // Helper methods
        EmitHelperMethods(sb, requiresRaw, keyRequiresRaw, modelName, keyTypeName, key != null);

        // Query methods (for Repository and Query patterns)
        if (repository.Kind != RepositoryKind.Command)
        {
            EmitQueryMethods(sb, modelName, keyTypeName, requiresRaw, keyRequiresRaw, key);
        }

        // Command methods (for Repository and Command patterns)
        if (repository.Kind != RepositoryKind.Query)
        {
            EmitCommandMethods(sb, modelName, keyTypeName, requiresRaw, keyRequiresRaw, key);
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void EmitHelperMethods(
        StringBuilder sb,
        bool requiresRaw,
        bool keyRequiresRaw,
        string modelName,
        string keyTypeName,
        bool hasComplexKey)
    {
        // Common request method
        sb.AppendLine("  private async request<TResponse>(");
        sb.AppendLine("    path: string,");
        sb.AppendLine("    method: string,");
        sb.AppendLine("    body?: unknown");
        sb.AppendLine("  ): Promise<TResponse> {");
        sb.AppendLine("    const headers = await this.headers();");
        sb.AppendLine("    const response = await fetch(`${this.baseUrl}/${path}`, {");
        sb.AppendLine("      method,");
        sb.AppendLine("      headers: {");
        sb.AppendLine("        'Content-Type': 'application/json',");
        sb.AppendLine("        ...headers,");
        sb.AppendLine("      },");
        sb.AppendLine("      body: body ? JSON.stringify(body) : undefined,");
        sb.AppendLine("    });");
        sb.AppendLine();
        sb.AppendLine("    if (!response.ok) {");
        sb.AppendLine("      const errorBody = await response.text();");
        sb.AppendLine("      throw new Error(`Request failed: ${response.status} ${errorBody}`);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    return response.json();");
        sb.AppendLine("  }");
        sb.AppendLine();

        // Key serialization helper
        if (hasComplexKey)
        {
            sb.AppendLine($"  private serializeKey(key: {keyTypeName}): string {{");
            if (keyRequiresRaw)
            {
                sb.AppendLine($"    const raw = map{keyTypeName}ToRaw{keyTypeName}(key);");
                sb.AppendLine("    return encodeURIComponent(JSON.stringify(raw));");
            }
            else
            {
                sb.AppendLine("    return encodeURIComponent(JSON.stringify(key));");
            }
            sb.AppendLine("  }");
        }
        else
        {
            sb.AppendLine($"  private serializeKey(key: {keyTypeName}): string {{");
            sb.AppendLine("    return encodeURIComponent(String(key));");
            sb.AppendLine("  }");
        }
        sb.AppendLine();
    }

    private static void EmitQueryMethods(
        StringBuilder sb,
        string modelName,
        string keyTypeName,
        bool requiresRaw,
        bool keyRequiresRaw,
        ModelDescriptor? key)
    {
        var rawModelName = $"{modelName}Raw";

        // GET method
        sb.AppendLine($"  async get(key: {keyTypeName}): Promise<{modelName} | null> {{");
        sb.AppendLine("    try {");
        if (requiresRaw)
        {
            sb.AppendLine($"      const raw = await this.request<{rawModelName}>(`Get/${{this.serializeKey(key)}}`, 'GET');");
            sb.AppendLine($"      return mapRaw{modelName}To{modelName}(raw);");
        }
        else
        {
            sb.AppendLine($"      return await this.request<{modelName}>(`Get/${{this.serializeKey(key)}}`, 'GET');");
        }
        sb.AppendLine("    } catch {");
        sb.AppendLine("      return null;");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine();

        // EXIST method
        sb.AppendLine($"  async exist(key: {keyTypeName}): Promise<boolean> {{");
        sb.AppendLine("    try {");
        sb.AppendLine("      const result = await this.request<{ i: boolean }>(`Exist/${this.serializeKey(key)}`, 'GET');");
        sb.AppendLine("      return result.i;");
        sb.AppendLine("    } catch {");
        sb.AppendLine("      return false;");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine();

        // QUERY method
        EmitQueryMethod(sb, modelName, keyTypeName, requiresRaw, keyRequiresRaw);

        // COUNT method
        sb.AppendLine("  async count(filter?: string): Promise<number> {");
        sb.AppendLine("    const body = filter ? { f: filter } : {};");
        sb.AppendLine("    const result = await this.request<{ c: number }>('Operation/Count', 'POST', body);");
        sb.AppendLine("    return result.c;");
        sb.AppendLine("  }");
        sb.AppendLine();
    }

    private static void EmitQueryMethod(
        StringBuilder sb,
        string modelName,
        string keyTypeName,
        bool requiresRaw,
        bool keyRequiresRaw)
    {
        var rawModelName = $"{modelName}Raw";
        var entityType = $"Entity<{modelName}, {keyTypeName}>";
        var rawEntityType = requiresRaw ? $"{{ k: unknown; v: {rawModelName} }}" : $"{{ k: unknown; v: {modelName} }}";

        sb.AppendLine($"  async query(options?: {{");
        sb.AppendLine("    filter?: string;");
        sb.AppendLine("    order?: string;");
        sb.AppendLine("    top?: number;");
        sb.AppendLine("    skip?: number;");
        sb.AppendLine($"  }}): Promise<{entityType}[]> {{");
        sb.AppendLine("    const body: Record<string, unknown> = {};");
        sb.AppendLine("    if (options?.filter) body.f = options.filter;");
        sb.AppendLine("    if (options?.order) body.o = options.order;");
        sb.AppendLine("    if (options?.top) body.t = options.top;");
        sb.AppendLine("    if (options?.skip) body.s = options.skip;");
        sb.AppendLine();
        sb.AppendLine($"    const raw = await this.request<{rawEntityType}[]>('Query', 'POST', body);");
        sb.AppendLine();

        if (requiresRaw)
        {
            sb.AppendLine("    return raw.map(item => ({");
            sb.AppendLine("      key: item.k as unknown as " + keyTypeName + ",");
            sb.AppendLine($"      value: mapRaw{modelName}To{modelName}(item.v),");
            sb.AppendLine("    }));");
        }
        else
        {
            sb.AppendLine("    return raw.map(item => ({");
            sb.AppendLine("      key: item.k as unknown as " + keyTypeName + ",");
            sb.AppendLine("      value: item.v,");
            sb.AppendLine("    }));");
        }
        sb.AppendLine("  }");
        sb.AppendLine();
    }

    private static void EmitCommandMethods(
        StringBuilder sb,
        string modelName,
        string keyTypeName,
        bool requiresRaw,
        bool keyRequiresRaw,
        ModelDescriptor? key)
    {
        var rawModelName = $"{modelName}Raw";
        var stateType = $"State<{modelName}, {keyTypeName}>";

        // INSERT method
        sb.AppendLine($"  async insert(key: {keyTypeName}, value: {modelName}): Promise<{stateType}> {{");
        if (requiresRaw)
        {
            sb.AppendLine($"    const rawValue = map{modelName}ToRaw{modelName}(value);");
            sb.AppendLine("    const body = { k: key, v: rawValue };");
        }
        else
        {
            sb.AppendLine("    const body = { k: key, v: value };");
        }
        sb.AppendLine("    const result = await this.request<RawState>('Insert', 'POST', body);");
        sb.AppendLine("    return this.mapState(result);");
        sb.AppendLine("  }");
        sb.AppendLine();

        // UPDATE method
        sb.AppendLine($"  async update(key: {keyTypeName}, value: {modelName}): Promise<{stateType}> {{");
        if (requiresRaw)
        {
            sb.AppendLine($"    const rawValue = map{modelName}ToRaw{modelName}(value);");
            sb.AppendLine("    const body = { k: key, v: rawValue };");
        }
        else
        {
            sb.AppendLine("    const body = { k: key, v: value };");
        }
        sb.AppendLine("    const result = await this.request<RawState>('Update', 'POST', body);");
        sb.AppendLine("    return this.mapState(result);");
        sb.AppendLine("  }");
        sb.AppendLine();

        // DELETE method
        sb.AppendLine($"  async delete(key: {keyTypeName}): Promise<{stateType}> {{");
        sb.AppendLine("    const result = await this.request<RawState>(`Delete/${this.serializeKey(key)}`, 'POST');");
        sb.AppendLine("    return this.mapState(result);");
        sb.AppendLine("  }");
        sb.AppendLine();

        // State mapper helper
        sb.AppendLine($"  private mapState(raw: RawState): {stateType} {{");
        sb.AppendLine("    return {");
        sb.AppendLine("      isOk: raw.i,");
        sb.AppendLine("      code: raw.c,");
        sb.AppendLine("      message: raw.m,");
        sb.AppendLine("    };");
        sb.AppendLine("  }");
        sb.AppendLine();

        // BATCH method
        EmitBatchMethod(sb, modelName, keyTypeName, requiresRaw);
    }

    private static void EmitBatchMethod(
        StringBuilder sb,
        string modelName,
        string keyTypeName,
        bool requiresRaw)
    {
        var stateType = $"State<{modelName}, {keyTypeName}>[]";

        sb.AppendLine($"  async batch(operations: BatchOperation<{modelName}, {keyTypeName}>[]): Promise<{stateType}> {{");
        sb.AppendLine("    const body = {");
        sb.AppendLine("      v: operations.map(op => ({");
        sb.AppendLine("        c: op.command,");
        sb.AppendLine("        k: op.key,");
        if (requiresRaw)
        {
            sb.AppendLine($"        v: op.value ? map{modelName}ToRaw{modelName}(op.value) : undefined,");
        }
        else
        {
            sb.AppendLine("        v: op.value,");
        }
        sb.AppendLine("      })),");
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    const result = await this.request<RawState[]>('Batch', 'POST', body);");
        sb.AppendLine("    return result.map(r => this.mapState(r));");
        sb.AppendLine("  }");
        sb.AppendLine();
    }

    private static string GetKeyTypeName(RepositoryDescriptor repository, ModelDescriptor? key)
    {
        if (key != null)
            return key.Name;

        // Map primitive key types
        return repository.KeyName.ToLowerInvariant() switch
        {
            "string" => "string",
            "int" or "long" or "short" or "byte" or "float" or "double" or "decimal" => "number",
            "bool" or "boolean" => "boolean",
            "guid" => "string",
            "datetime" or "datetimeoffset" => "string",
            _ => repository.KeyName
        };
    }
}
