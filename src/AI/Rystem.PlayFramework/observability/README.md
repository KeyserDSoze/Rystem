# PlayFramework Observability Stack

Complete observability setup with **Jaeger** (tracing), **Prometheus** (metrics), and **Grafana** (visualization).

## 🚀 Quick Start (5 minutes)

### 1. Configure Your Application

```csharp
// Program.cs
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add PlayFramework with telemetry
builder.Services.AddPlayFramework(play =>
{
    play.WithTelemetry(telemetry =>
    {
        telemetry.EnableTracing = true;
        telemetry.EnableMetrics = true;
        telemetry.SamplingRate = 1.0; // 100% in dev, 0.1 (10%) in production
    });
    
    play.AddScene(scene => scene.WithName("Demo"));
});

// Configure OpenTelemetry Exporters
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("playframework-api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(PlayFrameworkActivitySource.SourceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
                options.Protocol = OtlpExportProtocol.Grpc;
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(PlayFrameworkMetrics.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter();  // Expose /metrics endpoint
    });

var app = builder.Build();

// Important: Enable Prometheus scraping
app.MapPrometheusScrapingEndpoint();  // Exposes /metrics

app.Run();
```

### 2. Start Observability Stack

```bash
cd observability
docker-compose up -d
```

Wait 30 seconds for all services to start.

### 3. Access Dashboards

| Service    | URL                          | Credentials    | Purpose                    |
|------------|------------------------------|----------------|----------------------------|
| Grafana    | http://localhost:3000        | admin/admin    | Metrics visualization      |
| Jaeger     | http://localhost:16686       | None           | Distributed tracing        |
| Prometheus | http://localhost:9090        | None           | Metrics storage/queries    |

### 4. Import Dashboards (Auto-provisioned)

Dashboards are automatically imported on startup:
- **PlayFramework - Overview** (main dashboard)
- **PlayFramework - Performance Deep Dive** (latency analysis)
- **PlayFramework - Cost Tracking** (billing insights)

If they don't appear, manually import from `grafana/dashboards/` folder:
1. Go to Grafana → Dashboards → Import
2. Upload JSON file
3. Select "Prometheus" datasource

---

## 📊 What You'll See

### Overview Dashboard
- **Scene Execution Rate** (requests/sec)
- **Success Rate** (%)
- **Active Scenes** (concurrent)
- **Hourly Cost** ($)
- **Cache Hit Ratio** (%)
- **P95 Latency** (ms)
- **Token Usage Rate** (tokens/sec)

### Performance Dashboard
- **Latency Percentiles** (P50/P95/P99)
- **Tool Call Duration**
- **Cache Operation Latency**
- **LLM Call Duration**
- **Throughput by Scene**

### Cost Tracking Dashboard
- **Hourly/Daily/Monthly Projections**
- **Cost by Scene**
- **Cache Savings**
- **Top 5 Most Expensive Scenes**

---

## 🔍 Example Traces in Jaeger

```
SceneManager.Execute [5.2s]
  ├─ factory_name: production
  ├─ scene.name: CustomerSupport
  ├─ tokens.total: 350
  ├─ cost: 0.0045
  │
  ├─ Scene.Execute [4.8s]
  │   ├─ Tool.Execute: GetCustomerInfo [1.2s]
  │   │   └─ tool.success: true
  │   │
  │   ├─ Cache.Get: prompt-hash-abc123 [2ms] ❌ MISS
  │   │
  │   └─ LLM.Call: gpt-4o [3.4s]
  │       ├─ llm.provider: openai
  │       ├─ llm.model: gpt-4o
  │       ├─ tokens.prompt: 150
  │       ├─ tokens.completion: 200
  │       └─ cost: 0.0045
  │
  └─ Cache.Set: prompt-hash-abc123 [5ms]
```

---

## 🔧 Configuration

### Adjust Prometheus Scrape Target

Edit `prometheus/prometheus.yml`:

```yaml
scrape_configs:
  - job_name: 'playframework-api'
    static_configs:
      - targets: ['host.docker.internal:5000']  # Change to YOUR API port
```

### Enable Alerts

