namespace Rystem.PlayFramework
{
    public interface IChatClientToolBuilder
    {
        IChatClient AddStrictTool<T>(string name, string description);
        IChatClient AddTool<T>(string name, string description);
        IChatClient AddStrictTool<T>(string name, string description, T entity);
        IChatClient AddTool<T>(string name, string description, T entity);
        IChatClient AddStrictTool(Tool tool);
        IChatClient AddTool(Tool tool);
        IChatClient ClearTools();
    }
}
