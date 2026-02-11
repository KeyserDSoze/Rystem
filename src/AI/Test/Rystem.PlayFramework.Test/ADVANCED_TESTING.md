# Advanced Integration Tests - Complete Guide

## 🎯 Overview

`AdvancedIntegrationTests.cs` contiene **5 test estremamente complessi** che simulano scenari business realistici con workflow avanzati. Questi test dimostrano le capacità complete del PlayFramework in situazioni real-world di Business Intelligence e Data Analysis.

## 🏗️ Architettura Enhanced

### 6 Scene Orchestrate

Questi test utilizzano **6 scene specializzate** (le 4 originali + 2 nuove):

#### Scene Esistenti

1. **Calculator** - Operazioni matematiche di base
2. **Weather** - Dati meteorologici
3. **DataAnalysis** - Analisi statistica
4. **ReportGenerator** - Generazione report

#### 🆕 Nuove Scene Business

5. **SalesAnalysis** 💰
   - **Purpose**: Analisi dati vendite
   - **Tools**:
     - `getMonthlySales(year, month)` - Vendite mensili
     - `getYearlySales(year)` - Vendite annuali
     - `getTopProducts(count)` - Top N prodotti
   - **Mock Data**: Q1 2024 con growth ~16% YoY

6. **BusinessAnalytics** 📊
   - **Purpose**: Metriche business avanzate
   - **Tools**:
     - `calculateGrowthRate(old, new)` - Tasso di crescita %
     - `calculatePercentage(part, total)` - Calcolo percentuali
     - `detectTrend(values[])` - Rilevamento tendenze
   - **Use Cases**: KPI tracking, forecasting, trend analysis

## 📋 Test Suite

### Test 1: Quarterly Sales Analysis with Weather Correlation ⭐⭐⭐⭐⭐

**Complessità**: MASSIMA  
**Stima Tool Calls**: 15-20  
**Stima Scene**: 5-6  
**Scenario**: Analisi trimestrale Q1 2024 con correlazione meteo

```csharp
Advanced_QuarterlySalesAnalysis_WithWeatherCorrelation()
```

#### Workflow Previsto

```
1. SALES DATA COLLECTION
   └─ SalesAnalysis.getMonthlySales(2024, 1) → $52,000
   └─ SalesAnalysis.getMonthlySales(2024, 2) → $56,000
   └─ SalesAnalysis.getMonthlySales(2024, 3) → $61,000
   └─ Calculator.add(52000, 56000) → 108,000
   └─ Calculator.add(108000, 61000) → $169,000 (Q1 Total)

2. GROWTH ANALYSIS
   └─ BusinessAnalytics.calculateGrowthRate(52000, 56000) → +7.7%
   └─ BusinessAnalytics.calculateGrowthRate(56000, 61000) → +8.9%

3. STATISTICAL ANALYSIS
   └─ DataAnalysis.calculateAverage([52000, 56000, 61000]) → $56,333
   └─ DataAnalysis.findMinimum([52000, 56000, 61000]) → $52,000 (Jan)
   └─ DataAnalysis.findMaximum([52000, 56000, 61000]) → $61,000 (Mar)

4. WEATHER CORRELATION
   └─ Weather.getTemperature("Milan") → 18.5°C
   └─ Weather.getTemperature("Rome") → 22.3°C
   └─ Weather.getTemperature("Paris") → 15.2°C
   └─ DataAnalysis.calculateAverage([18.5, 22.3, 15.2]) → 18.7°C

5. EXECUTIVE REPORT
   └─ ReportGenerator.formatAsTable(headers, salesData) → Markdown table
   └─ ReportGenerator.formatAsTable(headers, weatherData) → Markdown table
   └─ ReportGenerator.createMarkdownReport(title, content) → Full report

EXPECTED OUTPUT:
# Q1 2024 Business Intelligence Report

## Executive Summary
Q1 2024 demonstrated strong performance with $169K total revenue...

## Sales Performance

| Month | Revenue | Growth % |
|-------|---------|----------|
| Jan   | $52,000 | -        |
| Feb   | $56,000 | +7.7%    |
| Mar   | $61,000 | +8.9%    |

## Weather Correlation

| City  | Avg Temperature |
|-------|-----------------|
| Milan | 18.5°C         |
| Rome  | 22.3°C         |
| Paris | 15.2°C         |

## Key Findings
- Consistent month-over-month growth
- Positive temperature correlation
- March peak aligns with spring season

## Recommendations
1. Increase marketing spend for Q2
2. Expand in warmer markets
3. Leverage seasonal trends
```

#### Assertions

