using System.Diagnostics;

namespace Rystem.PlayFramework.Telemetry;

/// <summary>
/// Central ActivitySource for PlayFramework distributed tracing.
/// Provides standardized activity names and tag keys for observability.
/// </summary>
public static class PlayFrameworkActivitySource
{
    /// <summary>
    /// ActivitySource name for PlayFramework traces.
    /// </summary>
    public const string SourceName = "Rystem.PlayFramework";
    
    /// <summary>
    /// Current version of PlayFramework.
    /// </summary>
    public const string Version = "1.0.0";
    
    /// <summary>
    /// Main ActivitySource instance for creating activities.
    /// </summary>
    public static readonly ActivitySource Instance = new(SourceName, Version);
    
    /// <summary>
    /// Standard tag names for PlayFramework activities.
    /// Use these constants to ensure consistent tagging across the framework.
    /// </summary>
    public static class Tags
    {
        // Framework-level tags
        public const string FactoryName = "playframework.factory_name";
        public const string ServiceVersion = "playframework.version";
        
        // Scene tags
        public const string SceneName = "playframework.scene.name";
        public const string SceneDescription = "playframework.scene.description";
        public const string SceneMode = "playframework.scene.mode";
        public const string UserMessage = "playframework.scene.user_message";
        
        // Tool tags
        public const string ToolName = "playframework.tool.name";
        public const string ToolType = "playframework.tool.type";
        public const string ToolArguments = "playframework.tool.arguments";
        public const string ToolResult = "playframework.tool.result";
        public const string ServiceType = "playframework.tool.service_type";
        public const string MethodName = "playframework.tool.method_name";
        
        // Actor tags
        public const string ActorName = "playframework.actor.name";
        public const string ActorRole = "playframework.actor.role";
        
        // Cache tags
        public const string CacheHit = "playframework.cache.hit";
        public const string CacheKey = "playframework.cache.key";
        public const string CacheBehavior = "playframework.cache.behavior";
        
        // LLM tags
        public const string LlmProvider = "playframework.llm.provider";
        public const string LlmModel = "playframework.llm.model";
        public const string LlmPrompt = "playframework.llm.prompt";
        public const string LlmResponse = "playframework.llm.response";
        public const string TokensPrompt = "playframework.llm.tokens.prompt";
        public const string TokensCompletion = "playframework.llm.tokens.completion";
        public const string TokensTotal = "playframework.llm.tokens.total";
        
        // Cost tags
        public const string Cost = "playframework.cost.total";
        public const string CostCurrency = "playframework.cost.currency";
        
        // Planning tags
        public const string PlanEnabled = "playframework.planner.enabled";
        public const string PlanSteps = "playframework.planner.steps";
        
        // Summarization tags
        public const string SummaryEnabled = "playframework.summarizer.enabled";
        public const string SummaryLength = "playframework.summarizer.length";
        
        // Director tags
        public const string DirectorEnabled = "playframework.director.enabled";
        public const string DirectorDecision = "playframework.director.decision";
        
        // MCP tags
        public const string McpServerUrl = "playframework.mcp.server_url";
        public const string McpFactoryName = "playframework.mcp.factory_name";
        public const string McpToolCount = "playframework.mcp.tool_count";
        public const string McpResourceCount = "playframework.mcp.resource_count";
        public const string McpPromptCount = "playframework.mcp.prompt_count";
    }
    
    /// <summary>
    /// Standard event names for PlayFramework activities.
    /// </summary>
    public static class Events
    {
        // Scene events
        public const string SceneStarted = "scene.started";
        public const string SceneCompleted = "scene.completed";
        public const string SceneFailed = "scene.failed";
        public const string SceneResolved = "scene.resolved";
        
        // Tool events
        public const string ToolCalled = "tool.called";
        public const string ToolCompleted = "tool.completed";
        public const string ToolFailed = "tool.failed";
        
        // Cache events
        public const string CacheAccessed = "cache.accessed";
        public const string CacheStored = "cache.stored";
        
        // LLM events
        public const string LlmCallStarted = "llm.call_started";
        public const string LlmCallCompleted = "llm.call_completed";
        public const string LlmCallFailed = "llm.call_failed";
        
        // Planning events
        public const string PlanGenerated = "plan.generated";
        public const string PlanStepCompleted = "plan.step_completed";
        
        // Summarization events
        public const string SummarizationStarted = "summarization.started";
        public const string SummarizationCompleted = "summarization.completed";
        
        // Director events
        public const string DirectorDecisionMade = "director.decision_made";
        
        // MCP events
        public const string McpServerConnected = "mcp.server_connected";
        public const string McpToolsLoaded = "mcp.tools_loaded";
        public const string McpResourcesLoaded = "mcp.resources_loaded";
        public const string McpToolCalled = "mcp.tool_called";
        public const string McpToolCompleted = "mcp.tool_completed";
        public const string McpToolFailed = "mcp.tool_failed";
    }
    
    /// <summary>
    /// Activity names for different operations.
    /// </summary>
    public static class Activities
    {
        // Root activities
        public const string SceneManagerExecute = "SceneManager.Execute";
        
        // Scene activities
        public const string SceneResolve = "Scene.Resolve";
        public const string SceneExecute = "Scene.Execute";
        public const string SceneBuildContext = "Scene.BuildContext";
        
        // Tool activities
        public const string ToolExecute = "Tool.Execute";
        
        // Cache activities
        public const string CacheGet = "Cache.Get";
        public const string CacheSet = "Cache.Set";
        
        // Planning activities
        public const string PlanningGenerate = "Planning.Generate";
        public const string PlanningExecuteStep = "Planning.ExecuteStep";
        
        // Summarization activities
        public const string SummarizationSummarize = "Summarization.Summarize";
        
        // Director activities
        public const string DirectorMakeDecision = "Director.MakeDecision";
        
        // LLM activities
        public const string LlmCall = "LLM.Call";
        public const string LlmStreamingCall = "LLM.StreamingCall";
        
        // MCP activities
        public const string McpLoadTools = "MCP.LoadTools";
        public const string McpLoadResources = "MCP.LoadResources";
        public const string McpExecuteTool = "MCP.ExecuteTool";
        public const string McpToolExecute = "MCP.Tool.Execute";
    }
}
