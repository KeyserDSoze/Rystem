using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Rystem.PlayFramework;

/// <summary>
/// Unified chat client manager with load balancing, fallback, retry, and cost calculation.
/// </summary>
internal sealed class ChatClientManager : IChatClientManager, IFactoryName
{
    private readonly IFactory<IChatClient> _chatClientFactory;
    private readonly IFactory<TokenCostSettings> _costSettingsFactory;
    private readonly IFactory<PlayFrameworkSettings> _settingsFactory;
    private readonly IFactory<ITransientErrorDetector> _errorDetectorFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChatClientManager> _logger;

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
        ILogger<ChatClientManager> logger)
    {
        _chatClientFactory = chatClientFactory;
        _costSettingsFactory = costSettingsFactory;
        _settingsFactory = settingsFactory;
        _errorDetectorFactory = errorDetectorFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        _logger.LogDebug("ChatClientManager factory name set to: {FactoryName}", name?.ToString() ?? "default");

        // Resolve all dependencies immediately
        _settings = _settingsFactory.Create(name) ?? throw new InvalidOperationException($"PlayFrameworkSettings not found for factory: {name?.ToString() ?? "default"}");
        _defaultCostSettings = _costSettingsFactory.Create(name);
        _errorDetector = _errorDetectorFactory.Create(name) ?? throw new InvalidOperationException($"ITransientErrorDetector not found for factory: {name?.ToString() ?? "default"}");
    }

    public string? ModelId => null; // Client-specific, available in response

    public string Currency
    {
        get
        {
            if (_settings?.ChatClientNames?.Count > 0)
            {
                var firstClientCostSettings = GetOrCreateCostSettings(_settings.ChatClientNames[0]);
                return firstClientCostSettings?.Currency ?? "USD";
            }

            // Fallback: use default cost settings
            return _defaultCostSettings?.Currency ?? "USD";
        }
    }

    public async Task<ChatResponseWithCost> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Convert to list for multiple enumeration
        var messageList = chatMessages as IList<ChatMessage> ?? chatMessages.ToList();

        // PHASE 1: Try load balancing pool
        if (_settings.ChatClientNames?.Count > 0)
        {
            _logger.LogDebug("🎯 Attempting load balancing pool with {ClientCount} clients (Mode: {Mode})",
                _settings.ChatClientNames.Count, _settings.LoadBalancingMode);

            var poolResult = await TryLoadBalancingPoolAsync(
                _settings,
                _errorDetector,
                messageList,
                options,
                cancellationToken);

            if (poolResult.Success)
            {
                _logger.LogInformation("✅ Load balancing pool succeeded (Client: {Client}, Cost: {Cost:F6})",
                    poolResult.Response!.ClientName, poolResult.Response.CalculatedCost);
                return poolResult.Response;
            }

            _logger.LogWarning("⚠️ Load balancing pool exhausted. Last error: {Error}",
                poolResult.LastException?.Message ?? "Unknown");
        }

        // PHASE 2: Try fallback chain
        if (_settings.FallbackChatClientNames?.Count > 0)
        {
            _logger.LogWarning("🔄 Switching to fallback chain with {ClientCount} clients (Mode: {Mode})",
                _settings.FallbackChatClientNames.Count, _settings.FallbackMode);

            var fallbackResult = await TryFallbackChainAsync(
                _settings,
                _errorDetector,
                messageList,
                options,
                cancellationToken);

            if (fallbackResult.Success)
            {
                _logger.LogInformation("✅ Fallback chain succeeded (Client: {Client}, Cost: {Cost:F6})",
                    fallbackResult.Response!.ClientName, fallbackResult.Response.CalculatedCost);
                return fallbackResult.Response;
            }

            _logger.LogError("❌ Fallback chain exhausted. Last error: {Error}",
                fallbackResult.LastException?.Message ?? "Unknown");
        }

        // PHASE 3: Try direct IChatClient resolution (backward compatibility)
        var directClient = _serviceProvider.GetService<IChatClient>();
        if (directClient != null)
        {
            _logger.LogWarning("🔄 No named clients configured, trying direct IChatClient resolution");

            try
            {
                var response = await directClient.GetResponseAsync(messageList, options, cancellationToken);

                return new ChatResponseWithCost
                {
                    Response = response,
                    ClientName = "direct",
                    InputTokens = (int)(response.Usage?.InputTokenCount ?? 0),
                    OutputTokens = (int)(response.Usage?.OutputTokenCount ?? 0),
                    CachedInputTokens = (int)(response.Usage?.TotalTokenCount - response.Usage?.InputTokenCount - response.Usage?.OutputTokenCount ?? 0),
                    CalculatedCost = CalculateCost(response, _defaultCostSettings)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Direct IChatClient resolution failed: {Error}", ex.Message);
            }
        }

        throw new InvalidOperationException("All chat clients (load balancing + fallback + direct) failed");
    }

    public async IAsyncEnumerable<ChatUpdateWithCost> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Try load balancing pool first
        if (_settings.ChatClientNames?.Count > 0)
        {
            var orderedClients = GetLoadBalancedOrder(_settings.ChatClientNames, _settings.LoadBalancingMode);

            foreach (var clientName in orderedClients)
            {
                bool success = false;
                Exception? lastError = null;

                _logger.LogDebug("🎯 Trying streaming from load balanced client: {Client}", clientName);

                await foreach (var update in TryStreamFromClientAsync(clientName, chatMessages, options, cancellationToken))
                {
                    if (update.IsError)
                    {
                        lastError = new Exception(update.ErrorMessage);
                        break;
                    }

                    yield return update;

                    if (update.IsComplete)
                    {
                        success = true;
                    }
                }

                if (success)
                {
                    _logger.LogInformation("✅ Streaming succeeded from client: {Client}", clientName);
                    yield break;
                }

                _logger.LogWarning("⚠️ Streaming failed from {Client}: {Error}", clientName, lastError?.Message ?? "Unknown");
            }
        }

        // Try fallback chain
        if (_settings.FallbackChatClientNames?.Count > 0)
        {
            var orderedClients = GetFallbackOrder(_settings.FallbackChatClientNames, _settings.FallbackMode);

            foreach (var clientName in orderedClients)
            {
                bool success = false;
                Exception? lastError = null;

                _logger.LogDebug("🔄 Trying streaming from fallback client: {Client}", clientName);

                await foreach (var update in TryStreamFromClientAsync(clientName, chatMessages, options, cancellationToken))
                {
                    if (update.IsError)
                    {
                        lastError = new Exception(update.ErrorMessage);
                        break;
                    }

                    yield return update;

                    if (update.IsComplete)
                    {
                        success = true;
                    }
                }

                if (success)
                {
                    _logger.LogInformation("✅ Fallback streaming succeeded from client: {Client}", clientName);
                    yield break;
                }

                _logger.LogWarning("⚠️ Fallback streaming failed from {Client}: {Error}", clientName, lastError?.Message ?? "Unknown");
            }
        }

        // Try direct IChatClient resolution (backward compatibility)
        var directClient = _serviceProvider.GetService<IChatClient>();
        if (directClient != null)
        {
            _logger.LogWarning("🔄 No named streaming clients configured, trying direct IChatClient resolution");

            await foreach (var update in directClient.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
            {
                var isComplete = update.FinishReason != null;
                yield return new ChatUpdateWithCost
                {
                    Update = update,
                    IsComplete = isComplete,
                    IsError = false
                };

                if (isComplete)
                {
                    yield break;
                }
            }
        }

        throw new InvalidOperationException("All streaming clients (load balancing + fallback + direct) failed");
    }

    /// <summary>
    /// Helper method to try streaming from a single client (handles exceptions).
    /// </summary>
    private async IAsyncEnumerable<ChatUpdateWithCost> TryStreamFromClientAsync(
        string clientName,
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IChatClient? client = null;
        TokenCostSettings? costSettings = null;
        bool initializationFailed = false;
        string? initializationError = null;

        try
        {
            client = GetOrCreateClient(clientName);
            costSettings = GetOrCreateCostSettings(clientName);
        }
        catch (Exception ex)
        {
            initializationFailed = true;
            initializationError = ex.Message;
        }

        if (initializationFailed)
        {
            yield return new ChatUpdateWithCost
            {
                IsError = true,
                ErrorMessage = $"Failed to initialize client: {initializationError}"
            };
            yield break;
        }

        await foreach (var update in client!.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            var isComplete = update.FinishReason != null;
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
    }

    #region Load Balancing Pool

    private async Task<ClientExecutionResult> TryLoadBalancingPoolAsync(
        PlayFrameworkSettings settings,
        ITransientErrorDetector errorDetector,
        IList<ChatMessage> chatMessages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        var orderedClients = GetLoadBalancedOrder(settings.ChatClientNames!, settings.LoadBalancingMode);

        foreach (var clientName in orderedClients)
        {
            _logger.LogDebug("🎯 Trying load balanced client: {Client}", clientName);

            var result = await ExecuteWithRetryAsync(
                clientName,
                settings.MaxRetryAttempts,
                settings.RetryBaseDelaySeconds,
                errorDetector,
                chatMessages,
                options,
                cancellationToken);

            if (result.Success)
            {
                return result;
            }

            lastException = result.LastException;

            // Non-transient error → skip to next client
            if (result.LastException != null && errorDetector.IsNonTransient(result.LastException))
            {
                _logger.LogWarning("❌ Non-transient error from {Client}, skipping: {Error}",
                    clientName, result.LastException.Message);
                continue;
            }
        }

        return new ClientExecutionResult { Success = false, LastException = lastException };
    }

    #endregion

    #region Fallback Chain

    private async Task<ClientExecutionResult> TryFallbackChainAsync(
        PlayFrameworkSettings settings,
        ITransientErrorDetector errorDetector,
        IList<ChatMessage> chatMessages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        var orderedClients = GetFallbackOrder(settings.FallbackChatClientNames!, settings.FallbackMode);

        foreach (var clientName in orderedClients)
        {
            _logger.LogDebug("🔄 Trying fallback client: {Client}", clientName);

            var result = await ExecuteWithRetryAsync(
                clientName,
                settings.MaxRetryAttempts,
                settings.RetryBaseDelaySeconds,
                errorDetector,
                chatMessages,
                options,
                cancellationToken);

            if (result.Success)
            {
                return result;
            }

            lastException = result.LastException;

            if (result.LastException != null && errorDetector.IsNonTransient(result.LastException))
            {
                _logger.LogWarning("❌ Non-transient error from fallback {Client}, skipping: {Error}",
                    clientName, result.LastException.Message);
                continue;
            }
        }

        return new ClientExecutionResult { Success = false, LastException = lastException };
    }

    #endregion

    #region Retry Logic

    private async Task<ClientExecutionResult> ExecuteWithRetryAsync(
        string clientName,
        int maxAttempts,
        double baseDelaySeconds,
        ITransientErrorDetector errorDetector,
        IList<ChatMessage> chatMessages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                _logger.LogDebug("🔄 Attempt {Attempt}/{MaxAttempts} for client {Client}",
                    attempt, maxAttempts, clientName);

                var client = GetOrCreateClient(clientName);
                var chatResponse = await client.GetResponseAsync(chatMessages, options, cancellationToken);

                // Calculate cost
                var costSettings = GetOrCreateCostSettings(clientName);
                var cost = CalculateCost(chatResponse, costSettings);

                var response = new ChatResponseWithCost
                {
                    Response = chatResponse,
                    CalculatedCost = cost,
                    InputTokens = (int)(chatResponse.Usage?.InputTokenCount ?? 0),
                    OutputTokens = (int)(chatResponse.Usage?.OutputTokenCount ?? 0),
                    CachedInputTokens = 0,
                    ClientName = clientName
                };

                _logger.LogInformation("✅ Client {Client} succeeded (Attempt {Attempt}, Tokens: {Input}→{Output}, Cost: {Cost:F6})",
                    clientName, attempt, response.InputTokens, response.OutputTokens, cost);

                return new ClientExecutionResult { Success = true, Response = response };
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (errorDetector.IsNonTransient(ex))
                {
                    _logger.LogError("❌ Non-transient error from {Client}: {Error}", clientName, ex.Message);
                    break; // Don't retry
                }

                if (attempt < maxAttempts)
                {
                    var delaySeconds = baseDelaySeconds * Math.Pow(2, attempt - 1);
                    _logger.LogWarning("⚠️ Transient error from {Client} (Attempt {Attempt}/{MaxAttempts}), retrying in {Delay}s: {Error}",
                        clientName, attempt, maxAttempts, delaySeconds, ex.Message);

                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }
                else
                {
                    _logger.LogError("❌ Client {Client} failed after {Attempts} attempts: {Error}",
                        clientName, maxAttempts, ex.Message);
                }
            }
        }

        return new ClientExecutionResult { Success = false, LastException = lastException };
    }

    #endregion

    #region Load Balancing Logic

    private List<string> GetLoadBalancedOrder(List<string> clients, LoadBalancingMode mode)
    {
        return mode switch
        {
            LoadBalancingMode.RoundRobin => GetRoundRobinOrder(clients),
            LoadBalancingMode.Random => GetRandomOrder(clients),
            LoadBalancingMode.Sequential => clients.ToList(),
            LoadBalancingMode.None => clients.Take(1).ToList(),
            _ => clients.ToList()
        };
    }

    private List<string> GetRoundRobinOrder(List<string> clients)
    {
        var index = Interlocked.Increment(ref _roundRobinIndex) % clients.Count;
        return clients.Skip(index).Concat(clients.Take(index)).ToList();
    }

    private List<string> GetRandomOrder(List<string> clients)
    {
        var shuffled = clients.ToList();
        lock (_random) // Lock for thread safety
        {
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
        }
        return shuffled;
    }

    private List<string> GetFallbackOrder(List<string> clients, FallbackMode mode)
    {
        return mode switch
        {
            FallbackMode.RoundRobin => GetRoundRobinOrder(clients),
            FallbackMode.Random => GetRandomOrder(clients),
            _ => clients.ToList() // Sequential
        };
    }

    #endregion

    #region Lazy Initialization & Cost Calculation

    private IChatClient GetOrCreateClient(string name)
    {
        return _clientCache.GetOrAdd(name, _ => new Lazy<IChatClient>(() =>
        {
            var client = _chatClientFactory.Create(name);
            if (client == null)
            {
                throw new InvalidOperationException($"IChatClient '{name}' not found in factory");
            }
            _logger.LogDebug("📦 Lazy-loaded chat client: {ClientName}", name);
            return client;
        })).Value;
    }

    private TokenCostSettings? GetOrCreateCostSettings(string name)
    {
        return _costSettingsCache.GetOrAdd(name, _ => new Lazy<TokenCostSettings?>(() =>
        {
            var settings = _costSettingsFactory.Create(name);
            if (settings != null)
            {
                _logger.LogDebug("📦 Lazy-loaded cost settings for: {ClientName} (Input: ${Input}/1K, Output: ${Output}/1K)",
                    name, settings.InputTokenCostPer1K, settings.OutputTokenCostPer1K);
            }
            return settings;
        })).Value;
    }

    private decimal CalculateCost(ChatResponse response, TokenCostSettings? settings)
    {
        if (settings == null || response.Usage == null)
            return 0m;

        var inputCost = ((decimal)(response.Usage.InputTokenCount ?? 0) / 1_000m) * settings.InputTokenCostPer1K;
        var outputCost = ((decimal)(response.Usage.OutputTokenCount ?? 0) / 1_000m) * settings.OutputTokenCostPer1K;

        return inputCost + outputCost;
    }

    #endregion

    /// <summary>
    /// Result of client execution attempt.
    /// </summary>
    private sealed class ClientExecutionResult
    {
        public bool Success { get; init; }
        public ChatResponseWithCost? Response { get; init; }
        public Exception? LastException { get; init; }
    }
}
