namespace Microsoft.Extensions.DependencyInjection
{
    public interface IFactoryName
    {
        void SetFactoryName(AnyOf<string?, Enum>? name);
        bool FactoryNameAlreadySetup { get; set; }
    }
}
