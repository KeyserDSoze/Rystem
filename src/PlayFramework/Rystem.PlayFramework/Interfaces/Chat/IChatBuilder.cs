namespace Rystem.PlayFramework
{
    public interface IChatBuilder
    {
        IChatBuilder AddConfiguration(string configurationName, Action<ChatBuilderSettings> settings);
    }
}
