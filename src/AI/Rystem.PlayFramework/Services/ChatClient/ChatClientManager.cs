using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rystem;

namespace Rystem.PlayFramework;

/// <summary>
/// Unified chat client manager with centralized load balancing, fallback, retry, and cost calculation.
/// Uses a single client provider pattern for both streaming and non-streaming requests.
/// </summary>
internal sealed class ChatClientManager : IChatClientManager, IFactoryName
{
    private readonly IFactory<IChatClient> _chatClientFactory;
    private readonly IFactory<TokenCostSettings> _costSettingsFactory;
    private readonly IFactory<PlayFrameworkSettings> _settingsFactory;
    private readonly IFactory<ITransientErrorDetector> _errorDetectorFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChatClientManager> _logger;
    private readonly Services.Helpers.IToolExecutionManager _toolExecutionManager;

    // Resolved dependencies (set via SetFactoryName)
    private PlayFrameworkSettings _settings = null!;
    private TokenCostSettings? _defaultCostSettings;
    private ITransientErrorDetector _errorDetector = null!;

    // Lazy client caches (thread-safe)
    private readonly ConcurrentDictionary<string, Lazy<IChatClient>> _clientCache = new();
    private readonly ConcurrentDictionary<string, Lazy<TokenCostSettings?>> _costSettingsCache = new();

    // Load balancing state
    private int _roundRobinIndex = 0;
    private readonly Random _random = new();

    public ChatClientManager(
        IFactory<IChatClient> chatClientFactory,
        IFactory<TokenCostSettings> costSettingsFactory,
        IFactory<PlayFrameworkSettings> settingsFactory,
        IFactory<ITransientErrorDetector> errorDetectorFactory,
        IServiceProvider serviceProvider,
        ILogger<ChatClientManager> logger,
        Services.Helpers.IToolExecutionManager toolExecutionManager)
    {
        _chatClientFactory = chatClientFactory;
        _costSettingsFactory = costSettingsFactory;
        _settingsFactory = settingsFactory;
        _errorDetectorFactory = errorDetectorFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _toolExecutionManager = toolExecutionManager;
    }

    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        _logger.LogDebug("ChatClientManager factory name set to: {FactoryName}", name?.ToString() ?? "default");

