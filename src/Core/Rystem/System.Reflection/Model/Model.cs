namespace System.Reflection
{
    public static class Model
    {
        public static ModelBuilder Create(string name)
            => new ModelBuilder(name);
        public static Type GetType(string name) => ModelBuilder.Types[name];
        public static dynamic Construct(string name)
            => Activator.CreateInstance(GetType(name))!;
    }
}
