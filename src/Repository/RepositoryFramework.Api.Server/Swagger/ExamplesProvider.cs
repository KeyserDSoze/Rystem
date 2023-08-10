using System.Population.Random;
using Swashbuckle.AspNetCore.Filters;

namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class ExamplesProvider<T> : IExamplesProvider<T>
    {
        private readonly T _value;
        public ExamplesProvider(IPopulation<T> populationService)
        {
            _value = populationService.Populate(1, 1).First();
        }
        public T GetExamples() => _value;
    }
}