        _settings = _settingsFactory.Create(name)
            ?? throw new InvalidOperationException($"PlayFrameworkSettings not found for factory: {name?.ToString() ?? "default"}");
        _defaultCostSettings = _costSettingsFactory.Create(name);
        _errorDetector = _errorDetectorFactory.Create(name)
            ?? throw new InvalidOperationException($"ITransientErrorDetector not found for factory: {name?.ToString() ?? "default"}");
    }

    public string? ModelId => null;

    public string Currency
    {
        get
        {
            if (_settings?.ChatClientNames?.Count > 0)
            {
                var firstClientCostSettings = GetOrCreateCostSettings(_settings.ChatClientNames[0]);
                return firstClientCostSettings?.Currency ?? "USD";
            }
            return _defaultCostSettings?.Currency ?? "USD";
        }
    }

    #region Main API Methods

    public async Task<ChatResponseWithCost> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messageList = chatMessages as IList<ChatMessage> ?? chatMessages.ToList();
        Exception? lastException = null;

        await foreach (var clientInfo in GetClientsToTryAsync(cancellationToken))
        {
            try
            {
                _logger.LogDebug("🎯 Trying {Phase} client: {Client} (Attempt {Attempt}/{MaxAttempts})",
                    clientInfo.Phase, clientInfo.ClientName, clientInfo.Attempt, clientInfo.MaxAttempts);

                var response = await clientInfo.Client.GetResponseAsync(messageList, options, cancellationToken);

                // Centralized deduplication using ToolExecutionManager
                response = _toolExecutionManager.DeduplicateToolCalls(response);

                var cost = CalculateCost(response, clientInfo.CostSettings);

                _logger.LogInformation("✅ {Phase} client {Client} succeeded (Attempt {Attempt}, Tokens: {Input}→{Output}, Cost: {Cost:F6})",
                    clientInfo.Phase, clientInfo.ClientName, clientInfo.Attempt,
                    response.Usage?.InputTokenCount ?? 0, response.Usage?.OutputTokenCount ?? 0, cost);

                return new ChatResponseWithCost
                {
                    Response = response,
                    CalculatedCost = cost,
                    InputTokens = (int)(response.Usage?.InputTokenCount ?? 0),
                    OutputTokens = (int)(response.Usage?.OutputTokenCount ?? 0),
                    CachedInputTokens = 0,
                    ClientName = clientInfo.ClientName
                };
            }
            catch (Exception ex)
            {
                lastException = ex;
                var isTransient = !_errorDetector.IsNonTransient(ex);

                if (isTransient && clientInfo.Attempt < clientInfo.MaxAttempts)
                {
                    var delay = TimeSpan.FromSeconds(_settings.RetryBaseDelaySeconds * Math.Pow(2, clientInfo.Attempt - 1));
                    _logger.LogWarning("⚠️ Transient error from {Client} (Attempt {Attempt}/{MaxAttempts}), retrying in {Delay}s: {Error}",
                        clientInfo.ClientName, clientInfo.Attempt, clientInfo.MaxAttempts, delay.TotalSeconds, ex.Message);
                    await Task.Delay(delay, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("❌ {ErrorType} error from {Client}: {Error}",
                        isTransient ? "Transient (max retries)" : "Non-transient", clientInfo.ClientName, ex.Message);
                }
            }
        }
        return new ChatResponseWithCost
        {
            Response = new ChatResponse(new ChatMessage(ChatRole.Assistant, lastException?.Message ?? "Generic error")),
            CalculatedCost = 0,
            InputTokens = 0,
            OutputTokens = 0,
            CachedInputTokens = 0,
            ClientName = string.Empty
        };
    }

    public async IAsyncEnumerable<ChatUpdateWithCost> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messageList = chatMessages as IList<ChatMessage> ?? chatMessages.ToList();
        var anyClientWorked = false;

        await foreach (var clientInfo in GetClientsToTryAsync(cancellationToken))
        {
            var streamSuccess = false;

            _logger.LogDebug("🎯 Trying {Phase} streaming client: {Client}", clientInfo.Phase, clientInfo.ClientName);

            await foreach (var update in TryStreamFromClientAsync(
                clientInfo.Client,
                clientInfo.ClientName,
                clientInfo.CostSettings,
                messageList,
                options,
                cancellationToken))
            {
                anyClientWorked = true;

                if (update.IsError)
                {
                    _logger.LogWarning("⚠️ Streaming error from {Client}: {Error}", clientInfo.ClientName, update.ErrorMessage);
                    break;
                }

                // Note: Deduplication of function calls happens in StreamingHelper
                // using ToolCallDeduplicator, since it's the one accumulating them
                if (update.IsComplete)
                {
                    streamSuccess = true;
                }

                yield return update;
            }

            if (streamSuccess)
            {
                _logger.LogInformation("✅ Streaming succeeded from {Phase} client: {Client}", clientInfo.Phase, clientInfo.ClientName);
                yield break;
            }
        }

        if (!anyClientWorked)
        {
            throw new InvalidOperationException("All streaming clients (load balancing + fallback + direct) failed");
        }
    }

    #endregion

    #region Centralized Client Provider

    /// <summary>
    /// Central method that yields clients in priority order:
    /// 1. Load balanced pool (with retry per client)
    /// 2. Fallback chain (with retry per client)
    /// 3. Direct IChatClient (backward compatibility)
    /// </summary>
    private async IAsyncEnumerable<ClientAttemptInfo> GetClientsToTryAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Phase 1: Load balanced pool
        if (_settings.ChatClientNames?.Count > 0)
        {
            var orderedClients = GetLoadBalancedOrder(_settings.ChatClientNames, _settings.LoadBalancingMode);

            foreach (var clientName in orderedClients)
            {
                for (int attempt = 1; attempt <= _settings.MaxRetryAttempts; attempt++)
                {
                    IChatClient? client;
                    TokenCostSettings? costSettings;

                    try
                    {
                        client = GetOrCreateClient(clientName);
                        costSettings = GetOrCreateCostSettings(clientName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("⚠️ Failed to create client {Client}: {Error}", clientName, ex.Message);
                        break; // Skip this client entirely
                    }

                    yield return new ClientAttemptInfo
                    {
                        Client = client,
                        ClientName = clientName,
                        CostSettings = costSettings,
                        Phase = "LoadBalanced",
                        Attempt = attempt,
                        MaxAttempts = _settings.MaxRetryAttempts
                    };
                }
            }
        }

        // Phase 2: Fallback chain
        if (_settings.FallbackChatClientNames?.Count > 0)
        {
            _logger.LogWarning("🔄 Primary pool exhausted, switching to fallback chain");
            var orderedClients = GetFallbackOrder(_settings.FallbackChatClientNames, _settings.FallbackMode);

            foreach (var clientName in orderedClients)
            {
                for (int attempt = 1; attempt <= _settings.MaxRetryAttempts; attempt++)
                {
                    IChatClient? client;
                    TokenCostSettings? costSettings;

                    try
                    {
                        client = GetOrCreateClient(clientName);
                        costSettings = GetOrCreateCostSettings(clientName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("⚠️ Failed to create fallback client {Client}: {Error}", clientName, ex.Message);
                        break;
                    }

                    yield return new ClientAttemptInfo
                    {
                        Client = client,
                        ClientName = clientName,
                        CostSettings = costSettings,
                        Phase = "Fallback",
                        Attempt = attempt,
                        MaxAttempts = _settings.MaxRetryAttempts
                    };
                }
            }
        }

        // Phase 3: Direct IChatClient (backward compatibility)
        var directClient = _serviceProvider.GetService<IChatClient>();
        if (directClient != null)
        {
            _logger.LogWarning("🔄 No named clients configured, using direct IChatClient");
            yield return new ClientAttemptInfo
            {
                Client = directClient,
                ClientName = "direct",
                CostSettings = _defaultCostSettings,
                Phase = "Direct",
                Attempt = 1,
                MaxAttempts = 1
            };
        }

        await Task.CompletedTask; // Async iterator requirement
    }

    #endregion

    #region Streaming Helper

    private async IAsyncEnumerable<ChatUpdateWithCost> TryStreamFromClientAsync(
        IChatClient client,
        string clientName,
        TokenCostSettings? costSettings,
        IList<ChatMessage> chatMessages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ChatResponseUpdate? lastUpdate = null;
        var receivedExplicitCompletion = false;

        await foreach (var update in client.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            lastUpdate = update;

            var isComplete = update.FinishReason != null;
            if (isComplete)
            {
                receivedExplicitCompletion = true;
            }

            var inputTokens = (int)(update.Contents?.FirstOrDefault()?.GetType().GetProperty("InputTokens")?.GetValue(update.Contents.First()) ?? 0);
            var outputTokens = (int)(update.Contents?.FirstOrDefault()?.GetType().GetProperty("OutputTokens")?.GetValue(update.Contents.First()) ?? 0);

            var estimatedCost = 0m;
            if (isComplete && costSettings != null)
            {
                estimatedCost = ((decimal)inputTokens / 1_000m) * costSettings.InputTokenCostPer1K +
                              ((decimal)outputTokens / 1_000m) * costSettings.OutputTokenCostPer1K;
            }

            yield return new ChatUpdateWithCost
            {
                Update = update,
                EstimatedCost = estimatedCost,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                CachedInputTokens = 0,
                IsComplete = isComplete,
                ClientName = clientName,
                IsError = false
            };
        }

        // If streaming ended without explicit FinishReason, emit a synthetic completion
        if (!receivedExplicitCompletion)
        {
            _logger.LogDebug("📍 Streaming ended without explicit FinishReason, emitting synthetic completion for {Client}", clientName);

            yield return new ChatUpdateWithCost
            {
                Update = lastUpdate, // Last received update (or null if empty stream)
                EstimatedCost = 0,
                InputTokens = 0,
                OutputTokens = 0,
                CachedInputTokens = 0,
                IsComplete = true, // Mark as complete!
                ClientName = clientName,
                IsError = false
            };
        }
    }

    #endregion

    #region Load Balancing Logic

    private List<string> GetLoadBalancedOrder(List<string> clients, LoadBalancingMode mode) =>
        mode switch
        {
            LoadBalancingMode.RoundRobin => GetRoundRobinOrder(clients),
            LoadBalancingMode.Random => GetRandomOrder(clients),
            LoadBalancingMode.Sequential => [.. clients],
            LoadBalancingMode.None => clients.Take(1).ToList(),
            _ => [.. clients]
        };

    private List<string> GetFallbackOrder(List<string> clients, FallbackMode mode) =>
        mode switch
        {
            FallbackMode.RoundRobin => GetRoundRobinOrder(clients),
            FallbackMode.Random => GetRandomOrder(clients),
            _ => [.. clients]
        };

    private List<string> GetRoundRobinOrder(List<string> clients)
    {
        var index = Interlocked.Increment(ref _roundRobinIndex) % clients.Count;
        return [.. clients.Skip(index), .. clients.Take(index)];
    }

    private List<string> GetRandomOrder(List<string> clients)
    {
        var shuffled = clients.ToList();
        lock (_random)
        {
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
        }
        return shuffled;
    }

    #endregion

    #region Client & Cost Helpers

    private IChatClient GetOrCreateClient(string name) =>
        _clientCache.GetOrAdd(name, _ => new Lazy<IChatClient>(() =>
        {
            var client = _chatClientFactory.Create(name)
                ?? throw new InvalidOperationException($"IChatClient '{name}' not found in factory");
            _logger.LogDebug("📦 Lazy-loaded chat client: {ClientName}", name);
            return client;
        })).Value;

    private TokenCostSettings? GetOrCreateCostSettings(string name) =>
        _costSettingsCache.GetOrAdd(name, _ => new Lazy<TokenCostSettings?>(() =>
        {
            var settings = _costSettingsFactory.Create(name);
            if (settings != null)
            {
                _logger.LogDebug("📦 Lazy-loaded cost settings for: {ClientName} (Input: ${Input}/1K, Output: ${Output}/1K)",
                    name, settings.InputTokenCostPer1K, settings.OutputTokenCostPer1K);
            }
            return settings;
        })).Value;

    private decimal CalculateCost(ChatResponse response, TokenCostSettings? settings)
    {
        if (settings == null || response.Usage == null)
            return 0m;

        var inputCost = ((decimal)(response.Usage.InputTokenCount ?? 0) / 1_000m) * settings.InputTokenCostPer1K;
        var outputCost = ((decimal)(response.Usage.OutputTokenCount ?? 0) / 1_000m) * settings.OutputTokenCostPer1K;

        return inputCost + outputCost;
    }

    #endregion

    #region Internal Types

    /// <summary>
    /// Information about a client attempt for the centralized provider.
    /// </summary>
    private sealed class ClientAttemptInfo
    {
        public required IChatClient Client { get; init; }
        public required string ClientName { get; init; }
        public TokenCostSettings? CostSettings { get; init; }
        public required string Phase { get; init; }
        public int Attempt { get; init; }
        public int MaxAttempts { get; init; }
    }

    #endregion
}
