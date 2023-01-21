using System.Text.Json.Serialization;

namespace Whistleblowing.Licensing.Models
{
    public class WhistleConfiguration
    {
        [JsonPropertyName("post")]
        public required ApiConfiguration Post { get; set; }
        [JsonPropertyName("update")]
        public required ApiConfiguration Update { get; set; }
        [JsonPropertyName("filter")]
        public required ApiConfiguration Filter { get; set; }
        [JsonPropertyName("download")]
        public required ApiConfiguration Download { get; set; }
        [JsonPropertyName("conversation")]
        public required ApiConfiguration Conversation { get; set; }
        [JsonPropertyName("advisoryConversation")]
        public required ApiConfiguration AdvisoryConversation { get; set; }
        [JsonPropertyName("sendMessage")]
        public required ApiConfiguration SendMessage { get; set; }
        [JsonPropertyName("sendAdvisoryMessage")]
        public required ApiConfiguration SendAdvisoryMessage { get; set; }
        [JsonPropertyName("setAdvisory")]
        public required ApiConfiguration SetAdvisory { get; set; }
        [JsonPropertyName("getAdvisors")]
        public required ApiConfiguration GetAdvisors { get; set; }
        [JsonPropertyName("csv")]
        public required ApiConfiguration GetCsv { get; set; }
        [JsonPropertyName("pdf")]
        public required ApiConfiguration GetPdf { get; set; }
     
    }
}
