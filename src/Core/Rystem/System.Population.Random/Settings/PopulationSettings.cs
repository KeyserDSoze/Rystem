namespace System.Population.Random
{
    public class PopulationSettings
    {
        public Dictionary<string, string[]> RegexForValueCreation { get; set; } = new();
        public Dictionary<string, Func<dynamic>> DelegatedMethodForValueCreation { get; set; } = new();
        public Dictionary<string, Func<IServiceProvider, Task<dynamic>>> DelegatedMethodForValueRetrieving { get; set; } = new();
        public Dictionary<string, Func<IServiceProvider, Task<IEnumerable<dynamic>>>> DelegatedMethodWithRandomForValueRetrieving { get; set; } = new();
        public Dictionary<string, dynamic> AutoIncrementations { get; set; } = new();
        public Dictionary<string, Type> ImplementationForValueCreation { get; set; } = new();
        public Dictionary<string, int> NumberOfElements { get; set; } = new();
    }
}
