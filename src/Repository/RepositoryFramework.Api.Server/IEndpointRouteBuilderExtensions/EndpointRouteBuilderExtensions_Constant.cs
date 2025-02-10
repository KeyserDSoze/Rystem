using System.Linq.Dynamic.Core;
using System.Population.Random;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RepositoryFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class EndpointRouteBuilderExtensions
    {
        private static readonly Dictionary<string, bool> s_setupRepositories = new();

        private const string NotImplementedExceptionIlOperation = "newobj instance void System.NotImplementedException";
        private sealed class RepositoryMethodValue
        {
            public string Name { get; init; } = null!;
            public RepositoryMethods Method { get; init; }
            public string DefaultHttpMethod { get; init; } = null!;
        }
        private static readonly Dictionary<PatternType, List<RepositoryMethodValue>> s_possibleMethods = new()
        {
            {
                PatternType.Repository,
                new() {
                    new()
                    {
                        Name = nameof(AddBootstrap),
                        DefaultHttpMethod = "Get",
                        Method = RepositoryMethods.Bootstrap
                    },
                    new()
                    {
                       Name = nameof(AddGet),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Get
                    },
                    new()
                    {
                       Name = nameof(AddQuery),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Query
                    },
                    new()
                    {
                       Name = nameof(AddExist),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Exist
                    },
                    new()
                    {
                       Name = nameof(AddOperation),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Operation
                    },
                    new()
                    {
                       Name = nameof(AddInsert),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Insert
                    },
                    new()
                    {
                       Name = nameof(AddUpdate),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Update
                    },
                    new()
                    {
                       Name = nameof(AddDelete),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Delete
                    },
                    new()
                    {
                       Name = nameof(AddBatch),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Batch
                    },
                }
            },
            {
                PatternType.Query,
                new() {
                    new()
                    {
                        Name = nameof(AddBootstrap),
                        DefaultHttpMethod = "Get",
                        Method = RepositoryMethods.Bootstrap
                    },
                    new()
                    {
                       Name = nameof(AddGet),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Get
                    },
                    new()
                    {
                       Name = nameof(AddQuery),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Query
                    },
                    new()
                    {
                       Name = nameof(AddExist),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Exist
                    },
                    new()
                    {
                       Name = nameof(AddOperation),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Operation
                    },
                }
            },
            {
                PatternType.Command,
                new() {
                    new()
                    {
                        Name = nameof(AddBootstrap),
                        DefaultHttpMethod = "Get",
                        Method = RepositoryMethods.Bootstrap
                    },
                    new()
                    {
                       Name = nameof(AddInsert),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Insert
                    },
                    new()
                    {
                       Name = nameof(AddUpdate),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Update
                    },
                    new()
                    {
                       Name = nameof(AddDelete),
                       DefaultHttpMethod = "Get",
                       Method = RepositoryMethods.Delete
                    },
                    new()
                    {
                       Name = nameof(AddBatch),
                       DefaultHttpMethod = "Post",
                       Method = RepositoryMethods.Batch
                    },
                }
            }
        };
    }
}
