# PlayFramework OpenTelemetry Integration

Complete observability for PlayFramework using OpenTelemetry distributed tracing and metrics.

## 📊 Overview

PlayFramework includes built-in support for **OpenTelemetry**, providing:

✅ **Distributed Tracing** - See exactly what happens during scene execution  
✅ **Metrics Collection** - Monitor performance, token usage, and costs  
✅ **Correlation** - Automatic linking between logs, traces, and metrics  
✅ **Production Ready** - Configurable sampling, attribute limits, PII controls  

---

## 🚀 Quick Start

### 1. Enable Telemetry

```csharp
services.AddPlayFramework(builder =>
{
    builder.WithTelemetry(telemetry =>
    {
        telemetry.EnableTracing = true;
        telemetry.EnableMetrics = true;
        telemetry.TraceScenes = true;
        telemetry.TraceTools = true;
        telemetry.TraceLlmCalls = true;
    });
    
    builder.AddScene(scene => scene.WithName("Demo"));
});
```

### 2. Configure OpenTelemetry Exporters

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService("MyApp")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = "production"
                });
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(PlayFrameworkActivitySource.SourceName)
            .AddSource("Microsoft.Extensions.AI") // LLM calls
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://jaeger:4317");
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(PlayFrameworkMetrics.MeterName)
            .AddPrometheusExporter();  // Expose /metrics endpoint
    });
```

### 3. Run and Observe

```csharp
var sceneManager = serviceProvider
    .GetRequiredService<IFactory<ISceneManager>>()
    .Create(null);

await foreach (var response in sceneManager.ExecuteAsync("Hello!"))
{
    Console.WriteLine(response.Message);
}
```

**View traces in Jaeger**: http://localhost:16686  
**View metrics in Prometheus**: http://localhost:9090  

---

## 📈 What Gets Traced

### Activity Hierarchy

```
SceneManager.Execute (5.2s)
├─ Scene.Resolve (0.1s)
├─ Scene.Execute (4.9s)
│  ├─ Cache.Get (0.05s)
│  ├─ LLM.Call (2.3s)
│  ├─ Tool.Execute (0.8s)
│  ├─ LLM.Call (1.5s)
│  └─ Cache.Set (0.03s)
└─ [Completed]
```

### Standard Tags

Every activity includes:
- `playframework.factory_name`
- `playframework.scene.name`
- `playframework.scene.mode`
- `playframework.llm.tokens.total`
- `playframework.cost.total`

### Standard Events

- `scene.started`
- `scene.completed`
- `scene.failed`
- `tool.called`
- `cache.accessed`

---

## 📊 Metrics Collected

### Counters

| Metric | Description |
|--------|-------------|
| `playframework.scene.executions` | Total scene executions |
| `playframework.tool.calls` | Total tool calls |
| `playframework.cache.hits` | Cache hits |
| `playframework.cache.misses` | Cache misses |
| `playframework.llm.calls` | LLM API calls |
| `playframework.llm.tokens.total` | Total tokens consumed |

### Histograms

| Metric | Unit | Description |
|--------|------|-------------|
| `playframework.scene.duration` | ms | Scene execution time |
| `playframework.tool.duration` | ms | Tool execution time |
| `playframework.llm.duration` | ms | LLM call duration |
| `playframework.llm.tokens.per_request` | count | Tokens per request |
| `playframework.cost.per_execution` | USD | Cost per scene |

### Gauges

| Metric | Description |
|--------|-------------|
| `playframework.scene.active` | Currently executing scenes |
| `playframework.llm.active` | Active LLM calls |

---

## ⚙️ Configuration Options

### Telemetry Settings

```csharp
builder.WithTelemetry(telemetry =>
{
    // === TRACING ===
    telemetry.EnableTracing = true;          // Enable distributed tracing
    telemetry.TraceScenes = true;            // Trace scene executions
    telemetry.TraceTools = true;             // Trace tool calls
    telemetry.TraceLlmCalls = true;          // Trace LLM API calls
    telemetry.TraceCacheOperations = true;   // Trace cache get/set
    telemetry.TracePlanning = true;          // Trace planner (if enabled)
    telemetry.TraceSummarization = true;     // Trace summarizer (if enabled)
    telemetry.TraceDirector = true;          // Trace director (if enabled)
    telemetry.TraceMcpOperations = true;     // Trace MCP servers (if used)
    
    // === METRICS ===
    telemetry.EnableMetrics = true;          // Enable metrics collection
    
    // === PRIVACY ===
    telemetry.IncludeLlmPrompts = false;     // ⚠️ Include full prompts (PII risk)
    telemetry.IncludeLlmResponses = false;   // ⚠️ Include full responses (PII risk)
    telemetry.MaxAttributeLength = 1000;     // Truncate long strings
    
    // === SAMPLING ===
    telemetry.SamplingRate = 1.0;            // 1.0 = 100%, 0.1 = 10%
    
    // === CUSTOM TAGS ===
    telemetry.CustomAttributes = new()
    {
        ["deployment.environment"] = "production",
        ["deployment.region"] = "us-east-1",
        ["service.version"] = "1.0.0",
        ["tenant.id"] = "customer-123"
    };
});
```

### Sampling Strategies

```csharp
// Development: 100% sampling
telemetry.SamplingRate = 1.0;

