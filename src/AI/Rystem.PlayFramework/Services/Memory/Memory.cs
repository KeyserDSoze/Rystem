using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework;

/// <summary>
/// Default implementation of conversation memory service.
/// Uses ChatClientManager for summarization with automatic retry and fallback.
/// </summary>
internal sealed class Memory : IMemory
{
    private readonly IFactory<MemorySettings> _settingsFactory;
    private readonly IFactory<IMemoryStorage> _storageFactory;
    private readonly ILogger<Memory> _logger;
    private MemorySettings? _settings;
    private IMemoryStorage? _storage;
    private string? _factoryName;

    public Memory(
        IFactory<MemorySettings> settingsFactory,
        IFactory<IMemoryStorage> storageFactory,
        ILogger<Memory> logger)
    {
        _settingsFactory = settingsFactory;
        _storageFactory = storageFactory;
        _logger = logger;
    }

    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        _factoryName = name?.ToString() ?? "default";
        _settings = _settingsFactory.Create(name) ?? throw new InvalidOperationException($"MemorySettings not found for factory: {name}");
        _storage = _storageFactory.Create(name) ?? throw new InvalidOperationException($"IMemoryStorage not found for factory: {name}");

        _logger.LogDebug("Memory initialized (Factory: {FactoryName}, MaxSummaryLength: {MaxLength})",
            _factoryName, _settings.MaxSummaryLength);
    }

    public async Task<ConversationMemory> SummarizeAsync(
        ConversationMemory? previousMemory,
        string startingMessage,
        IReadOnlyList<ChatMessage> conversationMessages,
        IReadOnlyDictionary<string, object>? metadata,
        SceneRequestSettings? settings,
        IChatClientManager chatClientManager,
        CancellationToken cancellationToken)
    {
        if (_settings == null || _storage == null)
            throw new InvalidOperationException("Memory not initialized. Call SetFactoryName first.");

        _logger.LogDebug("Starting memory summarization (Factory: {FactoryName}, PreviousMemory: {HasPrevious}, Messages: {MessageCount})",
            _factoryName, previousMemory != null, conversationMessages.Count);

        // Build prompt with previous memory + new conversation
        var promptMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, _settings.SystemPrompt)
        };

        if (_settings.IncludePreviousMemory && previousMemory != null)
        {
            var previousContext = $@"Previous memory (Conversation #{previousMemory.ConversationCount}):
Summary: {previousMemory.Summary}
Important Facts: {JsonSerializer.Serialize(previousMemory.ImportantFacts, JsonHelper.JsonSerializerOptions)}
Last Updated: {previousMemory.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC";

            promptMessages.Add(new ChatMessage(ChatRole.System, previousContext));

            _logger.LogDebug("Including previous memory in summarization (ConversationCount: {Count}, SummaryLength: {Length})",
                previousMemory.ConversationCount, previousMemory.Summary?.Length ?? 0);
        }

        // Add current conversation (including multi-modal content descriptions)
        var conversationText = string.Join("\n\n", conversationMessages.Select(m =>
        {
            var text = $"{m.Role}: {m.Text ?? "[no text]"}";
            var multiModalCount = m.Contents?.Count(c => c is DataContent or UriContent) ?? 0;
            if (multiModalCount > 0)
            {
                text += $" [+{multiModalCount} multi-modal content(s)]";
            }
            return text;
        }));

        promptMessages.Add(new ChatMessage(ChatRole.User, $@"Current conversation (starting message: ""{startingMessage}""):

{conversationText}

Extract and update the memory with only the most critical information. Return ONLY valid JSON."));

        // Call LLM using ChatClientManager (automatic retry + fallback + cost tracking)
        _logger.LogDebug("Calling ChatClientManager for memory summarization (Factory: {FactoryName})",
            _factoryName);

        var responseWithCost = await chatClientManager.GetResponseAsync(
            promptMessages,
            cancellationToken: cancellationToken);

        var responseText = responseWithCost.Response.Messages?.FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("No response from LLM for memory summarization");

        _logger.LogInformation(
            "Memory summarization completed. Cost: {Cost} {Currency}, Tokens: {Tokens} (Factory: {FactoryName})",
            responseWithCost.CalculatedCost, chatClientManager.Currency,
            responseWithCost.TotalTokens,
            _factoryName);

        // Parse JSON response
        var newMemory = ParseMemoryFromJson(responseText, previousMemory);

        // Truncate summary if needed
        if (newMemory.Summary.Length > _settings.MaxSummaryLength)
        {
            _logger.LogWarning(
                "Summary length ({Length}) exceeds max ({Max}). Truncating. (Factory: {FactoryName})",
                newMemory.Summary.Length, _settings.MaxSummaryLength, _factoryName);

            newMemory.Summary = newMemory.Summary[.._settings.MaxSummaryLength];
        }

        // Save to storage
        var conversationKey = settings?.ConversationKey ?? Guid.NewGuid().ToString();
        await _storage.SetAsync(conversationKey, newMemory, metadata, settings, cancellationToken);

        _logger.LogInformation(
            "Memory saved to storage. Key: {Key}, ConversationCount: {Count}, SummaryLength: {Length}, Facts: {FactCount} (Factory: {FactoryName})",
            conversationKey, newMemory.ConversationCount, newMemory.Summary.Length, newMemory.ImportantFacts.Count, _factoryName);

        return newMemory;
    }

    private ConversationMemory ParseMemoryFromJson(string json, ConversationMemory? previous)
    {
        try
        {
            // Try to extract JSON if wrapped in markdown code blocks
            var cleanJson = json.Trim();
            if (cleanJson.StartsWith("```json"))
            {
                cleanJson = cleanJson["```json".Length..];
            }
            if (cleanJson.StartsWith("```"))
            {
                cleanJson = cleanJson[3..];
            }
            if (cleanJson.EndsWith("```"))
            {
                cleanJson = cleanJson[..^3];
            }
            cleanJson = cleanJson.Trim();

            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(cleanJson);
            if (parsed == null)
            {
                throw new InvalidOperationException("Failed to parse memory JSON: result was null");
            }

            var summary = parsed.TryGetValue("summary", out var summaryElement)
                ? summaryElement.GetString() ?? string.Empty
                : string.Empty;

            var importantFacts = new Dictionary<string, object>();
            if (parsed.TryGetValue("importantFacts", out var factsElement) && factsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in factsElement.EnumerateObject())
                {
                    importantFacts[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                        JsonValueKind.Number => prop.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => "null",
                        _ => prop.Value.ToString()
                    };
                }
            }

            return new ConversationMemory
            {
                Summary = summary,
                ImportantFacts = importantFacts,
                LastUpdated = DateTime.UtcNow,
                ConversationCount = (previous?.ConversationCount ?? 0) + 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse memory JSON. Raw response: {Response} (Factory: {FactoryName})",
                json, _factoryName);

            // Fallback: create basic memory from response text
            return new ConversationMemory
            {
                Summary = json.Length > 2000 ? json[..2000] : json,
                ImportantFacts = new Dictionary<string, object>
                {
                    ["raw_response"] = json,
                    ["parse_error"] = ex.Message
                },
                LastUpdated = DateTime.UtcNow,
                ConversationCount = (previous?.ConversationCount ?? 0) + 1
            };
        }
    }
}
