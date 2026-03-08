# Grafana Dashboards for PlayFramework

`src/AI/Rystem.PlayFramework/observability/grafana` contains the prebuilt Grafana material for the PlayFramework observability stack.

This folder is not generic Grafana advice. It is the concrete provisioning and dashboard content used by `src/AI/Rystem.PlayFramework/observability/docker-compose.yml`.

## What is in this folder

- `dashboards/playframework-overview.json`
- `dashboards/playframework-performance.json`
- `dashboards/playframework-cost-tracking.json`
- `provisioning/datasources/datasources.yml`
- `provisioning/dashboards/dashboards.yml`

## Datasources that are auto-provisioned

`provisioning/datasources/datasources.yml` currently creates:

- datasource `Prometheus` -> `http://prometheus:9090`
- datasource `Jaeger` -> `http://jaeger:16686`

So the dashboards are ready to query Prometheus immediately, and Jaeger is available in Grafana for trace exploration.

## Dashboard provisioning behavior

`provisioning/dashboards/dashboards.yml` currently configures:

- provider name `PlayFramework Dashboards`
- folder `PlayFramework`
- file-backed dashboards loaded from `/var/lib/grafana/dashboards`
- `updateIntervalSeconds: 10`
- `allowUiUpdates: true`

That means when the Docker stack starts, Grafana loads the JSON files automatically into the `PlayFramework` folder.

## Current dashboards

### `playframework-overview.json`

Current title:

```text
PlayFramework - Overview
```

This is the operational summary dashboard. The current panels include:

- scene execution rate
- overall success rate
- active scenes
- hourly cost
- cache hit ratio
- P95 latency by scene
- token usage rate
- tool success rate
- cache hits vs misses
- error rate by scene
- cumulative cost

Use it first when you want a fast health check or incident overview.

### `playframework-performance.json`

Current title:

```text
PlayFramework - Performance Deep Dive
```

The current panels include:

- latency percentiles P50/P95/P99
- tool call duration P95
- cache operation latency P95
- LLM call duration P95
- throughput by scene
- active LLM calls
- token-per-request histograms
- average latency by execution mode

Use it when you are tuning scene design, tool latency, or model behavior.

### `playframework-cost-tracking.json`

Current title:

```text
PlayFramework - Cost Tracking
```

The current panels include:

- hourly, daily, and monthly cost projections
- average cost per request
- cumulative cost over time
- cost rate per hour
- cost by scene
- token usage vs cost correlation
- cost per 1K tokens
- cache savings per hour
- total requests served
- cost distribution by execution mode
- top 5 most expensive scenes

Use it when you want budgeting, optimization, or usage visibility.

## How these dashboards are wired

The dashboards do not only use raw metrics. They also rely on recording rules from:

- `../prometheus/recording-rules.yml`

Examples used directly in the dashboards:

- `playframework:success_rate:5m`
- `playframework:scene_latency_p95:5m`
- `playframework:scene_throughput:5m`
- `playframework:cost_per_hour:5m`
- `playframework:avg_cost_per_request:5m`
- `playframework:tool_success_rate:5m`

They also use raw Prometheus metric families such as:

- `playframework_scene_executions_total`
- `playframework_scene_duration_milliseconds_bucket`
- `playframework_tool_duration_milliseconds_bucket`
- `playframework_cache_hits_total`
- `playframework_cache_misses_total`
- `playframework_cost_per_execution_usd_sum`
- `playframework_llm_tokens_total`

If the recording rules are missing or Prometheus is not scraping your app correctly, the dashboards will look partially empty.

## Start the dashboards locally

From `src/AI/Rystem.PlayFramework/observability`:

```bash
docker compose up -d
```

Then open:

```text
http://localhost:3000
```

Login with:

```text
admin / admin
```

Then browse the `PlayFramework` folder.

## Manual import

Auto-provisioning is the intended path, but you can also import the JSON files manually.

1. Open Grafana at `http://localhost:3000`
2. Go to `Dashboards` -> `Import`
3. Upload one of the JSON files from `dashboards/`
4. Select datasource `Prometheus`
5. Save the dashboard

## Adapting dashboards to your environment

Common customizations:

- duplicate a dashboard and change panel thresholds for your own latency or cost targets
- change the default time range in Grafana UI
- add variables for `scene_name`, environment, or tenant labels
- export the updated JSON back into `dashboards/` if you want the file to remain the source of truth

Example variable query for scene filtering:

```promql
label_values(playframework_scene_executions_total, scene_name)
```

Example panel query for a single selected scene:

```promql
sum(rate(playframework_scene_executions_total{scene_name="$scene_name"}[5m]))
```

## Important caveats

### These dashboards assume Prometheus-normalized metric names

PlayFramework emits .NET meter names such as `playframework.scene.duration`, but the Prometheus exporter normalizes them into names such as `playframework_scene_duration_milliseconds_bucket`. The dashboards are intentionally written against the Prometheus form.

### The dashboards are Prometheus-first, not Jaeger-first

The `Jaeger` datasource is provisioned, but the included dashboards are metric dashboards. Jaeger is mainly there for manual trace navigation and correlation, not as the datasource powering these panels.

### Alert notifications are not configured in Grafana here

The repo includes Prometheus alert rules in `../prometheus/alerts.yml`, but it does not ship Grafana managed alerts or an Alertmanager notification pipeline. If you want Slack, email, or PagerDuty delivery, you need to add that infrastructure separately.

### UI edits are convenient, but the JSON files remain the durable source

`allowUiUpdates: true` lets you tweak dashboards from the Grafana UI, but for a reproducible setup you should export important changes back into `dashboards/*.json`.

## Troubleshooting

### Dashboard opens but panels say `No data`

- verify Prometheus can scrape your PlayFramework app
- verify the correct scrape port in `../prometheus/prometheus.yml`
- verify recording rules are loaded in Prometheus
- widen the time range and generate some traffic

### Prometheus datasource is healthy but only some panels work

- the missing panels may depend on recording rules rather than raw metrics
- check `http://localhost:9090/rules`
- test the relevant PromQL expression directly in Prometheus UI

### Dashboards are missing after container startup

- check Grafana logs with `docker compose logs grafana`
- verify `dashboards/` is mounted into `/var/lib/grafana/dashboards`
- verify the provisioning files are mounted into `/etc/grafana/provisioning`

### Changes made in Grafana disappear later

- export your edited dashboard JSON and save it back into `dashboards/`
- otherwise file provisioning or container recreation can overwrite what only lived in the Grafana database

## Useful references

- `src/AI/Rystem.PlayFramework/observability/README.md`
- `src/AI/Rystem.PlayFramework/observability/docker-compose.yml`
- `src/AI/Rystem.PlayFramework/observability/prometheus/recording-rules.yml`
- `src/AI/Rystem.PlayFramework/observability/prometheus/alerts.yml`
- `src/AI/Rystem.PlayFramework/Docs/TELEMETRY.md`

Use this folder when you want prebuilt Grafana dashboards for a PlayFramework app that is already exporting traces and metrics.
