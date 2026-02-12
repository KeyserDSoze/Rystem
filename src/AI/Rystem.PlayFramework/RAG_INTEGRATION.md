# RAG Integration Guide

Complete guide to using RAG (Retrieval-Augmented Generation) in PlayFramework with automatic cost tracking.

---

## 📦 Installation

RAG support is included in `Rystem.PlayFramework`. No additional packages required.

---

## 🚀 Quick Start

### 1. Implement IRagService

```csharp
public class AzureAISearchRagService : IRagService
{
    private readonly OpenAIClient _embeddingClient;
    private readonly SearchClient _searchClient;

    public AzureAISearchRagService(OpenAIClient embeddingClient, SearchClient searchClient)
    {
        _embeddingClient = embeddingClient;
        _searchClient = searchClient;
    }

    public async Task<RagResult> SearchAsync(RagRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // 1. Generate embedding for query
        var embeddingResponse = await _embeddingClient.GetEmbeddingsAsync(
            deploymentName: "text-embedding-ada-002",
            new EmbeddingsOptions(request.Query),
            cancellationToken: ct
        );

        var embedding = embeddingResponse.Value.Data[0].Embedding;
        int embeddingTokens = embeddingResponse.Value.Usage.TotalTokens;

        // 2. Search for similar documents
        var searchOptions = new SearchOptions
        {
            VectorSearch = new()
            {
                Queries = { new VectorizedQuery(embedding.ToArray()) 
                { 
                    KNearestNeighborsCount = request.Settings.TopK 
                }}
            },
            Size = request.Settings.TopK
        };

        var searchResults = await _searchClient.SearchAsync<SearchDocument>(
            searchText: null,
            searchOptions,
            cancellationToken: ct
        );

        // 3. Convert to RagDocument
        var documents = new List<RagDocument>();
        await foreach (var result in searchResults.Value.GetResultsAsync())
        {
            documents.Add(new RagDocument
            {
                Content = result.Document["content"]?.ToString() ?? "",
                Title = result.Document["title"]?.ToString(),
                Source = result.Document["source"]?.ToString(),
                Score = result.Score ?? 0,
                Metadata = result.Document.ToDictionary(x => x.Key, x => x.Value)
            });
        }

        sw.Stop();

        // 4. Return result with token usage
        // NOTE: Cost will be calculated automatically by PlayFramework using RagCostSettings
        return new RagResult
        {
            Documents = documents,
            DurationMs = sw.Elapsed.TotalMilliseconds,
            ExecutedQuery = request.Query,
            TotalCount = documents.Count,
            
            // Provide token usage - PlayFramework will calculate cost
            TokenUsage = new RagTokenUsage
            {
                EmbeddingTokens = embeddingTokens,
                SearchTokens = 0  // Vector search doesn't consume tokens
            }
        };
    }
}
```

### 2. Register RAG Service with Cost Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register RAG service with cost settings BEFORE factory key
builder.Services.AddRagService<AzureAISearchRagService>(
    configureCost: settings =>
    {
        // OpenAI text-embedding-ada-002 pricing
        settings.CostPerThousandEmbeddingTokens = 0.0001m;
        settings.CostPerThousandSearchTokens = 0m;       // Vector search is free
        settings.FixedCostPerSearch = 0m;                // No per-query fees
    },
    name: "azure"  // Factory key
);

