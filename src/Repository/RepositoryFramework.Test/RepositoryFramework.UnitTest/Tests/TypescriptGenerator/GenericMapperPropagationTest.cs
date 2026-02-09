using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.Services;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.Transformers;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for generic type parameter mapper callback propagation.
/// When a generic model like EntityVersions&lt;T&gt; is instantiated with a concrete type
/// that requires Raw mapping (e.g., ExtendedBook), the mapper callbacks must be propagated
/// through the entire call chain so that T's properties are correctly mapped.
/// </summary>
public class GenericMapperPropagationTest
{
    // =============================================
    // TypeDescriptor: IsGenericParameter
    // =============================================

    [Fact]
    public void TypeDescriptor_GenericParameter_IsGenericParameter_True()
    {
        var type = TestDescriptorFactory.CreateGenericParameterType("T");
        Assert.True(type.IsGenericParameter);
    }

    [Fact]
    public void TypeDescriptor_StringType_IsGenericParameter_False()
    {
        Assert.False(TestDescriptorFactory.StringType.IsGenericParameter);
    }

    [Fact]
    public void TypeDescriptor_ComplexType_IsGenericParameter_False()
    {
        Assert.False(TestDescriptorFactory.CreateComplexType("Book").IsGenericParameter);
    }

    // =============================================
    // TypeDescriptor: ContainsGenericParameter
    // =============================================

    [Fact]
    public void TypeDescriptor_GenericParameter_ContainsGenericParameter_True()
    {
        var type = TestDescriptorFactory.CreateGenericParameterType("T");
        Assert.True(type.ContainsGenericParameter);
    }

    [Fact]
    public void TypeDescriptor_ArrayOfGenericParameter_ContainsGenericParameter_True()
    {
        var type = TestDescriptorFactory.CreateArrayType(TestDescriptorFactory.CreateGenericParameterType("T"));
        Assert.True(type.ContainsGenericParameter);
    }

    [Fact]
    public void TypeDescriptor_DictionaryWithGenericParameterValue_ContainsGenericParameter_True()
    {
        var type = TestDescriptorFactory.CreateDictionaryType(
            TestDescriptorFactory.StringType,
            TestDescriptorFactory.CreateGenericParameterType("T"));
        Assert.True(type.ContainsGenericParameter);
    }

    [Fact]
    public void TypeDescriptor_StringType_ContainsGenericParameter_False()
    {
        Assert.False(TestDescriptorFactory.StringType.ContainsGenericParameter);
    }

    [Fact]
    public void TypeDescriptor_ArrayOfStrings_ContainsGenericParameter_False()
    {
        var type = TestDescriptorFactory.CreateArrayType(TestDescriptorFactory.StringType);
        Assert.False(type.ContainsGenericParameter);
    }

    // =============================================
    // MapperEmitter: Generic parameter as property
    // =============================================

    [Fact]
    public void MapperEmitter_GenericParameterProperty_RawToClean_UsesCallback()
    {
        var model = CreateGenericModelWithTProperty();
        var context = CreateContext(model);

        var result = MapperEmitter.EmitRawToClean(model, context);

        Assert.Contains("mapTFromRaw: (raw: any) => any = (x: any) => x", result);
        Assert.Contains("entity: mapTFromRaw(raw.e)", result);
    }

    [Fact]
    public void MapperEmitter_GenericParameterProperty_CleanToRaw_UsesCallback()
    {
        var model = CreateGenericModelWithTProperty();
        var context = CreateContext(model);

        var result = MapperEmitter.EmitCleanToRaw(model, context);

        Assert.Contains("mapTToRaw: (clean: any) => any = (x: any) => x", result);
        Assert.Contains("e: mapTToRaw(clean.entity)", result);
    }

