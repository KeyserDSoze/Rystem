using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework;

namespace Rystem.Localization
{
    internal sealed class Languages<T> : ILanguages<T>, IServiceForFactory
    {
        public RystemLocalizationFiles<T> Localizer { get; } = new RystemLocalizationFiles<T>
        {
            AllLanguages = []
        };
        private string? _factoryName;
        public void SetFactoryName(string name)
        {
            _factoryName = name;
        }

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