// Or use default cost (no configuration needed)
builder.Services.AddRagService<CustomRagService>(name: "custom");
```

### 3. Configure RAG in PlayFramework

#### **Option A: Global RAG (all scenes)**

```csharp
builder.Services.AddPlayFramework(frameworkBuilder =>
{
    frameworkBuilder
        .WithRag(settings =>
        {
            settings.TopK = 5;
            settings.SearchAlgorithm = VectorSearchAlgorithm.CosineSimilarity;
            settings.MinimumScore = 0.7;
        }, "azure");  // Use "azure" factory key
});
```

#### **Option B: Per-Scene RAG**

```csharp
builder.Services.AddPlayFramework(frameworkBuilder =>
{
    frameworkBuilder.AddScene("CustomerSupport", scene =>
    {
        scene
            .WithSystemMessage("You are a helpful customer support agent.")
            .WithRag(settings =>
            {
                settings.TopK = 10;  // More documents for support
                settings.MinimumScore = 0.6;
            }, "azure");
    });

    frameworkBuilder.AddScene("ProductInfo", scene =>
    {
        scene
            .WithSystemMessage("You provide product information.")
            .WithRag(settings =>
            {
                settings.TopK = 3;  // Fewer, more relevant documents
            }, "azure");
    });

    // Scene without RAG
    frameworkBuilder.AddScene("Greeting", scene =>
    {
        scene.WithSystemMessage("You greet users warmly.");
        // No RAG configured
    });
});
```

### 4. Use RAG Tool

The `search_knowledge_base` tool is automatically available to LLMs in scenes with RAG configured:

```csharp
var response = await playFramework.ExecuteSceneAsync("CustomerSupport", new()
{
    UserMessage = "How do I reset my password?",
    ExecutionMode = SceneExecutionMode.Direct
});

// LLM will automatically call search_knowledge_base("reset password") if needed
// Cost includes: LLM tokens + RAG embedding tokens
Console.WriteLine($"Total Cost: ${response.TotalCost:F6}");
```

---

## 💰 Cost Tracking

### Automatic Cost Calculation

PlayFramework automatically tracks RAG costs:

1. **If IRagService returns `Cost`**: Uses that value
2. **If IRagService returns `TokenUsage` but not `Cost`**: Calculates cost using `RagCostSettings`
3. **If neither**: Cost is $0

### Cost Configuration Examples

#### **OpenAI Pricing**

```csharp
services.AddRagService<OpenAIRagService>(settings =>
{
    // text-embedding-ada-002
    settings.CostPerThousandEmbeddingTokens = 0.0001m;

    // text-embedding-3-small (cheaper!)
    // settings.CostPerThousandEmbeddingTokens = 0.00002m;

    // text-embedding-3-large (more expensive)
    // settings.CostPerThousandEmbeddingTokens = 0.00013m;
}, "openai");
```

#### **Pinecone (per-query cost)**

```csharp
services.AddRagService<PineconeRagService>(settings =>
{
    settings.CostPerThousandEmbeddingTokens = 0.0001m;  // OpenAI embedding
    settings.FixedCostPerSearch = 0.0001m;              // Pinecone query cost
}, "pinecone");
```

#### **Custom Provider**

```csharp
services.AddRagService<CustomRagService>(settings =>
{
    settings.CostPerThousandEmbeddingTokens = 0.00005m;
    settings.CostPerThousandSearchTokens = 0.00001m;    // Hybrid search with reranking
    settings.FixedCostPerSearch = 0.0002m;
}, "custom");
```

### Cost is Automatically Included in Scene Total

```csharp
var response = await playFramework.ExecuteSceneAsync("CustomerSupport", new()
{
    UserMessage = "How does your product work?",
    ExecutionMode = SceneExecutionMode.Direct
});

// TotalCost includes:
// - LLM prompt tokens
// - LLM completion tokens
// - RAG embedding tokens (automatically calculated)
// - RAG search costs (if configured)
Console.WriteLine($"LLM Cost: ${response.Usage?.Cost ?? 0:F6}");
Console.WriteLine($"Total Cost (LLM + RAG + Tools): ${response.TotalCost:F6}");
```

---

## 📊 Telemetry & Observability

All RAG operations are automatically tracked with OpenTelemetry:

### **Activity Tags (Tracing)**

```csharp
// Jaeger traces include:
rag.factory_key = "azure"
rag.top_k = 5
rag.algorithm = "CosineSimilarity"
rag.documents_found = 3
rag.duration_ms = 234.5
rag.tokens.embedding = 150
rag.tokens.search = 0
rag.tokens.total = 150
rag.cost = 0.000015
```

### **Prometheus Metrics**

```promql
# Total RAG searches
playframework_rag_searches_total

