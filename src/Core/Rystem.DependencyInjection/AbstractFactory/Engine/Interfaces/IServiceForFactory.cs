namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceForFactory
    {
        void SetFactoryName(string name);
        bool FactoryNameAlreadySetup { get; set; }
    }
}
