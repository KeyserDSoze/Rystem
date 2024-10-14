using System.Text;
using System.Text.Json.Serialization;
using OpenAI.Chat;

namespace Rystem.PlayFramework
{
    public sealed class ChatResponse
    {
        public string? LastChunk { get; set; }
        public string? FullText { get; set; }
        public ChatFinishReason FinishReason { get; set; }
        public bool NeedToolCall => FinishReason == ChatFinishReason.ToolCalls;
        public bool NeedFunctionCall => FinishReason == ChatFinishReason.FunctionCall;
        public bool NeedFunctionCallOrToolCall => NeedToolCall || NeedFunctionCall;
        public bool ProblemWithContent => FinishReason == ChatFinishReason.ContentFilter;
        public bool LengthReached => FinishReason == ChatFinishReason.Length;
        public bool HasNormallyEnded => FinishReason == ChatFinishReason.Stop;
        public bool HasEnded => NeedToolCall || NeedFunctionCall || ProblemWithContent || LengthReached || HasNormallyEnded;
        [JsonIgnore]
        public StringBuilder FullTextStringBuilder { get; set; } = new();
        public ChatPrice? Price { get; set; }
        public List<ToolCall>? ToolCalls { get; set; }
    }
}