    [Fact]
    public void MapperEmitter_OptionalGenericParameterProperty_RawToClean_UsesCallback()
    {
        var model = CreateGenericModelWithOptionalTProperty();
        var context = CreateContext(model);

        var result = MapperEmitter.EmitRawToClean(model, context);

        Assert.Contains("raw.e != null ? mapTFromRaw(raw.e) : undefined", result);
    }

    [Fact]
    public void MapperEmitter_OptionalGenericParameterProperty_CleanToRaw_UsesCallback()
    {
        var model = CreateGenericModelWithOptionalTProperty();
        var context = CreateContext(model);

        var result = MapperEmitter.EmitCleanToRaw(model, context);

        Assert.Contains("clean.entity != null ? mapTToRaw(clean.entity) : undefined", result);
    }

    // =============================================
    // MapperEmitter: Array of generic parameter
    // =============================================

    [Fact]
    public void MapperEmitter_ArrayOfGenericParameter_RawToClean_UsesCallback()
    {
        var model = CreateGenericModelWithTArrayProperty();
        var context = CreateContext(model);

        var result = MapperEmitter.EmitRawToClean(model, context);

        Assert.Contains("raw.items?.map(mapTFromRaw) ?? []", result);
    }

    [Fact]
    public void MapperEmitter_ArrayOfGenericParameter_CleanToRaw_UsesCallback()
    {
        var model = CreateGenericModelWithTArrayProperty();
        var context = CreateContext(model);

        var result = MapperEmitter.EmitCleanToRaw(model, context);

        Assert.Contains("clean.items?.map(mapTToRaw) ?? []", result);
    }

    // =============================================
    // MapperEmitter: Nested generic model propagation
    // =============================================

    [Fact]
    public void MapperEmitter_NestedGenericArray_RawToClean_PropagatesCallback()
    {
        // EntityVersions<T> has List<EntityVersion<T>>
        // EntityVersion is a generic model that requires raw
        var entityVersion = CreateEntityVersionModel();
        var entityVersions = CreateEntityVersionsModel(entityVersion);
        var context = CreateContext(entityVersions, entityVersion);

        var result = MapperEmitter.EmitRawToClean(entityVersions, context);

        // Should propagate mapTFromRaw to nested mapper call
        Assert.Contains("raw.ents?.map(item => mapRawEntityVersionToEntityVersion(item, mapTFromRaw)) ?? []", result);
    }

    [Fact]
    public void MapperEmitter_NestedGenericArray_CleanToRaw_PropagatesCallback()
    {
        var entityVersion = CreateEntityVersionModel();
        var entityVersions = CreateEntityVersionsModel(entityVersion);
        var context = CreateContext(entityVersions, entityVersion);

        var result = MapperEmitter.EmitCleanToRaw(entityVersions, context);

        Assert.Contains("clean.entities?.map(item => mapEntityVersionToRawEntityVersion(item, mapTToRaw)) ?? []", result);
    }

    // =============================================
    // MapperEmitter: Non-generic models unchanged
    // =============================================

    [Fact]
    public void MapperEmitter_NonGenericModel_RawToClean_NoCallbacks()
    {
        var model = TestDescriptorFactory.CreateModel(
            "ExtendedBook",
            TestDescriptorFactory.CreateProperty("Id", "id", TestDescriptorFactory.StringType),
            TestDescriptorFactory.CreateProperty("ImageAsBase64", "ib64", TestDescriptorFactory.StringType));
        var context = CreateContext(model);

        var result = MapperEmitter.EmitRawToClean(model, context);

        Assert.DoesNotContain("mapTFromRaw", result);
        Assert.Contains("(raw: ExtendedBookRaw): ExtendedBook", result);
    }

    // =============================================
    // MapperEmitter: Dictionary with generic param value
    // =============================================

