namespace Rystem.PlayFramework
{
    public interface IChatClientBuilder
    {
        IChatClient AddUserMessage(string message);
        IChatClient AddSystemMessage(string message);
        IChatClient AddAssistantMessage(string message);
        IChatClient AddMaxOutputToken(int maxToken);
        IChatClient AddPriceModel(ChatPriceSettings priceSettings);
    }
}
