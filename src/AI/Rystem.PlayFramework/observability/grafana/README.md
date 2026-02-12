# Grafana Dashboards for PlayFramework

Pre-built Grafana dashboards for monitoring PlayFramework applications.

## 📊 Available Dashboards

### 1. **playframework-overview.json**
Main operational dashboard with key metrics:
- Scene execution rate (req/s)
- Success rate (%)
- Active scenes (gauge)
- Hourly cost ($)
- Cache hit ratio (%)
- P95 latency by scene
- Token usage rate
- Tool success rate
- Error rates

**When to use**: Daily operations, health checks, incident triage

---

### 2. **playframework-performance.json**
Deep performance analysis:
- Latency percentiles (P50/P95/P99)
- Tool call duration distribution
- Cache operation latency
- LLM call duration by model
- Throughput by scene
- Active LLM calls (concurrency)
- Tokens per request histogram
- Average latency by execution mode

**When to use**: Performance tuning, capacity planning, bottleneck identification

---

### 3. **playframework-cost-tracking.json**
Financial insights and cost optimization:
- Hourly/daily/monthly cost projections
- Average cost per request
- Cumulative cost over time
- Cost by scene
- Token usage vs cost correlation
- Cost per 1K tokens
- Cache savings ($)
- Top 5 most expensive scenes

**When to use**: Budget monitoring, cost optimization, billing analysis

---

## 🚀 How to Import

### Automatic (Recommended)

Dashboards are auto-provisioned when using the provided `docker-compose.yml`:

```bash
cd observability
docker-compose up -d
```

Wait 30 seconds, then open Grafana → Dashboards → Browse → "PlayFramework" folder.

---

### Manual Import

1. Open Grafana: http://localhost:3000
2. Login: `admin` / `admin`
3. Click **Dashboards** (left menu) → **Import**
4. Click **Upload JSON file**
5. Select one of the dashboard files
6. Choose **Prometheus** as datasource
7. Click **Import**

---

## 🔧 Customization

### Change Time Range

Default: Last 1 hour (Overview, Performance) | Last 24 hours (Cost Tracking)

To change:
1. Click time picker (top right)
2. Select custom range
3. Click **Save dashboard** → **Save**

### Add Custom Panels

Example: "Scene execution count by hour"

1. Click **Add panel** → **Add a new panel**
2. Query:
   ```promql
   sum(increase(playframework_scene_executions_total[1h])) by (scene_name)
   ```
3. Visualization: **Bar chart**
4. Title: "Hourly Execution Count"
5. Click **Apply**

### Modify Alerts

Example: High latency alert at 5s instead of 10s

1. Edit panel → **Alert** tab
2. Change condition:
   ```promql
   histogram_quantile(0.95, ...) > 5000
   ```
3. Configure notification channel
4. Click **Save**

---

## 📈 Key Metrics Explained

### Success Rate
```promql
playframework:success_rate:5m * 100
```
Percentage of successful scene executions (no exceptions).  
**Threshold**: <95% = warning, <90% = critical

### P95 Latency
```promql
playframework:scene_latency_p95:5m
```
95% of requests complete faster than this value.  
**Threshold**: >10s = warning, >20s = critical

### Cache Hit Ratio
```promql
playframework:cache_hit_ratio:5m * 100
```
Percentage of requests served from cache (no LLM call).  
**Threshold**: <50% = warning (cache not effective)

### Hourly Cost
```promql
playframework:cost_per_hour:5m
```
Projected cost per hour based on current rate.  
**Threshold**: >$10/hour = warning (adjust based on budget)

---

## 🎨 Visual Customization

### Change Colors

1. Edit panel
2. **Field** tab → **Thresholds**
3. Add steps:
   - 0-95: Red (bad)
   - 95-99: Yellow (ok)
   - 99-100: Green (good)

### Add Annotations

Mark deployments/incidents on graphs:

1. Dashboard settings → **Annotations**
2. Click **New annotation**
3. Name: "Deployments"
4. Data source: Prometheus
5. Query:
   ```promql
   changes(playframework_scene_executions_total[5m]) > 0
   ```

---

## 📊 Dashboard Variables

Add variables for dynamic filtering:

1. Dashboard settings → **Variables** → **New**
2. Name: `scene_name`
3. Type: Query
4. Data source: Prometheus
5. Query:
   ```promql
   label_values(playframework_scene_executions_total, scene_name)
   ```
6. Use in panels:
   ```promql
   playframework_scene_executions_total{scene_name="$scene_name"}
   ```

---

## 🔍 Troubleshooting

### "No data" in panels

**Check**:
1. Prometheus datasource connected: Configuration → Data Sources
2. Metrics being scraped: http://localhost:9090/targets
3. Time range correct (check if data exists in that period)
4. Query syntax correct (test in Prometheus UI)

### Slow dashboard loading

**Solutions**:
1. Reduce time range (1h instead of 24h)
2. Increase scrape interval in `prometheus.yml` (15s → 30s)
3. Use recording rules (pre-aggregated queries)
4. Disable auto-refresh for heavy dashboards

### Panel shows "N/A"

**Reason**: No data points in selected time range.

**Fix**: Run your application and generate load, or select wider time range.

---

## 🚀 Advanced Features

### Shared Crosshair
Hover over one graph to see values on all graphs at same time.

1. Dashboard settings → **General**
2. **Graph tooltip** → **Shared crosshair**

### Template Variables for Multi-Tenant

Filter by tenant/environment:

```promql
playframework_scene_executions_total{tenant_id="$tenant",environment="$env"}
```

### Alert Notification Channels

Send alerts to Slack/Email/PagerDuty:

1. Alerting → **Notification channels** → **New channel**
2. Type: Slack
3. Webhook URL: `https://hooks.slack.com/services/...`
4. Test notification
5. Link to dashboard alerts

---

## 📚 Related Documentation

- **TELEMETRY.md** - How telemetry works in PlayFramework
- **observability/README.md** - Full observability stack setup
- **prometheus/alerts.yml** - Alert rules
- **prometheus/recording-rules.yml** - Pre-aggregated queries

---

## 🆘 Support

For issues with dashboards:
1. Check Grafana logs: `docker-compose logs grafana`
2. Verify Prometheus queries in Prometheus UI: http://localhost:9090
3. Check PlayFramework telemetry configuration: `TELEMETRY.md`
4. Open issue: https://github.com/KeyserDSoze/Rystem/issues
