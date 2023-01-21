namespace System.Population.Random
{
    internal class ArrayPopulationService : IRandomPopulationService
    {
        public int Priority => 1;
        public dynamic GetValue(PopulationSettings settings, RandomPopulationOptions options)
        {
            var entity = Activator.CreateInstance(options.Type, options.NumberOfEntities);
            var valueType = options.Type.GetElementType();
            for (var i = 0; i < options.NumberOfEntities; i++)
                (entity as dynamic)![i] = options.PopulationService.Construct(settings, valueType!,
                    options.NumberOfEntities, options.TreeName, string.Empty);
            return entity!;
        }
        public bool IsValid(Type type)
            => type.IsArray;
    }
}