Alerts are pre-configured in `prometheus/alerts.yml`:
- High error rate (>5%)
- High latency (P95 >10s)
- High hourly cost (>$10/hour)
- Low cache hit rate (<50%)
- Tool failures (>10%)

### Adjust Sampling Rate

For production, reduce sampling to save costs:

```csharp
play.WithTelemetry(telemetry =>
{
    telemetry.SamplingRate = 0.1;  // 10% sampling
});
```

---

## 📈 Common Prometheus Queries

### Cost Analysis
```promql
# Hourly cost projection
sum(rate(playframework_cost_per_execution_usd_sum[5m])) * 3600

# Cost per scene
sum(rate(playframework_cost_per_execution_usd_sum[5m])) by (scene_name) * 3600

# Cache savings per hour
(sum(rate(playframework_cache_hits_total[5m])) * avg(playframework_cost_per_execution_usd)) * 3600
```

### Performance Analysis
```promql
# P95 latency by scene
histogram_quantile(0.95, sum(rate(playframework_scene_duration_milliseconds_bucket[5m])) by (le, scene_name))

# Throughput
sum(rate(playframework_scene_executions_total[5m])) by (scene_name)

# Success rate
sum(rate(playframework_scene_executions_total{success="true"}[5m])) / sum(rate(playframework_scene_executions_total[5m]))
```

### Cache Analysis
```promql
# Hit ratio
sum(rate(playframework_cache_hits_total[5m])) / (sum(rate(playframework_cache_hits_total[5m])) + sum(rate(playframework_cache_misses_total[5m])))

# Cache latency P95
histogram_quantile(0.95, sum(rate(playframework_cache_duration_milliseconds_bucket[5m])) by (le))
```

---

## 🛑 Stop/Restart

```bash
# Stop all services
docker-compose down

# Stop and remove volumes (fresh start)
docker-compose down -v

# View logs
docker-compose logs -f grafana
docker-compose logs -f prometheus
docker-compose logs -f jaeger
```

---

## 🌐 Production Deployment

### Azure Monitor / Application Insights

```csharp
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("https://your-app-insights.azure.com/v1/traces");
    options.Headers = $"Authorization=Bearer {apiKey}";
});
```

### AWS CloudWatch

```csharp
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("https://cloudwatch-otel.us-east-1.amazonaws.com");
});
```

### Datadog

```csharp
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("https://api.datadoghq.com");
    options.Headers = $"DD-API-KEY={apiKey}";
});
```

---

## 🆘 Troubleshooting

### Metrics not appearing in Prometheus

1. Check `/metrics` endpoint: http://localhost:5000/metrics
2. Verify Prometheus scraping: http://localhost:9090/targets
3. Check `prometheus/prometheus.yml` target address

### Traces not appearing in Jaeger

1. Verify OTLP exporter endpoint: `http://localhost:4317`
2. Check sampling rate (0.0 = disabled)
3. Verify `PlayFrameworkActivitySource.SourceName` is added to tracing

### Dashboards not loading in Grafana

1. Check Grafana logs: `docker-compose logs grafana`
2. Verify datasources: Grafana → Configuration → Data Sources
3. Manually import dashboards from `grafana/dashboards/`

### High CPU usage

1. Reduce scrape interval in `prometheus.yml` (15s → 30s)
2. Decrease sampling rate (1.0 → 0.1)
3. Disable PII logging (IncludeLlmPrompts, IncludeLlmResponses)

---

## 📚 Resources

- **OpenTelemetry Docs**: https://opentelemetry.io/docs/
- **Prometheus Docs**: https://prometheus.io/docs/
- **Grafana Docs**: https://grafana.com/docs/
- **Jaeger Docs**: https://www.jaegertracing.io/docs/
- **PlayFramework Telemetry**: See `TELEMETRY.md`

---

## 🎓 Next Steps

1. ✅ Start observability stack
2. ✅ Run your application with telemetry enabled
3. ✅ Open Grafana and explore dashboards
4. ✅ Simulate load and watch metrics update
5. ✅ Search traces in Jaeger
6. ✅ Set up alerts for production

**Questions?** Check `TELEMETRY.md` for detailed telemetry documentation.