# RAG tokens consumed
playframework_rag_tokens_total{rag_provider="azure"}

# RAG cost per hour
rate(playframework_rag_cost_sum[1h]) * 3600

# RAG search duration (p95)
histogram_quantile(0.95, rate(playframework_rag_duration_bucket[5m]))
```

### **Grafana Dashboard**

The cost-tracking dashboard includes RAG metrics:

- **RAG Cost per Hour** (separate from LLM)
- **RAG Token Usage** (by provider)
- **RAG Cost by Provider** (compare Azure vs Pinecone)
- **RAG Search Latency** (p50, p95, p99)

Start Grafana stack:
```bash
cd src/AI/Rystem.PlayFramework/observability
docker-compose up -d
```

Access dashboards: http://localhost:3000 (admin/admin)

---

## 🔧 Advanced Scenarios

### Multiple RAG Providers

```csharp
// Register multiple providers
services
    .AddRagService<AzureAISearchRagService>(settings =>
    {
        settings.CostPerThousandEmbeddingTokens = 0.0001m;
    }, "azure")
    .AddRagService<PineconeRagService>(settings =>
    {
        settings.CostPerThousandEmbeddingTokens = 0.0001m;
        settings.FixedCostPerSearch = 0.0001m;
    }, "pinecone")
    .AddRagService<CustomRagService>(settings =>
    {
        settings.CostPerThousandEmbeddingTokens = 0.00005m;
    }, "custom");

// Use different providers for different scenes
frameworkBuilder
    .AddScene("CustomerSupport", scene => scene.WithRag(s => s.TopK = 10, "azure"))
    .AddScene("ProductInfo", scene => scene.WithRag(s => s.TopK = 5, "pinecone"))
    .AddScene("KnowledgeBase", scene => scene.WithRag(s => s.TopK = 20, "custom"));
```

### Override RAG Settings Per Request

```csharp
var response = await playFramework.ExecuteSceneAsync("CustomerSupport", new()
{
    UserMessage = "Complex question requiring more context",
    ExecutionMode = SceneExecutionMode.Direct,
    // Override TopK for this request only
    Metadata = new Dictionary<string, object>
    {
        { "RagSettings", new RagSettings { TopK = 20 } }
    }
});
```

### Disable RAG for Specific Scene

```csharp
frameworkBuilder
    .WithRag(settings => settings.TopK = 5, "azure")  // Global RAG
    .AddScene("SimpleGreeting", scene =>
    {
        scene
            .WithSystemMessage("You greet users.")
            .WithoutRag("azure");  // Disable RAG for this scene
    });
```

### Custom Cost Calculation

If you need more sophisticated cost calculation (e.g., tiered pricing):

```csharp
public class TieredCostRagService : IRagService
{
    public async Task<RagResult> SearchAsync(RagRequest request, CancellationToken ct)
    {
        // ... perform search ...

        // Calculate cost with custom logic
        decimal cost = tokenUsage.Total switch
        {
            < 1000 => tokenUsage.Total * 0.0001m,      // First 1K tokens
            < 10000 => 0.1m + (tokenUsage.Total - 1000) * 0.00008m,  // Tiered
            _ => 0.82m + (tokenUsage.Total - 10000) * 0.00005m
        };

        return new RagResult
        {
            Documents = documents,
            TokenUsage = tokenUsage,
            Cost = cost  // PlayFramework will use this instead of calculating
        };
    }
}
```

---

## 🧪 Testing

### Mock RAG Service for Testing

```csharp
public class MockRagService : IRagService
{
    public Task<RagResult> SearchAsync(RagRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(new RagResult
        {
            Documents = new List<RagDocument>
            {
                new() { Content = "Mock document 1", Score = 0.95 },
                new() { Content = "Mock document 2", Score = 0.87 }
            },
            DurationMs = 50,
            TokenUsage = new RagTokenUsage { EmbeddingTokens = 100 },
            // Cost will be calculated by RagCostSettings (or return explicit cost)
        });
    }
}