```csharp
✅ scenesUsed.Count >= 3
✅ Contains "SalesAnalysis"
✅ Contains "Calculator"
✅ toolsExecuted.Count >= 10
✅ Contains AiResponseStatus.Planning
✅ Final message contains "report"
```

---

### Test 2: Year-over-Year Analysis with Forecasting ⭐⭐⭐⭐

**Complessità**: ALTA  
**Stima Tool Calls**: 10-15  
**Stima Scene**: 4-5  
**Scenario**: Comparazione 2023 vs 2024 con previsioni

```csharp
Advanced_YearOverYearAnalysis_WithForecasting()
```

#### Workflow Previsto

```
1. GET YEARLY SALES
   └─ SalesAnalysis.getYearlySales(2023) → $722,000
   └─ SalesAnalysis.getYearlySales(2024) → $169,000 (YTD Q1)

2. CALCULATE GROWTH RATE
   └─ Normalize to Q1 comparison: 722k/4 = 180.5k (Q1 2023 estimate)
   └─ BusinessAnalytics.calculateGrowthRate(180500, 169000) → -6.4% ⚠️

3. CONDITIONAL LOGIC
   IF growth < 0% THEN:
     └─ Identify concerning trends
     └─ Analyze what changed
     └─ Generate recovery recommendations
   
4. MONTHLY BREAKDOWN COMPARISON
   └─ Get monthly patterns for both years
   └─ BusinessAnalytics.detectTrend([values]) → "Moderate upward trend"

5. FORECAST Q2 2024
   └─ Based on Q1 growth: Forecast ~$180k for Q2
   └─ Generate forecast report with confidence levels
```

#### Key Features Demonstrated

- ✅ **Conditional workflow branching** based on growth rate
- ✅ **Trend detection** using BusinessAnalytics
- ✅ **Forecasting logic** with LLM reasoning
- ✅ **Strategic recommendations** based on data

---

### Test 3: Regional Sales Analysis with Weather Impact ⭐⭐⭐⭐

**Complessità**: ALTA  
**Stima Tool Calls**: 12-15  
**Stima Scene**: 4  
**Scenario**: Analisi performance per città con correlazione meteo

```csharp
Advanced_RegionalSalesAnalysis_WithWeatherImpact()
```

#### Workflow Previsto

```
PARALLEL DATA COLLECTION (conceptual):

Milan:
  └─ Weather.getTemperature("Milan") → 18.5°C
  └─ Estimate: 18.5 * 1000 = $18,500

Rome:
  └─ Weather.getTemperature("Rome") → 22.3°C
  └─ Estimate: 22.3 * 1000 = $22,300

Paris:
  └─ Weather.getTemperature("Paris") → 15.2°C
  └─ Estimate: 15.2 * 1000 = $15,200

ANALYSIS:
  └─ DataAnalysis.calculateAverage([18.5, 22.3, 15.2]) → 18.7°C
  └─ DataAnalysis.findMaximum([18500, 22300, 15200]) → Rome ($22,300)
  └─ DataAnalysis.findMinimum([18500, 22300, 15200]) → Paris ($15,200)
  └─ Calculator.subtract(22.3, 15.2) → 7.1°C (temp range)
  └─ Calculator.add(18500 + 22300 + 15200) → $56,000 (total est. sales)

RANKING:
  1. Rome - $22,300 (Warmest, Highest sales)
  2. Milan - $18,500
  3. Paris - $15,200 (Coldest, Lowest sales)

REPORT:
  └─ ReportGenerator.formatAsTable(...) → Comparison table
```

#### Expected Output

```markdown
| City  | Temperature | Est. Sales | Performance Rank |
|-------|-------------|------------|------------------|
| Rome  | 22.3°C      | $22,300    | 1st 🥇          |
| Milan | 18.5°C      | $18,500    | 2nd 🥈          |
| Paris | 15.2°C      | $15,200    | 3rd 🥉          |

**Key Insight**: Strong positive correlation between temperature and sales.
Warmer cities show 46% higher sales than colder regions.
```

---

### Test 4: Conditional Workflow with Decision Tree ⭐⭐⭐⭐

**Complessità**: ALTA  
**Stima Tool Calls**: 8-12  
**Stima Scene**: 3-4  
**Scenario**: Automated decision making con logica condizionale

```csharp
Advanced_ConditionalWorkflow_WithDecisionTree()
```

#### Decision Tree Logic