    [Fact]
    public void MapperEmitter_DictOfGenericParameter_RawToClean_UsesCallback()
    {
        var tType = TestDescriptorFactory.CreateGenericParameterType("T");
        var dictType = TestDescriptorFactory.CreateDictionaryType(TestDescriptorFactory.StringType, tType);
        var model = new ModelDescriptor
        {
            Name = "Container`1",
            FullName = "Test.Container`1",
            Namespace = "Test",
            Properties = [new PropertyDescriptor
            {
                CSharpName = "Items",
                JsonName = "items",
                TypeScriptName = "items",
                Type = dictType,
                IsRequired = true,
                IsOptional = false
            }],
            IsEnum = false,
            GenericTypeParameters = ["T"]
        };
        var context = CreateContext(model);

        var result = MapperEmitter.EmitRawToClean(model, context);

        Assert.Contains("mapTFromRaw(v)", result);
    }

    // =============================================
    // TransformerEmitter: Generic with callbacks
    // =============================================

    [Fact]
    public void TransformerEmitter_GenericWithRawType_EmitsCallbackParams()
    {
        var model = CreateEntityVersionModel();

        var result = TransformerEmitter.Emit(model);

        Assert.Contains("mapTFromRaw: (raw: any) => any = (x: any) => x", result);
        Assert.Contains("mapTToRaw: (clean: any) => any = (x: any) => x", result);
        Assert.Contains("mapRawEntityVersionToEntityVersion<T>(plain, mapTFromRaw)", result);
        Assert.Contains("mapEntityVersionToRawEntityVersion<T>(instance, mapTToRaw)", result);
    }

    [Fact]
    public void TransformerEmitter_NonGenericWithRawType_NoCallbacks()
    {
        var model = TestDescriptorFactory.CreateModel(
            "ExtendedBook",
            TestDescriptorFactory.CreateProperty("Id", "id", TestDescriptorFactory.StringType));

        var result = TransformerEmitter.Emit(model);

        Assert.DoesNotContain("mapTFromRaw", result);
        Assert.Contains("ExtendedBookTransformer", result);
    }

    // =============================================
    // BootstrapEmitter: Concrete mapper args
    // =============================================

    [Fact]
    public void BootstrapEmitter_GenericWithMappedTypeArg_PassesMappers()
    {
        var extendedBook = TestDescriptorFactory.CreateModel(
            "ExtendedBook",
            TestDescriptorFactory.CreateProperty("Id", "id", TestDescriptorFactory.StringType),
            TestDescriptorFactory.CreateProperty("ImageAsBase64", "ib64", TestDescriptorFactory.StringType));

        var entityVersions = new ModelDescriptor
        {
            Name = "EntityVersions`1",
            TypeScriptName = "EntityVersions<T>",
            FullName = "Test.EntityVersions`1",
            Namespace = "Test",
            Properties = [TestDescriptorFactory.CreateProperty("Id", "id", TestDescriptorFactory.StringType)],
            IsEnum = false,
            GenericTypeParameters = ["T"]
        };

        var closedGeneric = new ModelDescriptor
        {
            Name = "EntityVersions`1",
            TypeScriptName = "EntityVersions<ExtendedBook>",
            FullName = "Test.EntityVersions`1[[Test.ExtendedBook]]",
            Namespace = "Test",
            Properties = entityVersions.Properties,
            IsEnum = false,
            GenericBaseTypeName = "EntityVersions"
        };

        var models = new Dictionary<string, ModelDescriptor>
        {
            ["EntityVersions<ExtendedBook>"] = closedGeneric,
            ["EntityVersions<T>"] = entityVersions,
            ["ExtendedBook"] = extendedBook
        };
        var keys = new Dictionary<string, ModelDescriptor>();

        var repos = new[]
        {
            new RepositoryDescriptor
            {
                ModelName = "EntityVersions<ExtendedBook>",
                KeyName = "Guid",
                Kind = RepositoryKind.Repository,
                FactoryName = "versionedextendedbooks"
            }
        };

        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Should pass concrete mapper functions
        Assert.Contains("mapRawExtendedBookToExtendedBook", result);
        Assert.Contains("mapExtendedBookToRawExtendedBook", result);
    }

