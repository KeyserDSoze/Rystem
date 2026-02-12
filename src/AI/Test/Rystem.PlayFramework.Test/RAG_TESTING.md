# RAG Testing Documentation

Comprehensive test suite for RAG (Retrieval-Augmented Generation) integration in PlayFramework.

---

## 📁 Test Files

### **1. MockRagService.cs** (`Infrastructure/`)
Mock implementation of `IRagService` with realistic fake knowledge base.

**Features**:
- **12 realistic documents** covering:
  - Customer Support (password reset, security, billing)
  - Product Documentation (getting started, API integration)
  - Technical Documentation (database, performance)
  - Troubleshooting (error codes, network issues)
- **Simulated search** using keyword matching (mirrors vector similarity)
- **Configurable token usage** (150 tokens per query, simulating text-embedding-ada-002)
- **Score calculation** based on keyword matches

**Knowledge Base Categories**:
```
Authentication  → Password reset, 2FA, security
Billing         → Subscriptions, refunds, pricing
API             → Integration, authentication, webhooks
Architecture    → Database schema, performance tips
Troubleshooting → Error codes, connection issues
Tutorial        → Getting started, onboarding
```

---

## 🧪 Test Suites

### **RagIntegrationTests.cs** (18 tests)

#### **Basic Functionality**
1. ✅ `RagTool_CustomerSupport_ReturnsRelevantDocuments`
   - Verifies RAG returns relevant documents for password reset query
   - Checks cost tracking ($0.000015 for 150 tokens)

2. ✅ `RagTool_TechnicalDocs_ReturnsMoreDocuments`
   - Tests TopK=10 configuration (vs TopK=3 for CustomerSupport)
   - Verifies scene-specific settings override global

3. ✅ `RagTool_ApiSupport_FiltersRelevantCategory`
   - Tests custom settings (filter by category)
   - Verifies CustomSettings dictionary usage

4. ✅ `RagTool_Greeting_DoesNotUseRag`
   - Tests `WithoutRag()` disabling RAG for specific scene
   - Verifies cost is LLM-only (no RAG cost)

#### **Search Quality**
5. ✅ `RagTool_BillingQuery_FindsMultipleRelevantDocs`
   - Multi-topic query ("refund policy and subscription pricing")
   - Tests retrieval of documents from same category

6. ✅ `RagTool_LowQualityQuery_FiltersLowScores`
   - Gibberish query ("xyz abc qwerty")
   - Verifies MinimumScore filtering

7. ✅ `RagTool_ComplexQuery_CombinesMultipleTopics`
   - Query spanning multiple topics
   - Tests cross-category document retrieval

#### **Cost Tracking**
8. ✅ `RagCost_IsCalculatedAutomatically`
   - Verifies cost calculation: (150 tokens / 1000) × $0.0001 = $0.000015
   - Tests integration with `RagCostSettings`

9. ✅ `RagTool_MultipleQueries_AccumulatesCost`
   - 3 queries, total cost ≥ $0.000045
   - Verifies cost accumulation across requests

#### **Configuration**
10. ✅ `RagTool_TopK_RespectsConfiguration` (Theory: 3 cases)
    - CustomerSupport: TopK=3
    - TechnicalDocs: TopK=10
    - ApiSupport: TopK=5

11. ✅ `RagTool_MinimumScore_FiltersLowQualityMatches`
    - CustomerSupport: MinScore=0.5 (strict)
    - TechnicalDocs: MinScore=0.2 (permissive)

#### **Performance**
12. ✅ `RagTool_PerformanceMonitoring_TracksDuration`
    - Tracks execution time
    - Verifies <5 seconds for mock (reasonable performance)

13. ✅ `RagTool_EmptyKnowledgeBase_HandlesGracefully`
    - Query with no matches
    - Verifies error handling (no throw, graceful response)

#### **Streaming**
14. ✅ `RagTool_StreamingMode_WorksWithRag`
    - Tests RAG with streaming responses
    - Verifies chunk accumulation

#### **Cost Comparison**
15. ✅ `RagCostSettings_DifferentProviders_DifferentCosts`
    - Demonstrates cost differences:
      - OpenAI ada-002: $0.0001/1K
      - OpenAI 3-small: $0.00002/1K (5× cheaper)
      - OpenAI 3-large: $0.00013/1K (1.3× more expensive)
      - Pinecone: $0.0001/1K + $0.0001 per query

---

### **RagAdvancedTests.cs** (13 tests)

#### **Multi-Turn Conversations**
1. ✅ `MultiTurn_Conversation_MaintainsContext`
   - 3-turn customer support conversation
   - Tracks cumulative cost across turns
   - Verifies context-aware responses