```
START: Get sales data
  └─ SalesAnalysis.getYearlySales(2023) → $722,000
  └─ SalesAnalysis.getYearlySales(2024) → $169,000 (YTD)
  └─ BusinessAnalytics.calculateGrowthRate(...) → X%

BRANCH 1: IF X > 20%
  ├─ SalesAnalysis.getTopProducts(3) → [Product1, Product2, Product3]
  ├─ Calculate contribution %
  └─ RECOMMEND: "Accelerate growth strategy"
     * Increase marketing budget by 30%
     * Expand to new markets
     * Launch new product variants

BRANCH 2: ELSE IF X > 0%
  ├─ Analyze steady growth pattern
  └─ RECOMMEND: "Maintain current trajectory"
     * Continue existing strategies
     * Monitor key metrics
     * Optimize operational efficiency

BRANCH 3: ELSE (X <= 0%)
  ├─ Identify critical issues
  ├─ Analyze root causes
  └─ RECOMMEND: "Implement recovery plan"
     * Cost reduction initiatives
     * Market repositioning
     * Product/service improvements
```

#### Key Demonstrations

- ✅ **Complex conditional logic** handled by LLM
- ✅ **Context-aware recommendations** based on scenario
- ✅ **Multi-path execution** with different tool sequences
- ✅ **Strategic business reasoning**

---

### Test 5: Maximum Complexity - All Scenes Orchestrated ⭐⭐⭐⭐⭐

**Complessità**: EXTREME  
**Stima Tool Calls**: 20-30  
**Stima Scene**: 6/6 (ALL)  
**Scenario**: Report Business Intelligence completo con tutte le scene

```csharp
Advanced_MaximumComplexity_AllScenesOrchestrated()
```

#### The Ultimate Test

Questo test utilizza **TUTTE E 6 LE SCENE** in un unico workflow orchestrato, simulando un vero scenario di business intelligence enterprise.

#### Comprehensive Workflow

```
PART 1: SALES PERFORMANCE (3 scenes: SalesAnalysis, Calculator, BusinessAnalytics)
  ├─ Get Q1 monthly sales (Jan, Feb, Mar)
  ├─ Calculate total revenue
  └─ Calculate month-over-month growth rates

PART 2: STATISTICAL ANALYSIS (2 scenes: DataAnalysis, Calculator)
  ├─ Average monthly sales
  ├─ Best/worst months
  └─ Variance calculation

PART 3: YEAR-OVER-YEAR COMPARISON (2 scenes: SalesAnalysis, BusinessAnalytics)
  ├─ Get Q1 2023 baseline
  ├─ Calculate YoY growth
  └─ Performance assessment

PART 4: PRODUCT ANALYSIS (2 scenes: SalesAnalysis, BusinessAnalytics)
  ├─ Top 3 products
  └─ % contribution to total sales

PART 5: WEATHER CORRELATION (2 scenes: Weather, DataAnalysis)
  ├─ Regional temperatures (Milan, Rome, Paris)
  ├─ Average regional temp
  └─ Hypothesize weather impact

PART 6: EXECUTIVE REPORT (1 scene: ReportGenerator)
  ├─ Executive Summary (3-4 sentences)
  ├─ Sales Performance Table
  ├─ Key Metrics Table
  ├─ Weather Data Table
  ├─ Top Products List
  ├─ Strategic Recommendations (3 points)
  └─ Conclusion

EXPECTED OUTPUT: McKinsey-style professional report (500+ chars)
```

#### Execution Metrics

```
📊 EXECUTION METRICS
================================================================================
Total Execution Time: 45-90s (depending on LLM speed)
Total Responses: 40-60
Scenes Used: 6/6 ✅
  - SalesAnalysis: ~6 tools
  - Calculator: ~8 tools
  - BusinessAnalytics: ~4 tools
  - DataAnalysis: ~5 tools
  - Weather: ~3 tools
  - ReportGenerator: ~3 tools
Total Tool Calls: 20-30
Total Cost: $0.10-0.20 (GPT-4)
================================================================================
```

#### Assertions

```csharp
✅ scenesUsed.Count >= 4 (expected 6)
✅ toolsByScene.Values.Sum() >= 15 (expected 20-30)
✅ executionTime < 120s
✅ finalResponse.Message.Length > 500
✅ finalResponse.Message.Contains("##") // Markdown headers
✅ Contains AiResponseStatus.Planning
✅ Contains AiResponseStatus.Completed
```

---

## 🚀 Come Eseguire

### Prerequisiti

```bash
# Configure Azure OpenAI credentials
cd src\AI\Test\Rystem.PlayFramework.Test
dotnet user-secrets set "OpenAi:ApiKey" "your-key"
dotnet user-secrets set "OpenAi:EndpointBase" "your-resource-name"
dotnet user-secrets set "OpenAi:ModelName" "gpt-4"
```

### Eseguire Test Singolo

```bash
# Test 1: Quarterly Analysis (Most comprehensive)
dotnet test --filter "FullyQualifiedName~Advanced_QuarterlySalesAnalysis_WithWeatherCorrelation"

# Test 5: Maximum Complexity (Uses all 6 scenes)
dotnet test --filter "FullyQualifiedName~Advanced_MaximumComplexity_AllScenesOrchestrated"
```

