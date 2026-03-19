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
    private readonly IFactory<PlayFrameworkSettings> _settingsFactory;
    private readonly IFactory<ITransientErrorDetector> _errorDetectorFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChatClientManager> _logger;
    private readonly Services.Helpers.IToolExecutionManager _toolExecutionManager;

    // Resolved dependencies (set via SetFactoryName)
    private PlayFrameworkSettings _settings = null!;
    private ITransientErrorDetector _errorDetector = null!;

    // Lazy client cache (thread-safe)
    private readonly ConcurrentDictionary<string, Lazy<IChatClient>> _clientCache = new();

    // Load balancing state
    private int _roundRobinIndex = 0;
    private readonly Random _random = new();
    private string _currency = "USD";

    public ChatClientManager(
        IFactory<IChatClient> chatClientFactory,
        IFactory<PlayFrameworkSettings> settingsFactory,
        IFactory<ITransientErrorDetector> errorDetectorFactory,
        IServiceProvider serviceProvider,
        ILogger<ChatClientManager> logger,
        Services.Helpers.IToolExecutionManager toolExecutionManager)
    {
        _chatClientFactory = chatClientFactory;
        _settingsFactory = settingsFactory;
        _errorDetectorFactory = errorDetectorFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _toolExecutionManager = toolExecutionManager;
    }

    public bool FactoryNameAlreadySetup { get; set; }
    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        _logger.LogDebug("ChatClientManager factory name set to: {FactoryName}", name?.ToString() ?? "default");

        _settings = _settingsFactory.Create(name)
            ?? throw new InvalidOperationException($"PlayFrameworkSettings not found for factory: {name?.ToString() ?? "default"}");
        _errorDetector = _errorDetectorFactory.Create(name)
            ?? throw new InvalidOperationException($"ITransientErrorDetector not found for factory: {name?.ToString() ?? "default"}");
    }

    public string? ModelId => null;

    /// <summary>Currency is propagated from the first adapter response that includes a <see cref="CostCalculation"/>.
    /// Adapters embed per-call costs via <see cref="CostTrackingChatClient"/>; this surfaces the currency for budget messages.</summary>
    public string Currency => _currency;

    #region Main API Methods

    public async Task<ChatResponseWithCost> GetResponseAsync(
        List<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        await foreach (var clientInfo in GetClientsToTryAsync(cancellationToken))
        {
            try
            {
                _logger.LogDebug("Trying {Phase} client: {Client} (Attempt {Attempt}/{MaxAttempts})",
                    clientInfo.Phase, clientInfo.ClientName, clientInfo.Attempt, clientInfo.MaxAttempts);

                var response = await clientInfo.Client.GetResponseAsync(chatMessages, options, cancellationToken);

                // Centralized deduplication using ToolExecutionManager
                response = _toolExecutionManager.DeduplicateToolCalls(response);

                // Cost is pre-calculated by CostTrackingChatClient inside the adapter (if configured).
                var costCalc = response.AdditionalProperties?.TryGetValue(PlayFrameworkCostConstants.CostCalculationKey, out var costObj) == true
                    ? costObj as CostCalculation
                    : null;
                if (costCalc?.Currency != null) _currency = costCalc.Currency;
                var cost = costCalc?.TotalCost ?? 0m;
                var inputTokens = (int)(response.Usage?.InputTokenCount ?? 0);
                var outputTokens = (int)(response.Usage?.OutputTokenCount ?? 0);
                var cachedInputTokens = (int)(response.Usage?.CachedInputTokenCount ?? 0);

                _logger.LogInformation("{Phase} client {Client} succeeded (Attempt {Attempt}, Tokens: {Input}->{Output}, Cost: {Cost:F6})",
                    clientInfo.Phase, clientInfo.ClientName, clientInfo.Attempt,
                    inputTokens, outputTokens, cost);

                return new ChatResponseWithCost
                {
                    Response = response,
                    CalculatedCost = cost,
                    InputTokens = inputTokens,
                    OutputTokens = outputTokens,
                    CachedInputTokens = cachedInputTokens,
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
                    _logger.LogWarning("Transient error from {Client} (Attempt {Attempt}/{MaxAttempts}), retrying in {Delay}s: {Error}",
                        clientInfo.ClientName, clientInfo.Attempt, clientInfo.MaxAttempts, delay.TotalSeconds, ex.Message);
                    await Task.Delay(delay, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("{ErrorType} error from {Client}: {Error}",
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
        List<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var anyClientWorked = false;

        await foreach (var clientInfo in GetClientsToTryAsync(cancellationToken))
        {
            var streamSuccess = false;

            _logger.LogDebug("Trying {Phase} streaming client: {Client}", clientInfo.Phase, clientInfo.ClientName);

            await foreach (var update in TryStreamFromClientAsync(
                clientInfo.Client,
                clientInfo.ClientName,
                chatMessages,
                options,
                cancellationToken))
            {
                anyClientWorked = true;

                if (update.IsError)
                {
                    _logger.LogWarning("Streaming error from {Client}: {Error}", clientInfo.ClientName, update.ErrorMessage);
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
                _logger.LogInformation("Streaming succeeded from {Phase} client: {Client}", clientInfo.Phase, clientInfo.ClientName);
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
                    IChatClient client;
                    try
                    {
                        client = GetOrCreateClient(clientName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to create client {Client}: {Error}", clientName, ex.Message);
                        break;
                    }

                    yield return new ClientAttemptInfo
                    {
                        Client = client,
                        ClientName = clientName,
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
            _logger.LogWarning("Primary pool exhausted, switching to fallback chain");
            var orderedClients = GetFallbackOrder(_settings.FallbackChatClientNames, _settings.FallbackMode);

            foreach (var clientName in orderedClients)
            {
                for (int attempt = 1; attempt <= _settings.MaxRetryAttempts; attempt++)
                {
                    IChatClient client;
                    try
                    {
                        client = GetOrCreateClient(clientName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to create fallback client {Client}: {Error}", clientName, ex.Message);
                        break;
                    }

                    yield return new ClientAttemptInfo
                    {
                        Client = client,
                        ClientName = clientName,
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
            _logger.LogWarning("No named clients configured, using direct IChatClient");
            yield return new ClientAttemptInfo
            {
                Client = directClient,
                ClientName = "direct",
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

            // Usage counts come from UsageContent inside Contents.
            var usageContent = update.Contents?.OfType<UsageContent>().FirstOrDefault();
            var inputTokens = (int)(usageContent?.Details.InputTokenCount ?? 0);
            var outputTokens = (int)(usageContent?.Details.OutputTokenCount ?? 0);
            var cachedInputTokens = (int)(usageContent?.Details.CachedInputTokenCount ?? 0);

            // Cost is pre-calculated by CostTrackingChatClient inside the adapter (if configured).
            var costCalc = update.AdditionalProperties?.TryGetValue(PlayFrameworkCostConstants.CostCalculationKey, out var costObj) == true
                ? costObj as CostCalculation
                : null;
            if (costCalc?.Currency != null) _currency = costCalc.Currency;
            var estimatedCost = costCalc?.TotalCost ?? 0m;

            yield return new ChatUpdateWithCost
            {
                Update = update,
                EstimatedCost = estimatedCost,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                CachedInputTokens = cachedInputTokens,
                IsComplete = isComplete,
                ClientName = clientName,
                IsError = false
            };
        }

        // If streaming ended without explicit FinishReason, emit a synthetic completion
        if (!receivedExplicitCompletion)
        {
            _logger.LogDebug("Streaming ended without explicit FinishReason, emitting synthetic completion for {Client}", clientName);

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
            _logger.LogDebug("Lazy-loaded chat client: {ClientName}", name);
            return client;
        })).Value;

    #endregion

    #region Internal Types

    /// <summary>
    /// Information about a client attempt for the centralized provider.
    /// </summary>
    private sealed class ClientAttemptInfo
    {
        public required IChatClient Client { get; init; }
        public required string ClientName { get; init; }
        public required string Phase { get; init; }
        public int Attempt { get; init; }
        public int MaxAttempts { get; init; }
    }

    #endregion
}
