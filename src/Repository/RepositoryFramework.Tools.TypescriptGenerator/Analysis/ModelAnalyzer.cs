using System.Reflection;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Rules;

namespace RepositoryFramework.Tools.TypescriptGenerator.Analysis;

/// <summary>
/// Analyzes C# types and produces ModelDescriptors.
/// Handles recursive analysis of nested types.
/// </summary>
public class ModelAnalyzer
{
    private readonly TypeResolver _typeResolver;
    private readonly PropertyAnalyzer _propertyAnalyzer;
    private readonly Dictionary<Type, ModelDescriptor> _analyzedModels = [];
    private readonly Dictionary<string, (ModelDescriptor Model, int Depth, string DiscoveredBy)> _discoveredTypes = [];

    public ModelAnalyzer()
    {
        _typeResolver = new TypeResolver();
        _propertyAnalyzer = new PropertyAnalyzer(_typeResolver);
    }

    /// <summary>
    /// Analyzes a type and all its nested types recursively.
    /// </summary>
    /// <param name="type">The type to analyze</param>
    /// <param name="depth">Current depth in the type hierarchy</param>
    /// <param name="discoveredBy">The model that discovered this type</param>
    /// <returns>The ModelDescriptor for the type</returns>
    public ModelDescriptor Analyze(Type type, int depth = 0, string? discoveredBy = null)
    {
        // Check if already analyzed
        if (_analyzedModels.TryGetValue(type, out var existing))
            return existing;

        // Create a placeholder to prevent infinite recursion
        // This allows recursive references (e.g., EntityVersions<Book> where Book has EntityVersions<Book>)
        var placeholder = CreatePlaceholder(type);
        _analyzedModels[type] = placeholder;

        ModelDescriptor descriptor;

        if (type.IsEnum)
        {
            descriptor = AnalyzeEnum(type, depth, discoveredBy);
        }
        else
        {
            descriptor = AnalyzeClass(type, depth, discoveredBy);
        }

        // Update the cache with the complete descriptor
        _analyzedModels[type] = descriptor;
        TrackDiscovery(descriptor, depth, discoveredBy ?? descriptor.Name);

        return descriptor;
    }

    /// <summary>
    /// Creates a placeholder descriptor to prevent infinite recursion.
    /// </summary>
    private ModelDescriptor CreatePlaceholder(Type type)
    {
        return new ModelDescriptor
        {
            Name = type.Name,
            FullName = type.FullName ?? type.Name,
            Namespace = type.Namespace ?? string.Empty,
            Properties = [],
            IsEnum = type.IsEnum,
            NestedTypes = [],
            DiscoveryDepth = 0,
            ClrType = type,
            GenericTypeParameters = []
        };
    }

    /// <summary>
    /// Analyzes an enum type.
    /// </summary>
    private ModelDescriptor AnalyzeEnum(Type type, int depth, string? discoveredBy)
    {
        var values = Enum.GetValues(type);
        var enumValues = new List<EnumValueDescriptor>();

        foreach (var value in values)
        {
            var name = Enum.GetName(type, value)!;
            var numericValue = Convert.ToInt32(value);
            enumValues.Add(new EnumValueDescriptor
            {
                Name = name,
                Value = numericValue
            });
        }

        return new ModelDescriptor
        {
            Name = type.Name,
            FullName = type.FullName ?? type.Name,
            Namespace = type.Namespace ?? string.Empty,
            Properties = [],
            IsEnum = true,
            EnumValues = enumValues,
            DiscoveryDepth = depth,
            DiscoveredByModel = discoveredBy,
            ClrType = type
        };
    }