// Staging: 50% sampling
telemetry.SamplingRate = 0.5;

// Production: 10% sampling (high traffic)
telemetry.SamplingRate = 0.1;

// Production: 1% sampling (very high traffic)
telemetry.SamplingRate = 0.01;
```

### Disable Telemetry (Testing)

```csharp
builder.WithoutTelemetry();  // Disables all telemetry
```

---

## 🔧 OpenTelemetry Exporters

### Jaeger (Distributed Tracing)

```csharp
.WithTracing(tracing =>
{
    tracing.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://jaeger:4317");
        options.Protocol = OtlpExportProtocol.Grpc;
    });
})
```

**Docker Compose:**
```yaml
services:
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"  # UI
      - "4317:4317"    # OTLP gRPC
      - "4318:4318"    # OTLP HTTP
```

**View**: http://localhost:16686

### Prometheus (Metrics)

```csharp
.WithMetrics(metrics =>
{
    metrics.AddPrometheusExporter();
})

// In Program.cs
app.MapPrometheusScrapingEndpoint();  // /metrics endpoint
```

**Prometheus config** (`prometheus.yml`):
```yaml
scrape_configs:
  - job_name: 'playframework'
    static_configs:
      - targets: ['localhost:5000']
```

**View**: http://localhost:9090

### Grafana (Dashboards)

```csharp
.WithMetrics(metrics =>
{
    metrics.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://grafana:4317");
    });
})
```

**Docker Compose:**
```yaml
services:
  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_AUTH_ANONYMOUS_ENABLED=true
```

**View**: http://localhost:3000

### Azure Monitor (Application Insights)

```csharp
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    });
```

### Console Exporter (Development)

```csharp
.WithTracing(tracing =>
{
    tracing.AddConsoleExporter();
})
.WithMetrics(metrics =>
{
    metrics.AddConsoleExporter();
})
```

---

## 📊 Example Queries

### Prometheus (PromQL)

#### Average Scene Duration
```promql
rate(playframework_scene_duration_sum[5m]) 
/ 
rate(playframework_scene_duration_count[5m])
```

#### P95 Latency
```promql
histogram_quantile(0.95, 
  rate(playframework_scene_duration_bucket[5m]))
```

#### Success Rate
```promql
sum(rate(playframework_scene_executions{success="true"}[5m])) 
/ 
sum(rate(playframework_scene_executions[5m]))
```

#### Tokens per Minute
```promql
rate(playframework_llm_tokens_total[1m])
```

#### Cost per Hour
```promql
rate(playframework_cost_per_execution_sum[1h])
```

#### Cache Hit Rate
```promql
sum(rate(playframework_cache_hits[5m])) 
/ 
(sum(rate(playframework_cache_hits[5m])) + sum(rate(playframework_cache_misses[5m])))
```

### Jaeger (Trace Search)

```
service=MyApp
operation=SceneManager.Execute
duration>5s
error=true
tags.scene.name=CustomerSupport
```

---

## 🎯 Production Best Practices

### ✅ DO

1. **Use Sampling in Production**
   ```csharp
   telemetry.SamplingRate = 0.1;  // 10% for most apps
   ```

2. **Disable PII Collection**
   ```csharp
   telemetry.IncludeLlmPrompts = false;
   telemetry.IncludeLlmResponses = false;
   ```

3. **Add Environment Tags**
   ```csharp
   telemetry.CustomAttributes = new()
   {
       ["deployment.environment"] = "production",
       ["deployment.region"] = "us-east-1"
   };
   ```

4. **Monitor Costs**
   ```promql
   sum(rate(playframework_cost_per_execution_sum[1h]))
   ```

5. **Set Up Alerts**
   ```promql
   # Alert if P95 latency > 5s
   histogram_quantile(0.95, playframework_scene_duration_bucket) > 5000
   
   # Alert if error rate > 5%
   rate(playframework_scene_executions{success="false"}[5m]) / 
   rate(playframework_scene_executions[5m]) > 0.05
   ```

### ❌ DON'T

1. **Don't trace 100% in production** (high overhead)
2. **Don't include prompts/responses** (PII risk, large traces)
3. **Don't ignore trace IDs in logs** (correlation is key)
4. **Don't skip sampling configuration** (defaults to 100%)
5. **Don't forget to monitor cardinality** (too many unique tags)

---

## 🔗 Correlation: Logs ↔ Traces

### Automatic Correlation

When using OpenTelemetry with ILogger, traces and logs are automatically correlated:

```csharp
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.AddOtlpExporter();
});
```

### Log Output with TraceId

```
[12:34:56 INF] Scene execution started
   Factory: production
   TraceId: d4f2e3a1-b5c6-7890-abcd-1234567890ab  ← Click in Jaeger!
   SpanId: 1234567890abcdef
