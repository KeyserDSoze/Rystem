using System.Population.Random;

namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class ExamplesProvider<T> : IExamplesProvider<T>
    {
        private readonly T _value;
        public ExamplesProvider(IPopulation<T> populationService)
        {
            _value = populationService.Populate(1, 1).First();
        }
        public ExamplesProvider(T value)
        {
            _value = value;
        }
        public T GetExamples() => _value;
    }
    public interface IExamplesProvider<out T>
    {
        T GetExamples();
    }
}
