namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceWithFactoryWithOptions : IServiceForFactory
    {
        bool OptionsAlreadySetup { get; set; }
    }
}
