using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework;

namespace Rystem.Localization
{
    internal sealed class Languages<T> : ILanguages<T>
    {
        public RystemLocalizationFiles<T> Localizer { get; } = new RystemLocalizationFiles<T>
        {
            AllLanguages = []
        };
        private readonly AnyOf<string?, Enum>? _factoryName;
        public Languages(AnyOf<string?, Enum>? factoryName)
             => _factoryName = factoryName;

        public async ValueTask WarmUpAsync(IServiceProvider serviceProvider)
        {
            var repository = serviceProvider.GetRequiredService<IFactory<IRepository<T, string>>>().Create(_factoryName);
            var allLanguages = await repository.ToListAsync();
            foreach (var language in allLanguages)
            {
                Localizer.AllLanguages.Add(language.Key, language.Value);
            }
            if (Localizer.AllLanguages.Count == 0)
            {
                throw new Exception("No languages found");
            }
        }
    }
}
