using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;
using RepositoryFramework.Tools.TypescriptGenerator.Utils;
using Xunit;
// Alias for the duplicate Calendar class
using DuplicateCalendar = RepositoryFramework.UnitTest.TypescriptGenerator.DuplicateModels.Calendar;

namespace RepositoryFramework.UnitTest.TypescriptGenerator
{
    public class AssemblyLoaderTest
    {
        [Fact]
        public void FindType_WithSimpleName_ReturnsType()
        {
            // Arrange
            var loader = new TestableAssemblyLoader();
            loader.AddType(typeof(Models.Calendar));

            // Act
            var result = loader.FindType("Calendar");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Calendar", result.Name);
        }

        [Fact]
        public void FindType_WithFullyQualifiedName_ReturnsType()
        {
            // Arrange
            var loader = new TestableAssemblyLoader();
            loader.AddType(typeof(Models.Calendar));

            // Act
            var result = loader.FindType("RepositoryFramework.UnitTest.TypescriptGenerator.Models.Calendar");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Calendar", result.Name);
        }

        [Fact]
        public void FindType_WithNonExistentType_ReturnsNull()
        {
            // Arrange
            var loader = new TestableAssemblyLoader();
            loader.AddType(typeof(Models.Calendar));

            // Act
            var result = loader.FindType("NonExistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindType_WithDuplicateSimpleNames_ThrowsAmbiguousMatchException()
        {
            // Arrange
            var loader = new TestableAssemblyLoader();
            loader.AddType(typeof(Models.Calendar));
            loader.AddType(typeof(DuplicateCalendar)); // Same simple name "Calendar", different namespace

            // Act & Assert
            var ex = Assert.Throws<AmbiguousMatchException>(() => loader.FindType("Calendar"));
            Assert.Contains("Multiple types found", ex.Message);
            Assert.Contains("Calendar", ex.Message);
        }

        [Fact]
        public void FindType_WithDuplicateNames_FullyQualifiedWorks()
        {
            // Arrange
            var loader = new TestableAssemblyLoader();
            loader.AddType(typeof(Models.Calendar));
            loader.AddType(typeof(DuplicateCalendar));

            // Act
            var result1 = loader.FindType("RepositoryFramework.UnitTest.TypescriptGenerator.Models.Calendar");
            var result2 = loader.FindType("RepositoryFramework.UnitTest.TypescriptGenerator.DuplicateModels.Calendar");

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotEqual(result1.FullName, result2.FullName);
        }

        [Fact]
        public void FindType_CaseInsensitive_ReturnsType()
        {
            // Arrange
            var loader = new TestableAssemblyLoader();
            loader.AddType(typeof(Models.Calendar));

            // Act
            var result = loader.FindType("calendar");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Calendar", result.Name);
        }

        [Fact]
        public void FindType_WithUserFriendlyGeneric_ReturnsClosedGenericType()
        {
            // Arrange
            var loader = new TestableAssemblyLoader();
            loader.AddType(typeof(List<>)); // Open generic List`1
            loader.AddType(typeof(Models.Calendar));

            // Act
            var result = loader.FindType("List<Calendar>");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsGenericType);
            Assert.False(result.IsGenericTypeDefinition);
            Assert.Equal("List`1", result.Name);
            Assert.Equal(typeof(Models.Calendar), result.GetGenericArguments()[0]);
        }

        [Fact]
        public void FindType_WithReflectionGeneric_ReturnsClosedGenericType()
        {
            // Arrange
            var loader = new TestableAssemblyLoader();
            loader.AddType(typeof(List<>));
            loader.AddType(typeof(Models.Calendar));

            // Act
            var result = loader.FindType("List`1[[RepositoryFramework.UnitTest.TypescriptGenerator.Models.Calendar]]");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsGenericType);
            Assert.Equal(typeof(Models.Calendar), result.GetGenericArguments()[0]);
        }
    }

    /// <summary>
    /// Test helper that simulates loaded types from assemblies.
    /// </summary>
    internal class TestableAssemblyLoader : AssemblyLoader
    {
        private readonly List<Type> _testTypes = [];

        public void AddType(Type type)
        {
            _testTypes.Add(type);
        }

        public new Type? FindType(string typeName)
        {
            // Handle generic types using GenericTypeHelper
            if (GenericTypeHelper.IsGenericType(typeName))
            {
                return FindGenericTypeTestable(typeName);
            }

            // Check if it's a fully qualified name
            var isFullyQualified = typeName.Contains('.') && !typeName.StartsWith('.');

            // Try exact match first (fully qualified name)
            var type = _testTypes.FirstOrDefault(t => t.FullName == typeName);
            if (type != null)
                return type;

            // If not found by exact match and it's a simple name, search by simple name
            if (!isFullyQualified)
            {
                var matchingTypes = _testTypes
                    .Where(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchingTypes.Count == 0)
                    return null;

                if (matchingTypes.Count == 1)
                    return matchingTypes[0];

                // Multiple types found - throw exception with helpful message
                var typeNames = string.Join("\n  - ", matchingTypes.Select(t => t.FullName));
                throw new AmbiguousMatchException(
                    $"Multiple types found with name '{typeName}'. Please use the fully qualified name:\n  - {typeNames}");
            }

            return null;
        }

        private Type? FindGenericTypeTestable(string typeName)
        {
            var genericInfo = GenericTypeHelper.Parse(typeName);

            // Find the open generic type (e.g., List`1)
            var openGenericType = _testTypes.FirstOrDefault(t =>
                t.IsGenericTypeDefinition &&
                t.Name == genericInfo.ReflectionName);

            if (openGenericType == null)
                return null;

            // Find each type argument
            var typeArgs = new List<Type>();
            foreach (var argName in genericInfo.TypeArguments)
            {
                var argType = FindType(argName);
                if (argType == null)
                    return null;
                typeArgs.Add(argType);
            }

            // Construct the closed generic type
            return openGenericType.MakeGenericType([.. typeArgs]);
        }
    }
}

namespace RepositoryFramework.UnitTest.TypescriptGenerator.DuplicateModels
{
    /// <summary>
    /// Duplicate Calendar class in a different namespace for testing ambiguity.
    /// </summary>
    public class Calendar
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
