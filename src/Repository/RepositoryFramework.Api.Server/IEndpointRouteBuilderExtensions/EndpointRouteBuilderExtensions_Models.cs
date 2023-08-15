using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RepositoryFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class EndpointRouteBuilderExtensions
    {
        private static bool s_modelsAlreadyMapped = false;
        private static void AddApiModels(this IEndpointRouteBuilder app)
        {
            if (!s_modelsAlreadyMapped)
            {
                s_modelsAlreadyMapped = true;
                var languages = new List<ProgrammingLanguage>() { ProgrammingLanguage.Typescript };
                var registry = app.ServiceProvider.GetService<RepositoryFrameworkRegistry>();
                var typeList = registry!.Services.Select(x => x.Value.ModelType).Where(x => !x.IsPrimitive()).ToList();
                typeList.AddRange(registry.Services.Select(x => x.Value.KeyType).Where(x => !x.IsPrimitive()));
                foreach (var language in languages)
                {
                    var converted = typeList.ConvertAs(ProgrammingLanguage.Typescript);
                    Try.WithDefaultOnCatch(() =>
                    {
                        app
                            .MapGet($"Repository/Models/{language}", () =>
                            {
                                return Results.Text(converted.Text, contentType: converted.MimeType);
                            })
                            .WithTags($"_RepositoryModels-{language}");
                    });
                }
            }
        }
    }
}