    /// <summary>
    /// Analyzes a class/record/struct type.
    /// </summary>
    private ModelDescriptor AnalyzeClass(Type type, int depth, string? discoveredBy)
    {
        var properties = _propertyAnalyzer.AnalyzeProperties(type);
        var nestedTypes = new List<ModelDescriptor>();

        // Analyze nested complex types
        foreach (var property in properties)
        {
            var propType = property.Type;

            // Get the actual type to analyze
            Type? typeToAnalyze = null;

            if (propType.IsEnum && propType.ClrType != null)
            {
                typeToAnalyze = propType.ClrType;
            }
            else if (!propType.IsPrimitive && !propType.IsEnum)
            {
                if (propType.IsArray && propType.ElementType?.ClrType != null &&
                    !propType.ElementType.IsPrimitive && !propType.ElementType.IsEnum)
                {
                    typeToAnalyze = propType.ElementType.ClrType;
                }
                else if (propType.IsDictionary && propType.ValueType?.ClrType != null &&
                         !propType.ValueType.IsPrimitive && !propType.ValueType.IsEnum)
                {
                    typeToAnalyze = propType.ValueType.ClrType;
                }
                else if (propType.ClrType != null)
                {
                    typeToAnalyze = propType.ClrType;
                }
            }

            // Also analyze enum element types in arrays
            if (propType.IsArray && propType.ElementType is { IsEnum: true, ClrType: not null })
            {
                typeToAnalyze = propType.ElementType.ClrType;
            }

            if (typeToAnalyze != null && !_analyzedModels.ContainsKey(typeToAnalyze))
            {
                var nestedDescriptor = Analyze(typeToAnalyze, depth + 1, type.Name);
                nestedTypes.Add(nestedDescriptor);
            }
        }

        // Detect generic type parameters
        var genericParameters = new List<string>();
        string? genericBaseName = null;

        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();

            // Check if this is an open generic (contains generic parameters)
            if (type.IsGenericTypeDefinition || genericArgs.Any(arg => arg.IsGenericParameter))
            {
                // Open generic: EntityVersions<T>
                genericParameters = genericArgs.Select(arg => arg.Name).ToList();
            }
            else
            {
                // Closed generic: EntityVersions<Timeline>
                // We need to analyze the open generic base type instead
                var openGeneric = type.GetGenericTypeDefinition();
                if (!_analyzedModels.ContainsKey(openGeneric))
                {
                    var openDescriptor = Analyze(openGeneric, depth, discoveredBy);
                    nestedTypes.Add(openDescriptor);
                }

                // Store reference to open generic
                var baseName = openGeneric.Name;
                var backtickIndex = baseName.IndexOf('`');
                genericBaseName = backtickIndex > 0 ? baseName[..backtickIndex] : baseName;

                // Also analyze the type arguments
                foreach (var typeArg in genericArgs)
                {
                    if (!typeArg.IsGenericParameter && 
                        !PrimitiveTypeRules.IsPrimitive(typeArg) &&
                        !_analyzedModels.ContainsKey(typeArg))
                    {
                        var argDescriptor = Analyze(typeArg, depth + 1, type.Name);
                        nestedTypes.Add(argDescriptor);
                    }
                }
            }
        }

        return new ModelDescriptor
        {
            Name = type.Name,
            FullName = type.FullName ?? type.Name,
            Namespace = type.Namespace ?? string.Empty,
            Properties = properties,
            IsEnum = false,
            NestedTypes = nestedTypes,
            DiscoveryDepth = depth,
            DiscoveredByModel = discoveredBy,
            ClrType = type,
            GenericTypeParameters = genericParameters,
            GenericBaseTypeName = genericBaseName
        };
    }

    /// <summary>
    /// Tracks the discovery of a type for dependency resolution.
    /// </summary>
    private void TrackDiscovery(ModelDescriptor model, int depth, string discoveredBy)
    {
        if (!_discoveredTypes.TryGetValue(model.Name, out var existing))
        {
            _discoveredTypes[model.Name] = (model, depth, discoveredBy);
        }
        else if (depth < existing.Depth)
        {
            // This discovery is at a shallower depth, so it wins
            _discoveredTypes[model.Name] = (model, depth, discoveredBy);
        }
    }

    /// <summary>
    /// Gets all analyzed models.
    /// </summary>
    public IReadOnlyDictionary<Type, ModelDescriptor> GetAnalyzedModels() => _analyzedModels;

    /// <summary>
    /// Gets the ownership information for types (which model "owns" each type).
    /// </summary>
    public IReadOnlyDictionary<string, (ModelDescriptor Model, int Depth, string DiscoveredBy)> GetTypeOwnership()
        => _discoveredTypes;

    /// <summary>
    /// Finds a model by name.
    /// </summary>
    public ModelDescriptor? FindByName(string name)
    {
        return _analyzedModels.Values.FirstOrDefault(m =>
            string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Clears all cached analysis data.
    /// </summary>
    public void Clear()
    {
        _analyzedModels.Clear();
        _discoveredTypes.Clear();
        _typeResolver.ClearCache();
    }
}
