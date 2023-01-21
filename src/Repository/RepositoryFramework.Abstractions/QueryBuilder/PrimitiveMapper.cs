namespace RepositoryFramework
{
    public sealed class PrimitiveMapper
    {
        public Dictionary<string, string> FromNameToAssemblyQualifiedName { get; }
        public Dictionary<string, string> FromAssemblyQualifiedNameToName { get; }
        private PrimitiveMapper()
        {
            FromNameToAssemblyQualifiedName = new()
            {
                { "int", typeof(int).AssemblyQualifiedName! },
                { "int?", typeof(int?).AssemblyQualifiedName! },
                { "uint", typeof(uint).AssemblyQualifiedName! },
                { "uint?", typeof(uint?).AssemblyQualifiedName! },
                { "short", typeof(short).AssemblyQualifiedName! },
                { "short?", typeof(short?).AssemblyQualifiedName! },
                { "ushort", typeof(ushort).AssemblyQualifiedName! },
                { "ushort?", typeof(ushort?).AssemblyQualifiedName! },
                { "long", typeof(long).AssemblyQualifiedName! },
                { "long?", typeof(long?).AssemblyQualifiedName! },
                { "ulong", typeof(ulong).AssemblyQualifiedName! },
                { "ulong?", typeof(ulong?).AssemblyQualifiedName! },
                { "nint", typeof(nint).AssemblyQualifiedName! },
                { "nint?", typeof(nint?).AssemblyQualifiedName! },
                { "nuint", typeof(nuint).AssemblyQualifiedName! },
                { "nuint?", typeof(nuint?).AssemblyQualifiedName! },
                { "float", typeof(float).AssemblyQualifiedName! },
                { "float?", typeof(float?).AssemblyQualifiedName! },
                { "double", typeof(double).AssemblyQualifiedName! },
                { "double?", typeof(double?).AssemblyQualifiedName! },
                { "decimal", typeof(decimal).AssemblyQualifiedName! },
                { "decimal?", typeof(decimal?).AssemblyQualifiedName! },
                { "Range", typeof(Range).AssemblyQualifiedName! },
                { "Range?", typeof(Range?).AssemblyQualifiedName! },
                { "DateTime", typeof(DateTime).AssemblyQualifiedName! },
                { "DateTime?", typeof(DateTime?).AssemblyQualifiedName! },
                { "TimeSpan", typeof(TimeSpan).AssemblyQualifiedName! },
                { "TimeSpan?", typeof(TimeSpan?).AssemblyQualifiedName! },
                { "DateTimeOffset", typeof(DateTimeOffset).AssemblyQualifiedName! },
                { "DateTimeOffset?", typeof(DateTimeOffset?).AssemblyQualifiedName! },
                { "Guid", typeof(Guid).AssemblyQualifiedName! },
                { "Guid?", typeof(Guid?).AssemblyQualifiedName! },
                { "char", typeof(char).AssemblyQualifiedName! },
                { "char?", typeof(char?).AssemblyQualifiedName! },
                { "bool", typeof(bool).AssemblyQualifiedName! },
                { "bool?", typeof(bool?).AssemblyQualifiedName! },
                { "string", typeof(string).AssemblyQualifiedName! }
            };
            FromAssemblyQualifiedNameToName = FromNameToAssemblyQualifiedName.ToDictionary(x => x.Value, x => x.Key);
        }
        public static PrimitiveMapper Instance { get; } = new();
    }
}
