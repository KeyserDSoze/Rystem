# PlayFramework Observability Stack

`src/AI/Rystem.PlayFramework/observability` is a local Docker-based observability bundle for PlayFramework.

It gives you a ready-made stack with:

- Jaeger for traces
- Prometheus for scraping, recording rules, and alert evaluation
- Grafana for dashboards
- Node Exporter for optional host metrics

This folder is infrastructure and dashboard material. It does not automatically instrument your PlayFramework application for you. Your app still needs OpenTelemetry tracing, metrics export, and a `/metrics` endpoint.

## What is in this folder

- `docker-compose.yml` - local stack definition
- `prometheus/prometheus.yml` - scrape targets
- `prometheus/recording-rules.yml` - pre-aggregated queries used by dashboards
- `prometheus/alerts.yml` - Prometheus alert rules
- `grafana/provisioning/datasources/datasources.yml` - preconfigured `Prometheus` and `Jaeger` datasources
- `grafana/provisioning/dashboards/dashboards.yml` - dashboard auto-provisioning
- `grafana/dashboards/*.json` - the PlayFramework dashboards themselves

## What you still need in your app

The PlayFramework telemetry primitives are inside the main package:

- `PlayFrameworkActivitySource.SourceName` = `Rystem.PlayFramework`
- `PlayFrameworkMetrics.MeterName` = `Rystem.PlayFramework`

PlayFramework emits activities and meters, but your app must connect them to OpenTelemetry exporters.

## Example: instrument a PlayFramework backend

This is the minimum shape you need in your application to make the observability stack useful.

```csharp
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Rystem.PlayFramework;
using Rystem.PlayFramework.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPlayFramework("default", framework =>
{
    framework
        .WithTelemetry(telemetry =>
        {
            telemetry.EnableTracing = true;
            telemetry.EnableMetrics = true;
            telemetry.SamplingRate = 1.0;
            telemetry.CustomAttributes = new()
            {
                ["deployment.environment"] = builder.Environment.EnvironmentName,
                ["service.version"] = "1.0.0"
            };
        })
        .AddScene("General Requests", "General conversation", _ => { });
});

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
            .AddPrometheusExporter();
    });

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

app.Run();
```

Without the OTLP exporter and `MapPrometheusScrapingEndpoint()`, Jaeger and Prometheus have nothing to collect.

The snippet only shows the observability wiring. Keep your normal PlayFramework chat-client or adapter registration in place as well.

## Start the local stack

From this folder:

```bash
docker compose up -d
```

The stack defined in `docker-compose.yml` exposes:

| Service | URL | Purpose |
| --- | --- | --- |
| Grafana | `http://localhost:3000` | dashboards |
| Jaeger | `http://localhost:16686` | trace search and timelines |
| Prometheus | `http://localhost:9090` | metrics and alert rule evaluation |
| Node Exporter | `http://localhost:9100` | host-level machine metrics |

Grafana credentials are currently:

```text
admin / admin
```

## Important setup step: fix the Prometheus scrape target

The provided `prometheus/prometheus.yml` currently points the PlayFramework scrape job to:

```yaml
targets: ['host.docker.internal:5000']
```

That is only a placeholder. You must change it to your real app port.

For example, the sample backend in this repo uses `http://localhost:5158`, so the scrape target should be:

```yaml
targets: ['host.docker.internal:5158']
```

After editing the file, reload Prometheus:

```bash
docker compose restart prometheus
```

Or trigger a config reload if lifecycle reload is enabled.

## Docker host alias caveat

The provided configuration uses `host.docker.internal`, which works well on Docker Desktop environments.

On Linux you may need to replace it with a reachable host IP or add the appropriate Docker host alias yourself.

## What Grafana auto-provisions

Grafana is preconfigured from:

- `grafana/provisioning/datasources/datasources.yml`
- `grafana/provisioning/dashboards/dashboards.yml`

That means on startup it automatically creates:

- datasource `Prometheus` -> `http://prometheus:9090`
- datasource `Jaeger` -> `http://jaeger:16686`
- folder `PlayFramework`
- dashboards from `grafana/dashboards/`

Current dashboard titles are:

- `PlayFramework - Overview`
- `PlayFramework - Performance Deep Dive`
- `PlayFramework - Cost Tracking`

## What PlayFramework emits

From the source, PlayFramework emits:

- activities such as `SceneManager.Execute`, `Scene.Execute`, `Tool.Execute`, `LLM.Call`, `Cache.Get`, `Cache.Set`
- tags such as `playframework.factory_name`, `playframework.scene.name`, `playframework.scene.mode`, `playframework.cost.total`, `playframework.llm.tokens.total`
- metrics through `PlayFrameworkMetrics`, including scene counts, tool counts, cache hits/misses, LLM call counts, durations, costs, and active gauges

Examples from source files:

- `PlayFrameworkActivitySource.Activities.SceneManagerExecute`
- `PlayFrameworkActivitySource.Activities.SceneExecute`
- `PlayFrameworkActivitySource.Activities.ToolExecute`
- `PlayFrameworkActivitySource.Activities.LlmCall`
- `PlayFrameworkMetrics.RecordSceneExecution(...)`
- `PlayFrameworkMetrics.RecordToolCall(...)`
- `PlayFrameworkMetrics.RecordCacheAccess(...)`
- `PlayFrameworkMetrics.RecordLlmCall(...)`