// In test setup
services.AddRagService<MockRagService>(settings =>
{
    settings.CostPerThousandEmbeddingTokens = 0.0001m;
});
```

### Verify Cost Calculation

```csharp
[Fact]
public async Task RagCost_IsIncludedInSceneTotal()
{
    var response = await _playFramework.ExecuteSceneAsync("TestScene", new()
    {
        UserMessage = "Test query"
    });

    // Verify RAG cost is included
    Assert.True(response.TotalCost > 0);
    
    // Cost should be: (100 tokens / 1000) * 0.0001 = $0.00001
    // Plus LLM cost
    Assert.InRange(response.TotalCost, 0.00001m, 1m);
}
```

---

## 📚 API Reference

### **IRagService**

```csharp
public interface IRagService
{
    Task<RagResult> SearchAsync(RagRequest request, CancellationToken cancellationToken = default);
}
```

### **RagCostSettings**

```csharp
public sealed class RagCostSettings
{
    decimal CostPerThousandEmbeddingTokens { get; set; }  // Default: 0.0001
    decimal CostPerThousandSearchTokens { get; set; }      // Default: 0
    decimal FixedCostPerSearch { get; set; }               // Default: 0
    
    decimal CalculateCost(RagTokenUsage? tokenUsage);
}
```

### **Registration Methods**

```csharp
// Register with cost configuration (settings BEFORE name)
IServiceCollection.AddRagService<TService>(
    Action<RagCostSettings>? configureCost = null,
    AnyOf<string?, Enum>? name = null
)

// Configure global RAG
PlayFrameworkBuilder.WithRag(
    Action<RagSettings> configure,
    AnyOf<string?, Enum>? name = null
)

// Configure per-scene RAG
SceneBuilder.WithRag(
    Action<RagSettings> configure,
    AnyOf<string?, Enum>? name = null
)

// Disable per-scene RAG
SceneBuilder.WithoutRag(AnyOf<string?, Enum>? name = null)
```

---

## ❓ FAQ

### **Q: Do I need to calculate cost in my IRagService?**

**A:** No! Just return `TokenUsage`, and PlayFramework will calculate cost using `RagCostSettings`. Only return `Cost` if you need custom logic.

### **Q: Is RAG cost included in the scene's TotalCost?**

**A:** Yes! RAG costs are automatically added to `SceneContext.TotalCost` and returned in `AiSceneResponse.TotalCost`.

### **Q: Can I use multiple RAG providers in the same application?**

**A:** Yes! Register each with a different factory key and specify which one to use per scene via `WithRag(settings, "key")`.

### **Q: What if I don't configure RagCostSettings?**

**A:** Cost will be $0 unless your `IRagService` explicitly returns `Cost`. Token tracking still works.

### **Q: How do I track RAG costs separately from LLM costs?**

**A:** Use Prometheus queries:
```promql
# LLM cost
rate(playframework_cost_per_execution_sum[1h]) * 3600

# RAG cost
rate(playframework_rag_cost_sum[1h]) * 3600
```

---

## 🔗 Related Documentation

- [Repository Pattern](https://rystem.net/mcp/tools/repository-setup.md)
- [OpenTelemetry Integration](../TELEMETRY.md)
- [Grafana Dashboards](../observability/grafana/README.md)
- [Cost Tracking](../BUDGET_LIMIT.md)

---

## 🆘 Support

- **GitHub**: https://github.com/KeyserDSoze/Rystem
- **Documentation**: https://rystem.net
- **Issues**: https://github.com/KeyserDSoze/Rystem/issues
