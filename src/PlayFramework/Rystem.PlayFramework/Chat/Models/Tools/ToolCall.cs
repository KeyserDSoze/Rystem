using OpenAI.Chat;

namespace Rystem.PlayFramework
{
    public sealed class ToolCall
    {
        public string Id { get; set; }
        public string FunctionName { get; set; }
        public BinaryData Entity { get; set; }
        public ChatToolCallKind Kind { get; set; }
    }
}