## Prometheus metric naming note

Inside `PlayFrameworkMetrics.cs`, the meter names use dotted .NET style names such as:

- `playframework.scene.executions`
- `playframework.scene.duration`
- `playframework.cost.per_execution`

When exposed through the Prometheus exporter, those become normalized Prometheus names such as:

- `playframework_scene_executions_total`
- `playframework_scene_duration_milliseconds_bucket`
- `playframework_cost_per_execution_usd_sum`

That is why the Grafana dashboards and alert rules use underscored Prometheus names rather than the dotted meter names from C#.

## Recording rules and dashboards

The dashboards are intentionally built on top of the recording rules in `prometheus/recording-rules.yml`.

Important recording rules include:

- `playframework:success_rate:5m`
- `playframework:scene_throughput:5m`
- `playframework:scene_latency_p95:5m`
- `playframework:cache_hit_ratio:5m`
- `playframework:cost_per_hour:5m`
- `playframework:tool_success_rate:5m`

If dashboards show missing values, check both raw metrics and the recording rules.

## Alerts in this stack

Prometheus alert rules are included in `prometheus/alerts.yml`, for example:

- high error rate
- high scene latency
- high projected hourly cost
- low cache hit rate
- high token usage
- high tool failure rate
- metrics endpoint down

Important caveat: this stack does not include an `alertmanager` service.

So alerts can still be evaluated in Prometheus, but notification delivery to Slack, email, or PagerDuty is not wired out of the box.

## Example local validation flow

1. instrument your app with OpenTelemetry and map `/metrics`
2. change `prometheus/prometheus.yml` to the correct host port
3. run `docker compose up -d`
4. open `http://localhost:9090/targets` and verify the PlayFramework job is `UP`
5. execute a few PlayFramework requests in your app
6. open Jaeger and search for traces from service `playframework-api`
7. open Grafana and inspect the `PlayFramework` folder

## Example queries you can run in Prometheus

```promql
# overall success rate
playframework:success_rate:5m

# projected hourly cost
playframework:cost_per_hour:5m

# p95 latency by scene
playframework:scene_latency_p95:5m

# cache hit ratio
playframework:cache_hit_ratio:5m

# raw throughput by scene
sum(rate(playframework_scene_executions_total[5m])) by (scene_name)
```

## Troubleshooting

### No metrics in Prometheus

- verify your app exposes `/metrics`
- verify `app.MapPrometheusScrapingEndpoint()` is present
- verify the target port in `prometheus/prometheus.yml`
- verify `http://localhost:9090/targets` shows the job as `UP`

### No traces in Jaeger

- verify your app adds `.AddSource(PlayFrameworkActivitySource.SourceName)`
- verify the OTLP exporter points to `http://localhost:4317`
- verify your app is actually executing PlayFramework requests

### Dashboards show no data

- verify Prometheus has raw metrics first
- verify recording rules loaded successfully in Prometheus
- verify Grafana datasource `Prometheus` is healthy
- widen the dashboard time range if you only generated a few requests

### Alert rules exist but nothing notifies anyone

- expected with the current files; there is no Alertmanager service in `docker-compose.yml`

## Important caveats

### The sample API in this repo is not fully instrumented out of the box

`src/AI/Test/Rystem.PlayFramework.Api/Program.cs` enables PlayFramework metrics at the builder level, but it does not currently add the OpenTelemetry exporters or map `/metrics` for this Docker stack. You still need to wire that yourself.

### Some telemetry settings are more granular than the runtime currently enforces

Flags like `TraceScenes`, `TraceTools`, `TraceLlmCalls`, `TraceCacheOperations`, `TracePlanning`, `TraceSummarization`, `TraceDirector`, and `TraceMcpOperations` exist on `TelemetrySettings`, but the current implementation relies much more heavily on the broad `EnableTracing` and `EnableMetrics` switches than those fine-grained toggles.

### Prompt and response capture needs privacy review

`IncludeLlmPrompts` and `IncludeLlmResponses` can expose sensitive data. Keep them off unless you intentionally want payload-level tracing and have reviewed the privacy implications.

## Useful references

- `src/AI/Rystem.PlayFramework/Telemetry/PlayFrameworkActivitySource.cs`
- `src/AI/Rystem.PlayFramework/Telemetry/PlayFrameworkMetrics.cs`
- `src/AI/Rystem.PlayFramework/Telemetry/TelemetrySettings.cs`
- `src/AI/Rystem.PlayFramework/Docs/TELEMETRY.md`
- `src/AI/Rystem.PlayFramework/observability/docker-compose.yml`
- `src/AI/Rystem.PlayFramework/observability/prometheus/prometheus.yml`
- `src/AI/Rystem.PlayFramework/observability/prometheus/recording-rules.yml`
- `src/AI/Rystem.PlayFramework/observability/prometheus/alerts.yml`
- `src/AI/Rystem.PlayFramework/observability/grafana/README.md`

Use this folder when you want a local Grafana + Prometheus + Jaeger stack around a PlayFramework app that you have already instrumented.
