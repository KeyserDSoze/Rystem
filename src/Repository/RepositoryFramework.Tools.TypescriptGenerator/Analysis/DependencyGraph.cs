using RepositoryFramework.Tools.TypescriptGenerator.Domain;

namespace RepositoryFramework.Tools.TypescriptGenerator.Analysis;

/// <summary>
/// Manages the dependency graph between models.
/// Resolves which model "owns" shared types based on discovery depth.
/// </summary>
public class DependencyGraph
{
    private readonly Dictionary<string, TypeOwnership> _typeOwnership = [];
    private readonly Dictionary<string, HashSet<string>> _dependencies = [];
    private readonly Dictionary<string, ModelDescriptor> _models = [];

    /// <summary>
    /// Adds a model to the dependency graph.
    /// </summary>
    public void AddModel(ModelDescriptor model)
    {
        _models[model.Name] = model;

        if (!_dependencies.ContainsKey(model.Name))
            _dependencies[model.Name] = [];

        // Track this model as owner of itself (depth 0)
        TrackOwnership(model.Name, model.Name, 0);

        // Process nested types
        ProcessNestedTypes(model, model.Name, 1);
    }

    private void ProcessNestedTypes(ModelDescriptor model, string rootModelName, int depth)
    {
        foreach (var property in model.Properties)
        {
            var typeToTrack = GetTypeToTrack(property.Type);
            if (typeToTrack != null)
            {
                TrackOwnership(typeToTrack, rootModelName, depth);
                _dependencies[rootModelName].Add(typeToTrack);

                // If we have the nested model, process its nested types too
                if (property.NestedModel != null)
                {
                    ProcessNestedTypes(property.NestedModel, rootModelName, depth + 1);
                }
            }
        }

        // Process declared nested types
        foreach (var nested in model.NestedTypes)
        {
            TrackOwnership(nested.Name, rootModelName, depth);
            _dependencies[rootModelName].Add(nested.Name);
            ProcessNestedTypes(nested, rootModelName, depth + 1);
        }
    }

    private static string? GetTypeToTrack(TypeDescriptor type)
    {
        if (type.IsPrimitive)
            return null;

        if (type.IsEnum)
            return type.CSharpName;

        if (type.IsUnion)
        {
            // Union types don't get tracked themselves;
            // their members are tracked via nested type discovery
            return null;
        }

        if (type.IsArray && type.ElementType != null)
            return GetTypeToTrack(type.ElementType);

        if (type.IsDictionary && type.ValueType != null)
            return GetTypeToTrack(type.ValueType);

        return type.CSharpName;
    }

    private void TrackOwnership(string typeName, string ownerModel, int depth)
    {
        if (!_typeOwnership.TryGetValue(typeName, out var existing))
        {
            _typeOwnership[typeName] = new TypeOwnership
            {
                TypeName = typeName,
                OwnerModel = ownerModel,
                Depth = depth
            };
        }
        else if (depth < existing.Depth)
        {
            // Shallower depth wins
            _typeOwnership[typeName] = new TypeOwnership
            {
                TypeName = typeName,
                OwnerModel = ownerModel,
                Depth = depth
            };
        }
        // At equal depth, first one wins (no change)
    }

    /// <summary>
    /// Gets the owner model for a type name.
    /// </summary>
    public string? GetOwner(string typeName)
    {
        return _typeOwnership.TryGetValue(typeName, out var ownership)
            ? ownership.OwnerModel
            : null;
    }

    /// <summary>
    /// Gets all dependencies for a model (types it needs to import).
    /// </summary>
    public IReadOnlySet<string> GetDependencies(string modelName)
    {
        return _dependencies.TryGetValue(modelName, out var deps)
            ? deps
            : new HashSet<string>();
    }

    /// <summary>
    /// Gets all types that a model owns (should be defined in its file).
    /// </summary>
    public IEnumerable<string> GetOwnedTypes(string modelName)
    {
        return _typeOwnership
            .Where(kvp => kvp.Value.OwnerModel == modelName)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Gets the types that need to be imported by a model from other model files.
    /// </summary>
    public IEnumerable<(string TypeName, string FromModel)> GetImports(string modelName)
    {
        var dependencies = GetDependencies(modelName);
        var ownedTypes = GetOwnedTypes(modelName).ToHashSet();

        foreach (var dep in dependencies)
        {
            if (!ownedTypes.Contains(dep))
            {
                var owner = GetOwner(dep);
                if (owner != null && owner != modelName)
                {
                    yield return (dep, owner);
                }
            }
        }
    }

    /// <summary>
    /// Returns models in topological order (dependencies first).
    /// </summary>
    public IEnumerable<string> GetTopologicalOrder()
    {
        var visited = new HashSet<string>();
        var result = new List<string>();

        void Visit(string modelName)
        {
            if (visited.Contains(modelName))
                return;

            visited.Add(modelName);

            if (_dependencies.TryGetValue(modelName, out var deps))
            {
                foreach (var dep in deps)
                {
                    var owner = GetOwner(dep);
                    if (owner != null && owner != modelName)
                    {
                        Visit(owner);
                    }
                }
            }

            result.Add(modelName);
        }

        foreach (var model in _models.Keys)
        {
            Visit(model);
        }

        return result;
    }

    /// <summary>
    /// Gets all ownership information.
    /// </summary>
    public IReadOnlyDictionary<string, TypeOwnership> GetAllOwnership() => _typeOwnership;

    /// <summary>
    /// Gets all models.
    /// </summary>
    public IReadOnlyDictionary<string, ModelDescriptor> GetAllModels() => _models;

    /// <summary>
    /// Clears the dependency graph.
    /// </summary>
    public void Clear()
    {
        _typeOwnership.Clear();
        _dependencies.Clear();
        _models.Clear();
    }
}

/// <summary>
/// Represents ownership information for a type.
/// </summary>
public sealed record TypeOwnership
{
    public required string TypeName { get; init; }
    public required string OwnerModel { get; init; }
    public required int Depth { get; init; }
}