### Eseguire Tutti i Test Advanced

**⚠️ ATTENZIONE: Rimuovi Skip prima di eseguire**

1. Apri `AdvancedIntegrationTests.cs`
2. Rimuovi `Skip = "..."` dai test desiderati
3. Esegui:

```bash
dotnet test --filter "FullyQualifiedName~AdvancedIntegrationTests"
```

**Stima costi** (GPT-4):
- Test singolo: $0.02-0.05
- Tutti e 5 i test: $0.15-0.30

## 📊 Comparazione Complessità

| Test File | Test Count | Scenes | Avg Tools | Complexity | Cost Est. |
|-----------|------------|--------|-----------|------------|-----------|
| **SimpleCalculatorTests** | 4 | 1 | 2-3 | ⭐ Low | $0.01 |
| **MultiSceneTests** | 10 (6 integration) | 4 | 5-8 | ⭐⭐⭐ Medium | $0.05 |
| **InteractiveMultiSceneDemo** | 6 demos | 4 | 8-12 | ⭐⭐⭐⭐ High | $0.08 |
| **AdvancedIntegrationTests** | 5 | 6 | 15-30 | ⭐⭐⭐⭐⭐ Extreme | $0.20 |

## 🎯 Capacità Dimostrate

### ✅ Advanced Features

1. **6-Scene Orchestration** - Tutte le scene coordinate in un unico workflow
2. **Conditional Branching** - Logica if/else basata su dati runtime
3. **Business Intelligence** - KPI, growth rates, trends, forecasting
4. **Cross-Scene Data Flow** - Risultati propagati tra scene diverse
5. **Professional Report Generation** - Output executive-quality
6. **Weather Correlation Analysis** - Cross-domain data analysis
7. **Strategic Recommendations** - AI-driven business insights
8. **Complex Planning** - Multi-step workflows con 15-30 operazioni

### 📈 Business Use Cases Reali

- **Quarterly Business Reviews** (QBR)
- **Year-over-Year Performance Analysis**
- **Regional Performance Comparison**
- **Automated Decision Support Systems**
- **Executive Dashboard Data Preparation**
- **Market Trend Analysis**
- **Sales Forecasting**
- **Climate Impact Studies**

## 🔬 Technical Highlights

### Mock Services Implementation

#### MockSalesService
```csharp
- 2023 monthly data (12 months)
- 2024 YTD data (6 months with projections)
- Realistic growth patterns (~16% YoY)
- Top products catalog
```

#### MockBusinessAnalyticsService
```csharp
- Growth rate calculation with validation
- Percentage calculations
- Trend detection (upward/downward/flat)
- Statistical context
```

### Performance Considerations

- **Planning**: Enabled by default per gestire complessità
- **Summarization**: Disabled (threshold basso per test)
- **Caching**: Disabled (evitare interferenze tra test)
- **MaxRecursionDepth**: 3 (sufficiente per questi workflow)

## 🎓 Lessons Learned

### Best Practices per Test Complessi

1. **Descrizioni Chiare delle Scene** - LLM necessita contesto preciso
2. **Tool Naming Coerente** - Verbi + sostantivi descrittivi
3. **Mock Data Realistici** - Dati coerenti facilitano reasoning LLM
4. **Assertions Flessibili** - `>= N` invece di `== N` per tool calls
5. **Timeout Generosi** - 120s per workflow complessi
6. **Logging Strategico** - Solo eventi chiave per evitare clutter

### Pattern Architetturali

```
Single Scene (Calculator)
  ↓
Multi-Scene (4 scenes)
  ↓
Enhanced Multi-Scene (6 scenes)
  ↓
Complex Workflows (conditional logic)
  ↓
Maximum Complexity (all scenes + planning + 30 tools)
```

## 📚 Related Documentation

- **[MULTI_SCENE_TESTING.md](MULTI_SCENE_TESTING.md)** - Test multi-scene intermedi
- **[TEST_SUITE_SUMMARY.md](TEST_SUITE_SUMMARY.md)** - Overview completo tutti i test
- **[README_TESTING.md](README_TESTING.md)** - Setup e configurazione
- **[TOOL_CALLING.md](../../Rystem.PlayFramework/TOOL_CALLING.md)** - Architettura tool calling

---

**Status**: ✅ Build successful, 5 tests created, all skipped by default  
**Total Test Suite**: Now **38 tests** (16 unit, 22 integration/demo)  
**Framework Version**: Rystem.PlayFramework 1.0.0  
**Created**: January 2025
