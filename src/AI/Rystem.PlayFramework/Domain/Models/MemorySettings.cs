namespace Rystem.PlayFramework;

/// <summary>
/// Configuration for conversation memory.
/// </summary>
public sealed class MemorySettings
{
    /// <summary>
    /// Whether memory is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Maximum length of the summary in characters.
    /// Default: 2000.
    /// </summary>
    public int MaxSummaryLength { get; set; } = 2000;

    /// <summary>
    /// System prompt for memory summarization.
    /// Used to instruct the LLM on how to extract and format important information.
    /// </summary>
    public string SystemPrompt { get; set; } = @"You are a conversation memory system. Your task is to:

1. Extract ONLY the most critical information from the conversation
2. Update the existing memory with new important facts
3. Keep the summary concise and focused (max 2000 characters)
4. Return a JSON object with this structure:
{
  ""summary"": ""Brief summary of the entire conversation context"",
  ""importantFacts"": {
    ""key1"": ""value1"",
    ""key2"": ""value2""
  }
}

Focus on: user identity, preferences, ongoing issues, decisions made, and action items.
Ignore: greetings, small talk, and redundant information.";

    /// <summary>
    /// Whether to include previous memory in summarization prompts.
    /// Default: true.
    /// </summary>
    public bool IncludePreviousMemory { get; set; } = true;

    /// <summary>
    /// Metadata keys to use for building storage key (similar to rate limiting GroupBy).
    /// Example: ["userId", "sessionId"] → storage key: "userId:john|sessionId:abc123"
    /// If null/empty, uses ConversationKey from SceneRequestSettings.
    /// Set via .WithDefaultMemoryStorage("userId", "sessionId") in MemoryBuilder.
    /// </summary>
    public string[]? StorageKeys { get; set; }
}
