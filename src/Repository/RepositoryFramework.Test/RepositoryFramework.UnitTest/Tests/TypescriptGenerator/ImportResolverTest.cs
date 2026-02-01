using System.Collections.Generic;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using Xunit;
using static RepositoryFramework.UnitTest.TypescriptGenerator.TestDescriptorFactory;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for ImportResolver - resolving TypeScript imports.
/// </summary>
public class ImportResolverTest
{
    private static EmitterContext CreateContext()
    {
        var context = new EmitterContext();
        context.AllModels["User"] = CreateModel("User", CreateProperty("Id", NumberType));
        context.AllModels["Address"] = CreateModel("Address", CreateProperty("Street", StringType));
        context.AllModels["Order"] = CreateModel("Order", CreateProperty("Id", NumberType));
        context.TypeOwnership["Address"] = "User";
        context.TypeOwnership["OrderItem"] = "Order";
        context.TypesRequiringRaw.Add("User");
        context.TypesRequiringRaw.Add("Address");
        return context;
    }

    [Fact]
    public void ResolveImports_ModelWithNoExternalDependencies_ReturnsEmpty()
    {
        // Arrange
        var model = CreateModel("Simple",
            CreateProperty("Name", StringType),
            CreateProperty("Age", NumberType));
        var context = new EmitterContext();
        context.AllModels["Simple"] = model;

        // Act
        var result = ImportResolver.ResolveImports("Simple", model, context, null);

        // Assert
        Assert.True(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void ResolveImports_WithArrayOfPrimitives_NoImports()
    {
        // Arrange
        var model = CreateModel("Tags",
            CreateProperty("Names", "names", CreateArrayType(StringType)));
        var context = new EmitterContext();
        context.AllModels["Tags"] = model;

        // Act
        var result = ImportResolver.ResolveImports("Tags", model, context, null);

        // Assert
        Assert.True(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void ResolveImports_WithEnumProperty_ImportsEnumFromOwnerFile()
    {
        // Arrange
        var context = CreateContext();
        var statusEnum = CreateEnum("Status", ("Active", 0));
        context.EnumTypes.Add("Status");
        context.TypeOwnership["Status"] = "Order";
        context.AllModels["Status"] = statusEnum;

        var model = CreateModel("Task",
            CreateProperty("Status", "status", CreateEnumType("Status")));
        context.AllModels["Task"] = model;

        // Act
        var result = ImportResolver.ResolveImports("Task", model, context, null);

        // Assert
        Assert.Contains("import", result);
        Assert.Contains("Status", result);
    }

    [Fact]
    public void ResolveImports_SelfReference_NoImport()
    {
        // Arrange
        var context = new EmitterContext();
        var model = CreateModel("Node",
            CreateProperty("Children", "children", CreateArrayType(CreateComplexType("Node"))));
        context.AllModels["Node"] = model;
        context.TypeOwnership["Node"] = "Node";

        // Act
        var result = ImportResolver.ResolveImports("Node", model, context, null);

        // Assert
        Assert.True(string.IsNullOrEmpty(result) || !result.Contains("from './node'"));
    }

    [Fact]
    public void ResolveImports_NestedTypeOwnedBySameModel_NoImport()
    {
        // Arrange
        var context = new EmitterContext();
        var nested = CreateModel("OrderItem", CreateProperty("Id", NumberType));
        var model = new ModelDescriptor
        {
            Name = "Order",
            FullName = "Test.Order",
            Namespace = "Test",
            IsEnum = false,
            Properties = [CreateProperty("Items", "items", CreateArrayType(CreateComplexType("OrderItem")))],
            NestedTypes = [nested]
        };
        context.AllModels["Order"] = model;
        context.AllModels["OrderItem"] = nested;
        context.TypeOwnership["OrderItem"] = "Order";

        // Act
        var result = ImportResolver.ResolveImports("Order", model, context, null);

        // Assert
        Assert.True(string.IsNullOrEmpty(result) || !result.Contains("OrderItem"));
    }

    [Fact]
    public void ResolveImports_PrimitiveTypes_NeverImported()
    {
        // Arrange
        var context = new EmitterContext();
        var model = CreateModel("AllPrimitives",
            CreateProperty("Text", StringType),
            CreateProperty("Count", NumberType),
            CreateProperty("Flag", BooleanType));
        context.AllModels["AllPrimitives"] = model;

        // Act
        var result = ImportResolver.ResolveImports("AllPrimitives", model, context, null);

        // Assert
        Assert.True(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void ResolveImports_WithDictionaryOfComplexValues_ImportsValueType()
    {
        // Arrange
        var context = CreateContext();
        var productModel = CreateModel("Product", CreateProperty("Name", StringType));
        context.TypeOwnership["Product"] = "Product";
        context.AllModels["Product"] = productModel;

        var model = CreateModel("Catalog",
            CreateProperty("Products", "products", CreateDictionaryType(StringType, CreateComplexType("Product"))));
        context.AllModels["Catalog"] = model;

        // Act
        var result = ImportResolver.ResolveImports("Catalog", model, context, null);

        // Assert
        Assert.Contains("Product", result);
    }
}