#### **Budget Management**
2. ✅ `BudgetLimit_StopsWhenExceeded`
   - Sets budget ($0.001)
   - Executes requests until budget exhausted
   - Tracks requests processed and average cost

3. ✅ `CostComparison_BudgetVsComprehensive`
   - BudgetAssistant: TopK=2, MinScore=0.6 (cheap)
   - ResearchAssistant: TopK=15, MinScore=0.2 (expensive)
   - Demonstrates cost/quality tradeoff

#### **Caching**
4. ✅ `RagCache_ReducesCostForRepeatedQueries`
   - Same query executed twice
   - Verifies cache savings (in real scenario)

#### **Complex Workflows**
5. ✅ `ComplexWorkflow_MultiSceneWithRag`
   - 4 queries across 3 different scenes
   - Cost breakdown by scene
   - Total workflow cost analysis

#### **Performance**
6. ✅ `RagPerformance_ParallelQueries`
   - 5 queries executed in parallel
   - Tracks total time and per-query average
   - Verifies <10 seconds for mock

#### **Configuration Impact**
7. ✅ `RagConfiguration_ImpactOnQuality` (Theory: 3 cases)
   - TopK=2, MinScore=0.6 (budget)
   - TopK=5, MinScore=0.4 (balanced)
   - TopK=15, MinScore=0.2 (comprehensive)
   - Demonstrates quality vs cost tradeoff

#### **Telemetry**
8. ✅ `RagTelemetry_TracksMetrics`
   - 3 queries with cost and duration tracking
   - Calculates averages
   - Verifies metrics are recorded

#### **Error Handling**
9. ✅ `ErrorHandling_RagServiceUnavailable`
   - Simulates RAG service failure
   - Verifies graceful degradation (LLM still responds)

---

## 📊 Test Coverage

### **Functional Coverage**
- ✅ Global RAG configuration
- ✅ Per-scene RAG configuration
- ✅ RAG disabling (`WithoutRag`)
- ✅ TopK configuration (2, 3, 5, 10, 15)
- ✅ MinimumScore filtering (0.2, 0.4, 0.5, 0.6)
- ✅ Custom settings (filter by category)
- ✅ Streaming mode integration
- ✅ Multi-turn conversations

### **Cost Tracking Coverage**
- ✅ Automatic cost calculation
- ✅ Token usage tracking
- ✅ Cost accumulation across requests
- ✅ Budget limit enforcement
- ✅ Cost comparison (budget vs comprehensive)
- ✅ Provider cost differences

### **Performance Coverage**
- ✅ Execution duration tracking
- ✅ Parallel query execution
- ✅ Cache impact on cost

### **Error Handling Coverage**
- ✅ Empty search results
- ✅ Low-quality queries
- ✅ RAG service unavailable

---

## 🎯 Test Scenarios

### **Scenario 1: Customer Support Agent**
```csharp
Scene: CustomerSupport
TopK: 3 (focused)
MinScore: 0.5 (high quality)
Use Case: Quick, accurate answers
Cost: ~$0.000015 per query
```

**Example Queries**:
- "How do I reset my password?"
- "What is your refund policy?"
- "How do I cancel my subscription?"

### **Scenario 2: Technical Documentation Assistant**
```csharp
Scene: TechnicalDocs
TopK: 10 (comprehensive)
MinScore: 0.2 (include marginal matches)
Use Case: Detailed technical information
Cost: ~$0.000015 per query (RAG) + higher LLM cost (more context)
```

**Example Queries**:
- "How do I authenticate API requests?"
- "Explain webhook configuration"
- "What are common error codes?"

### **Scenario 3: Budget-Conscious Assistant**
```csharp
Scene: BudgetAssistant
TopK: 2 (minimal)
MinScore: 0.6 (very strict)
Use Case: Cost optimization
Cost: ~$0.000015 per query (RAG) + minimal LLM cost
```

**Example Queries**:
- Quick factual questions
- Simple clarifications

### **Scenario 4: Research Assistant**
```csharp
Scene: ResearchAssistant
TopK: 15 (maximum)
MinScore: 0.2 (permissive)
Use Case: Comprehensive research
Cost: ~$0.000015 per query (RAG) + high LLM cost (max context)
```

**Example Queries**:
- Complex multi-topic questions
- Research requiring multiple sources

---

## 💰 Cost Analysis

### **RAG Cost Breakdown**
```
Embedding Generation: $0.0001 per 1K tokens (OpenAI ada-002)
Vector Search: $0 (no additional cost)
Fixed Cost: $0

Example Query (150 tokens):
  RAG Cost = (150 / 1000) × $0.0001 = $0.000015
```