    [Fact]
    public void BootstrapEmitter_GenericWithUnmappedTypeArg_NoMapperArgs()
    {
        var book = TestDescriptorFactory.CreateModel(
            "Book",
            TestDescriptorFactory.CreateProperty("Id", TestDescriptorFactory.StringType),
            TestDescriptorFactory.CreateProperty("Name", TestDescriptorFactory.StringType));

        var entityVersions = new ModelDescriptor
        {
            Name = "EntityVersions`1",
            TypeScriptName = "EntityVersions<T>",
            FullName = "Test.EntityVersions`1",
            Namespace = "Test",
            Properties = [TestDescriptorFactory.CreateProperty("Id", "id", TestDescriptorFactory.StringType)],
            IsEnum = false,
            GenericTypeParameters = ["T"]
        };

        var closedGeneric = new ModelDescriptor
        {
            Name = "EntityVersions`1",
            TypeScriptName = "EntityVersions<Book>",
            FullName = "Test.EntityVersions`1[[Test.Book]]",
            Namespace = "Test",
            Properties = entityVersions.Properties,
            IsEnum = false,
            GenericBaseTypeName = "EntityVersions"
        };

        var models = new Dictionary<string, ModelDescriptor>
        {
            ["EntityVersions<Book>"] = closedGeneric,
            ["EntityVersions<T>"] = entityVersions,
            ["Book"] = book
        };
        var keys = new Dictionary<string, ModelDescriptor>();

        var repos = new[]
        {
            new RepositoryDescriptor
            {
                ModelName = "EntityVersions<Book>",
                KeyName = "Guid",
                Kind = RepositoryKind.Repository,
                FactoryName = "versionedbooks"
            }
        };

        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Book doesn't require raw, so no mapper args
        Assert.Contains("createEntityVersionsTransformer<Book>()", result);
        Assert.DoesNotContain("mapRawBookToBook", result);
    }

    [Fact]
    public void BootstrapEmitter_GenericWithMappedTypeArg_ImportsMappers()
    {
        var extendedBook = TestDescriptorFactory.CreateModel(
            "ExtendedBook",
            TestDescriptorFactory.CreateProperty("Id", "id", TestDescriptorFactory.StringType));

        var entityVersions = new ModelDescriptor
        {
            Name = "EntityVersions`1",
            TypeScriptName = "EntityVersions<T>",
            FullName = "Test.EntityVersions`1",
            Namespace = "Test",
            Properties = [TestDescriptorFactory.CreateProperty("Id", "id", TestDescriptorFactory.StringType)],
            IsEnum = false,
            GenericTypeParameters = ["T"]
        };

        var closedGeneric = new ModelDescriptor
        {
            Name = "EntityVersions`1",
            TypeScriptName = "EntityVersions<ExtendedBook>",
            FullName = "Test.EntityVersions`1[[Test.ExtendedBook]]",
            Namespace = "Test",
            Properties = entityVersions.Properties,
            IsEnum = false,
            GenericBaseTypeName = "EntityVersions"
        };

        var models = new Dictionary<string, ModelDescriptor>
        {
            ["EntityVersions<ExtendedBook>"] = closedGeneric,
            ["EntityVersions<T>"] = entityVersions,
            ["ExtendedBook"] = extendedBook
        };
        var keys = new Dictionary<string, ModelDescriptor>();

        var repos = new[]
        {
            new RepositoryDescriptor
            {
                ModelName = "EntityVersions<ExtendedBook>",
                KeyName = "Guid",
                Kind = RepositoryKind.Repository,
                FactoryName = "versionedextendedbooks"
            }
        };

        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Should import mapper functions from extendedbook types file
        Assert.Contains("mapRawExtendedBookToExtendedBook", result);
        Assert.Contains("mapExtendedBookToRawExtendedBook", result);
        Assert.Contains("from '../types/extendedbook'", result);
    }

    // =============================================
    // TypeResolver: IsGenericParameter on CLR types
    // =============================================