```

### Find Logs from Trace

In Jaeger, click on a span → View related logs (if exporter supports it).

---

## 🎭 Example: Full Setup

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// === LOGGING ===
builder.Logging.AddConsole();
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.AddOtlpExporter();
});

// === OPENTELEMETRY ===
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService("PlayFrameworkApp")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = builder.Environment.EnvironmentName,
                    ["service.version"] = "1.0.0"
                });
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(PlayFrameworkActivitySource.SourceName)
            .AddSource("Microsoft.Extensions.AI")
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .SetSampler(new TraceIdRatioBasedSampler(0.1))  // 10% sampling
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://jaeger:4317");
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(PlayFrameworkMetrics.MeterName)
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddPrometheusExporter();
    });

// === PLAYFRAMEWORK ===
builder.Services.AddPlayFramework(pf =>
{
    pf.WithTelemetry(telemetry =>
    {
        telemetry.EnableTracing = true;
        telemetry.EnableMetrics = true;
        telemetry.SamplingRate = 0.1;  // Must match OpenTelemetry sampler
        telemetry.CustomAttributes = new()
        {
            ["tenant.id"] = "customer-123"
        };
    });
    
    pf.AddScene(scene => scene
        .WithName("CustomerSupport")
        .WithDescription("Customer support assistant"));
});

var app = builder.Build();

// Expose Prometheus metrics
app.MapPrometheusScrapingEndpoint();

app.Run();
```

### Docker Compose

```yaml
version: '3.8'

services:
  app:
    build: .
    ports:
      - "5000:8080"
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317
    depends_on:
      - jaeger
      - prometheus

  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"  # UI
      - "4317:4317"    # OTLP gRPC
    environment:
      - COLLECTOR_OTLP_ENABLED=true

  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_AUTH_ANONYMOUS_ENABLED=true
    depends_on:
      - prometheus
```

---

## 🎯 Troubleshooting

### No traces appearing in Jaeger

1. Check sampling rate:
   ```csharp
   telemetry.SamplingRate = 1.0;  // 100% for testing
   ```

2. Check exporter endpoint:
   ```csharp
   options.Endpoint = new Uri("http://jaeger:4317");
   ```

3. Check ActivitySource is registered:
   ```csharp
   .AddSource(PlayFrameworkActivitySource.SourceName)
   ```

### Metrics not showing in Prometheus

1. Check /metrics endpoint:
   ```bash
   curl http://localhost:5000/metrics
   ```

2. Check Prometheus target:
   ```yaml
   targets: ['app:8080']  # Not localhost if in Docker
   ```

3. Check meter is registered:
   ```csharp
   .AddMeter(PlayFrameworkMetrics.MeterName)
   ```

### High memory usage

1. Reduce sampling:
   ```csharp
   telemetry.SamplingRate = 0.01;  // 1%
   ```

2. Disable prompt/response capture:
   ```csharp
   telemetry.IncludeLlmPrompts = false;
   telemetry.IncludeLlmResponses = false;
   ```

3. Reduce attribute length:
   ```csharp
   telemetry.MaxAttributeLength = 500;
   ```

---

## 📚 See Also

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/net/)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Microsoft.Extensions.AI Telemetry](https://learn.microsoft.com/en-us/dotnet/ai/observability)

---

## 🎉 Summary

PlayFramework's OpenTelemetry integration provides:

✅ **Zero-config Defaults** - Works out of the box  
✅ **Production Ready** - Configurable sampling & PII controls  
✅ **Complete Visibility** - Traces, metrics, and logs  
✅ **Standard Compliance** - OpenTelemetry standard  
✅ **Multi-Backend** - Jaeger, Prometheus, Azure Monitor, etc.  

**Start simple, scale as needed!** 🚀