### **Total Cost Comparison**
```
Budget Config (TopK=2):
  RAG: $0.000015
  LLM: ~$0.001 (minimal context)
  Total: ~$0.001015

Comprehensive Config (TopK=15):
  RAG: $0.000015
  LLM: ~$0.005 (max context = more tokens)
  Total: ~$0.005015
```

### **Cost Optimization Strategies**
1. **Reduce TopK**: Fewer documents = less LLM context = lower cost
2. **Increase MinScore**: Higher threshold = fewer documents
3. **Cache results**: Repeated queries cost $0 (cache hit)
4. **Choose cheaper embeddings**: text-embedding-3-small (5× cheaper)

---

## 🚀 Running Tests

### **Run All RAG Tests**
```bash
dotnet test --filter "FullyQualifiedName~Rag"
```

### **Run Integration Tests Only**
```bash
dotnet test --filter "FullyQualifiedName~RagIntegrationTests"
```

### **Run Advanced Tests Only**
```bash
dotnet test --filter "FullyQualifiedName~RagAdvancedTests"
```

### **Run Specific Test**
```bash
dotnet test --filter "FullyQualifiedName~MultiTurn_Conversation"
```

---

## 📈 Expected Results

### **Success Criteria**
- ✅ All 31 tests pass (18 + 13)
- ✅ Cost tracking accurate (±0.000001)
- ✅ Execution time reasonable (<5s per test)
- ✅ No exceptions thrown

### **Performance Benchmarks**
- Single query: <100ms (mock)
- Multi-turn (3 queries): <300ms
- Parallel (5 queries): <500ms
- Complex workflow (4 queries): <400ms

### **Cost Benchmarks**
- Single query: $0.000015 (RAG only)
- Multi-turn (3 queries): ≥$0.000045
- Complex workflow (4 queries): Varies by scene config

---

## 🔍 Test Output Examples

### **Multi-Turn Conversation**
```
Q: How do I reset my password?
A: To reset your password, click on 'Forgot Password'...
Cost: $0.001234

Q: What if I don't receive the email?
A: Check your spam folder...
Cost: $0.001189

Q: How long is the reset link valid?
A: The link expires in 24 hours...
Cost: $0.001156

Total Conversation Cost: $0.003579
Average Cost per Turn: $0.001193
```

### **Cost Comparison**
```
=== BUDGET ASSISTANT (TopK=2, MinScore=0.6) ===
Quick answer with 2 documents...
Cost: $0.001023

=== RESEARCH ASSISTANT (TopK=15, MinScore=0.2) ===
Comprehensive answer with 15 documents...
Cost: $0.005678

=== COST COMPARISON ===
Budget: $0.001023
Comprehensive: $0.005678
Difference: $0.004655 (5.6× more expensive)
```

---

## 🐛 Known Limitations

1. **Mock Search**: Uses keyword matching, not true vector similarity
2. **Fixed Token Count**: MockRagService always returns 150 tokens
3. **No Actual Embeddings**: Real implementations use OpenAI/Azure APIs
4. **In-Memory Knowledge Base**: 12 documents vs thousands in production

---

## 🔗 Related Documentation

- [RAG Integration Guide](../../../Rystem.PlayFramework/RAG_INTEGRATION.md)
- [Cost Tracking](../../../Rystem.PlayFramework/BUDGET_LIMIT.md)
- [Telemetry](../../../Rystem.PlayFramework/TELEMETRY.md)
- [Test Base Classes](./README_TESTING.md)

---

## 📝 Adding New Tests

### **Template for RAG Test**
```csharp
[Fact]
public async Task MyNewRagTest()
{
    // Arrange
    var request = new SceneRequestSettings
    {
        UserMessage = "Your query here",
        ExecutionMode = SceneExecutionMode.Direct
    };

    // Act
    var response = await PlayFramework.ExecuteSceneAsync("SceneName", request);

    // Assert
    Assert.True(response.Success);
    Assert.Contains("expected keyword", response.Response, StringComparison.OrdinalIgnoreCase);
    Assert.True(response.TotalCost > 0); // Verify cost tracking
    
    Output.WriteLine($"Cost: ${response.TotalCost:F6}");
}
```

---

## ✅ Test Checklist

When adding RAG tests, verify:
- [ ] Cost is calculated correctly
- [ ] Token usage is tracked
- [ ] RAG settings are respected (TopK, MinScore)
- [ ] Scene-specific overrides work
- [ ] Telemetry is recorded
- [ ] Error handling is graceful
- [ ] Performance is reasonable
- [ ] Documentation is updated