    [Fact]
    public void TypeResolver_GenericParameter_SetsIsGenericParameter()
    {
        var resolver = new TypeResolver();
        // Use a real generic type definition to get a generic parameter
        var genericArgs = typeof(List<>).GetGenericArguments();
        var tParam = genericArgs[0]; // T

        var descriptor = resolver.Resolve(tParam);

        Assert.True(descriptor.IsGenericParameter);
        Assert.Equal("T", descriptor.CSharpName);
    }

    // =============================================
    // ModelAnalyzer integration: EntityVersion<T>
    // =============================================

    [Fact]
    public void ModelAnalyzer_EntityVersionOpenGeneric_PropertiesWithGenericParam()
    {
        var analyzer = new ModelAnalyzer();
        var descriptor = analyzer.Analyze(typeof(TestEntityVersion<>));

        // Should have T as a generic parameter
        Assert.Contains("T", descriptor.GenericTypeParameters);

        // Entity property should use the generic parameter T
        var entityProp = descriptor.Properties.FirstOrDefault(p => p.CSharpName == "Entity");
        Assert.NotNull(entityProp);
        Assert.True(entityProp.Type.IsGenericParameter);
        Assert.Equal("T", entityProp.Type.CSharpName);
    }

    [Fact]
    public void ModelAnalyzer_ContainerWithTArray_ElementTypeIsGenericParam()
    {
        var analyzer = new ModelAnalyzer();
        var descriptor = analyzer.Analyze(typeof(TestContainer<>));

        var itemsProp = descriptor.Properties.FirstOrDefault(p => p.CSharpName == "Items");
        Assert.NotNull(itemsProp);
        Assert.True(itemsProp.Type.IsArray);
        Assert.True(itemsProp.Type.ElementType!.IsGenericParameter);
        Assert.Equal("T", itemsProp.Type.ElementType.CSharpName);
    }

    // =============================================
    // Full EmitRawToClean for EntityVersion<T>-like model
    // =============================================

    [Fact]
    public void MapperEmitter_EntityVersionLike_RawToClean_FullOutput()
    {
        var model = CreateEntityVersionModel();
        var context = CreateContext(model);

        var result = MapperEmitter.EmitRawToClean(model, context);

        // Should have callback parameter
        Assert.Contains("mapTFromRaw: (raw: any) => any = (x: any) => x", result);
        // Date fields still use parseDateTime
        Assert.Contains("parseDateTime(raw.ct)", result);
        Assert.Contains("parseDateTime(raw.la)", result);
        // Entity field uses callback
        Assert.Contains("entity: raw.e != null ? mapTFromRaw(raw.e) : undefined", result);
    }

    [Fact]
    public void MapperEmitter_EntityVersionLike_CleanToRaw_FullOutput()
    {
        var model = CreateEntityVersionModel();
        var context = CreateContext(model);

        var result = MapperEmitter.EmitCleanToRaw(model, context);

        Assert.Contains("mapTToRaw: (clean: any) => any = (x: any) => x", result);
        Assert.Contains("formatDateTime(clean.creationTime)", result);
        Assert.Contains("e: clean.entity != null ? mapTToRaw(clean.entity) : undefined", result);
    }

    // =============================================
    // Multiple generic parameters
    // =============================================

    [Fact]
    public void MapperEmitter_TwoGenericParams_EmitsBothCallbacks()
    {
        var tType = TestDescriptorFactory.CreateGenericParameterType("T");
        var uType = TestDescriptorFactory.CreateGenericParameterType("U");

        var model = new ModelDescriptor
        {
            Name = "Pair`2",
            FullName = "Test.Pair`2",
            Namespace = "Test",
            Properties =
            [
                new PropertyDescriptor
                {
                    CSharpName = "First", JsonName = "f", TypeScriptName = "first",
                    Type = tType, IsRequired = true, IsOptional = false
                },
                new PropertyDescriptor
                {
                    CSharpName = "Second", JsonName = "s", TypeScriptName = "second",
                    Type = uType, IsRequired = true, IsOptional = false
                }
            ],
            IsEnum = false,
            GenericTypeParameters = ["T", "U"]
        };
        var context = CreateContext(model);

        var rawToClean = MapperEmitter.EmitRawToClean(model, context);

        Assert.Contains("mapTFromRaw: (raw: any) => any = (x: any) => x", rawToClean);
        Assert.Contains("mapUFromRaw: (raw: any) => any = (x: any) => x", rawToClean);
        Assert.Contains("first: mapTFromRaw(raw.f)", rawToClean);
        Assert.Contains("second: mapUFromRaw(raw.s)", rawToClean);

        var cleanToRaw = MapperEmitter.EmitCleanToRaw(model, context);

        Assert.Contains("mapTToRaw: (clean: any) => any = (x: any) => x", cleanToRaw);
        Assert.Contains("mapUToRaw: (clean: any) => any = (x: any) => x", cleanToRaw);
        Assert.Contains("f: mapTToRaw(clean.first)", cleanToRaw);
        Assert.Contains("s: mapUToRaw(clean.second)", cleanToRaw);
    }

    // =============================================
    // Helpers
    // =============================================

    /// <summary>
    /// Creates an EntityVersion-like model: generic with dates + T property.
    /// </summary>
    private static ModelDescriptor CreateEntityVersionModel()
    {
        return new ModelDescriptor
        {
            Name = "EntityVersion`1",
            FullName = "Test.EntityVersion`1",
            Namespace = "Test",
            Properties =
            [
                new PropertyDescriptor
                {
                    CSharpName = "CreationTime", JsonName = "ct", TypeScriptName = "creationTime",
                    Type = TestDescriptorFactory.DateTimeType, IsRequired = true, IsOptional = false
                },
                new PropertyDescriptor
                {
                    CSharpName = "LastUpdate", JsonName = "la", TypeScriptName = "lastUpdate",
                    Type = TestDescriptorFactory.DateTimeType, IsRequired = true, IsOptional = false
                },
                new PropertyDescriptor
                {
                    CSharpName = "Entity", JsonName = "e", TypeScriptName = "entity",
                    Type = TestDescriptorFactory.CreateGenericParameterType("T"), IsRequired = false, IsOptional = true
                }
            ],
            IsEnum = false,
            GenericTypeParameters = ["T"]
        };
    }

    /// <summary>
    /// Creates an EntityVersions-like model: generic with List&lt;EntityVersion&lt;T&gt;&gt;.
    /// </summary>
    private static ModelDescriptor CreateEntityVersionsModel(ModelDescriptor entityVersion)
    {
        // The element type of the array is EntityVersion<T> (a constructed generic with unresolved T)
        var elementType = new TypeDescriptor
        {
            CSharpName = "EntityVersion`1",
            FullName = "Test.EntityVersion`1",
            TypeScriptName = "EntityVersion<T>",
            IsPrimitive = false,
            IsNullable = true,
            IsArray = false,
            IsDictionary = false,
            IsEnum = false
        };

        var arrayType = new TypeDescriptor
        {
            CSharpName = "List`1",
            FullName = "System.Collections.Generic.List`1",
            TypeScriptName = "EntityVersion<T>[]",
            IsPrimitive = false,
            IsNullable = true,
            IsArray = true,
            IsDictionary = false,
            IsEnum = false,
            ElementType = elementType
        };

        return new ModelDescriptor
        {
            Name = "EntityVersions`1",
            FullName = "Test.EntityVersions`1",
            Namespace = "Test",
            Properties =
            [
                new PropertyDescriptor
                {
                    CSharpName = "Id", JsonName = "id", TypeScriptName = "id",
                    Type = TestDescriptorFactory.StringType, IsRequired = true, IsOptional = false
                },
                new PropertyDescriptor
                {
                    CSharpName = "BookId", JsonName = "bid", TypeScriptName = "bookId",
                    Type = TestDescriptorFactory.StringType, IsRequired = true, IsOptional = false
                },
                new PropertyDescriptor
                {
                    CSharpName = "Entities", JsonName = "ents", TypeScriptName = "entities",
                    Type = arrayType, IsRequired = false, IsOptional = true
                }
            ],
            IsEnum = false,
            GenericTypeParameters = ["T"],
            NestedTypes = [entityVersion]
        };
    }

    private static ModelDescriptor CreateGenericModelWithTProperty()
    {
        return new ModelDescriptor
        {
            Name = "Wrapper`1",
            FullName = "Test.Wrapper`1",
            Namespace = "Test",
            Properties =
            [
                new PropertyDescriptor
                {
                    CSharpName = "Id", JsonName = "id", TypeScriptName = "id",
                    Type = TestDescriptorFactory.StringType, IsRequired = true, IsOptional = false
                },
                new PropertyDescriptor
                {
                    CSharpName = "Entity", JsonName = "e", TypeScriptName = "entity",
                    Type = TestDescriptorFactory.CreateGenericParameterType("T"), IsRequired = true, IsOptional = false
                }
            ],
            IsEnum = false,
            GenericTypeParameters = ["T"]
        };
    }

    private static ModelDescriptor CreateGenericModelWithOptionalTProperty()
    {
        return new ModelDescriptor
        {
            Name = "Wrapper`1",
            FullName = "Test.Wrapper`1",
            Namespace = "Test",
            Properties =
            [
                new PropertyDescriptor
                {
                    CSharpName = "Id", JsonName = "id", TypeScriptName = "id",
                    Type = TestDescriptorFactory.StringType, IsRequired = true, IsOptional = false
                },
                new PropertyDescriptor
                {
                    CSharpName = "Entity", JsonName = "e", TypeScriptName = "entity",
                    Type = TestDescriptorFactory.CreateGenericParameterType("T"), IsRequired = false, IsOptional = true
                }
            ],
            IsEnum = false,
            GenericTypeParameters = ["T"]
        };
    }

    private static ModelDescriptor CreateGenericModelWithTArrayProperty()
    {
        return new ModelDescriptor
        {
            Name = "Container`1",
            FullName = "Test.Container`1",
            Namespace = "Test",
            Properties =
            [
                new PropertyDescriptor
                {
                    CSharpName = "Id", JsonName = "id", TypeScriptName = "id",
                    Type = TestDescriptorFactory.StringType, IsRequired = true, IsOptional = false
                },
                new PropertyDescriptor
                {
                    CSharpName = "Items", JsonName = "items", TypeScriptName = "items",
                    Type = TestDescriptorFactory.CreateArrayType(TestDescriptorFactory.CreateGenericParameterType("T")),
                    IsRequired = true, IsOptional = false
                }
            ],
            IsEnum = false,
            GenericTypeParameters = ["T"]
        };
    }

    private static EmitterContext CreateContext(params ModelDescriptor[] models)
    {
        var context = new EmitterContext();
        foreach (var model in models)
        {
            var baseName = model.Name.Contains('`')
                ? model.Name[..model.Name.IndexOf('`')]
                : model.Name;
            context.AllModels[model.Name] = model;
            if (model.RequiresRawType)
                context.TypesRequiringRaw.Add(baseName);
        }
        return context;
    }
}

// Test models for ModelAnalyzer integration tests
public sealed class TestEntityVersion<T>
{
    [JsonPropertyName("ct")]
    public required DateTime CreationTime { get; set; }

    [JsonPropertyName("la")]
    public required DateTime LastUpdate { get; set; }

    [JsonPropertyName("e")]
    public T? Entity { get; set; }
}

public sealed class TestContainer<T>
{
    [JsonPropertyName("items")]
    public required List<T> Items { get; set; }
}
