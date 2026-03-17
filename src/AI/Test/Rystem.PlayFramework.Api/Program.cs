using Rystem.PlayFramework;
using Rystem.PlayFramework.Adapters;
using Rystem.PlayFramework.Adapters.FoundryLocal;
using Rystem.PlayFramework.Api;
using Rystem.PlayFramework.Api.Infrastructure;
using Rystem.PlayFramework.Api.Models;
using Rystem.PlayFramework.Api.Services;
using Microsoft.Extensions.AI;
using Scalar.AspNetCore;
using RepositoryFramework.InMemory;

var builder = WebApplication.CreateBuilder(args);

// Configure detailed logging to console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug); // Show all logs including Debug

// Configure CORS for TypeScript client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// In-memory distributed cache (required for OnClient continuation tokens)
builder.Services.AddMemoryCache();
builder.Services.AddRepository<StoredConversation, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(inMemoryOptions =>
    {
    }, "default");
});
builder.Services.AddOpenApi();

// Register Calculator Service (used by Calculator scene)
builder.Services.AddSingleton<ICalculatorService, CalculatorService>();

// Register Shape Service (used by AnyOf repro scene)
builder.Services.AddSingleton<IShapeService, ShapeService>();

// Register User Lookup Service (used to reproduce nullable-Guid deserialization)
builder.Services.AddSingleton<IUserLookupService, UserLookupService>();

// Configure Azure OpenAI (from user secrets or appsettings)
var azureOpenAIEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var azureOpenAIKey = builder.Configuration["AzureOpenAI:Key"];
var azureOpenAIDeployment = builder.Configuration["AzureOpenAI:Deployment"] ?? "gpt-4o";

if (!string.IsNullOrEmpty(azureOpenAIEndpoint) && !string.IsNullOrEmpty(azureOpenAIKey))
{
    builder.Services.AddAdapterForAzureOpenAI("default", settings =>
    {
        settings.Endpoint = new Uri(azureOpenAIEndpoint);
        settings.ApiKey = azureOpenAIKey;
        settings.Deployment = azureOpenAIDeployment;
    });

    // Register voice adapter (STT + TTS) — reuses the same Azure OpenAI endpoint & key
    var voiceSttDeployment = builder.Configuration["AzureOpenAI:Voice:SttDeployment"] ?? "whisper";
    var voiceTtsDeployment = builder.Configuration["AzureOpenAI:Voice:TtsDeployment"] ?? "tts-1";
    var voiceTtsVoice = builder.Configuration["AzureOpenAI:Voice:TtsVoice"] ?? "alloy";
    var voiceTtsFormat = builder.Configuration["AzureOpenAI:Voice:TtsOutputFormat"] ?? "mp3";
    var voiceTtsSpeed = float.TryParse(builder.Configuration["AzureOpenAI:Voice:TtsSpeed"], out var speed) ? speed : 1.0f;

    builder.Services.AddVoiceAdapterForAzureOpenAI("default", voiceSettings =>
    {
        voiceSettings.Endpoint = new Uri(azureOpenAIEndpoint);
        voiceSettings.ApiKey = azureOpenAIKey;
        voiceSettings.SttDeployment = voiceSttDeployment;
        voiceSettings.TtsDeployment = voiceTtsDeployment;
        voiceSettings.TtsVoice = voiceTtsVoice;
        voiceSettings.TtsOutputFormat = voiceTtsFormat;
        voiceSettings.TtsSpeed = voiceTtsSpeed;
    });
}
else
{
    // Fallback to mock implementation for demo purposes
    builder.Services.AddSingleton<IChatClient>(new MockChatClient());
}

//// ── Foundry Local adapter (local AI model) ─────────────────────────────
//var foundryModel = builder.Configuration["FoundryLocal:Model"] ?? "phi-4-mini";
//var foundryUrl = builder.Configuration["FoundryLocal:WebServiceUrl"] ?? "http://127.0.0.1:5272";

//builder.Services.AddAdapterForFoundryLocal("foundry", settings =>
//{
//    settings.Model = foundryModel;
//    settings.WebServiceUrl = foundryUrl;
//    settings.AppName = "Rystem.PlayFramework.Test";
//});

// Configure PlayFramework with Chat scene
builder.Services.AddPlayFramework("default", frameworkBuilder =>
{
    frameworkBuilder
        .WithVoice("default") // Enable voice pipeline (STT → PlayFramework → TTS)
        .UseDefaultGuardrails()
        .AddCache(cacheBuilder =>
        {
            cacheBuilder
                .WithMemory()
                .WithExpiration(TimeSpan.FromMinutes(30));
        })
        .UseRepository()
        .WithPlanning(planningSettings =>
        {
            planningSettings.MaxRecursionDepth = 5;
        })
        .WithRetry(maxAttempts: 3, baseDelaySeconds: 1.0)
        .WithTelemetry(telemetryBuilder =>
        {
            telemetryBuilder.EnableMetrics = true;
            telemetryBuilder.TraceSummarization = true;
            telemetryBuilder.TraceDirector = true;
            telemetryBuilder.TraceLlmCalls = true;
        })
        .AddMainActor("You are a helpful AI assistant. You help users with their questions and tasks in a friendly and professional manner.")
        .AddScene("General Requests", "Use this scene for every request. General conversation and question answering. ", sceneBuilder =>
            {
                sceneBuilder
                    .WithDescriptionFromTools()
                    .WithActors(actorBuilder =>
                    {
                        actorBuilder
                            .AddActor("Provide clear, concise, and accurate answers.")
                            .AddActor("Be friendly and engaging in conversation.")
                            .AddActor("If you don't know something, admit it honestly.");
                    })
                    .OnClient(clientBuilder =>
                    {
                        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                        // CLIENT TOOLS (require response — AwaitingClient)
                        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                        clientBuilder
                            .AddTool("getCurrentLocation",
                                "Gets the user's current geographic location (latitude, longitude) from the browser. ALWAYS call this tool when the user mentions 'dove sono', 'posizione', 'location', 'where am I', nearby places, weather, or anything location-dependent.",
                                timeoutSeconds: 15)
                            .AddTool<UserConfirmationArgs>("getUserConfirmation",
                                "Asks the user for explicit yes/no confirmation via a browser dialog. ALWAYS call this tool when the user says 'conferma', 'confirm', 'chiedi conferma', or asks you to verify before doing something.",
                                timeoutSeconds: 60)

                            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                            // COMMANDS (fire-and-forget — CommandClient)
                            // Three feedback modes: Never, OnError, Always
                            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                            .AddCommand<LogActionArgs>("logUserAction",
                                "Logs a user action silently in the browser console for debugging. No feedback is sent back. ALWAYS call this when the user says 'logga', 'log this', or 'registra azione'.",
                                feedbackMode: CommandFeedbackMode.Never,
                                timeoutSeconds: 5)
                            .AddCommand<TrackEventArgs>("trackAnalytics",
                                "Tracks an analytics event on the client. Feedback is sent only if tracking fails. ALWAYS call this when the user says 'traccia', 'track', 'analytics', or 'registra evento'.",
                                feedbackMode: CommandFeedbackMode.OnError,
                                timeoutSeconds: 10)
                            .AddCommand<SaveDataArgs>("saveToLocalStorage",
                                "Saves a key-value pair to browser localStorage. ALWAYS sends confirmation back. ALWAYS call this when the user says 'salva', 'save', 'ricorda', 'memorizza', or asks to store/remember a value.",
                                feedbackMode: CommandFeedbackMode.Always,
                                timeoutSeconds: 10)
                            .AddCommand<NotificationArgs>("showNotification",
                                "Shows a visual notification/alert in the browser to the user. Feedback is sent only if displaying fails. ALWAYS call this when the user says 'notifica', 'notify', 'avvisa', 'mostra notifica', or 'alert'.",
                                feedbackMode: CommandFeedbackMode.OnError,
                                timeoutSeconds: 10);
                    });
            })
        .AddScene("Calculator", "Use this scene for mathematical calculations and arithmetic operations (addition, subtraction, multiplication, division).", sceneBuilder =>
        {
            sceneBuilder
                .WithDescriptionFromTools()
                .WithActors(actorBuilder =>
                {
                    actorBuilder
                        .AddActor("Perform accurate calculations using available mathematical operations.")
                        .AddActor("Show the calculation steps clearly to the user.")
                        .AddActor("Handle division by zero gracefully and explain the error.");
                })
                .WithService<ICalculatorService>(serviceBuilder =>
                {
                    serviceBuilder
                        .WithMethod<double>(x => x.Add(default, default), "Add", "Adds two numbers together. Use for addition operations (e.g., '5 plus 3', '5+3').")
                        .WithMethod<double>(x => x.Subtract(default, default), "Subtract", "Subtracts the second number from the first. Use for subtraction operations (e.g., '5 minus 3', '5-3').")
                        .WithMethod<double>(x => x.Multiply(default, default), "Multiply", "Multiplies two numbers together. Use for multiplication operations (e.g., '5 times 7.69', '5*7.69', '5 per 7.69' when 'per' means multiplication).")
                        .WithMethod<double>(x => x.Divide(default, default), "Divide", "Divides the first number by the second. Use for division operations (e.g., '5 divided by 7.69', '5/7.69', '5 diviso 7.69').");
                });
        })
        .AddScene("Shape Operations",
            "Use this scene for any geometry or shape-related request: describing shapes, calculating areas/perimeters, " +
            "or querying shape info. Keywords: 'shape', 'circle', 'rectangle', 'triangle', 'area', 'perimeter', " +
            "'forma', 'cerchio', 'rettangolo', 'triangolo', 'area', 'perimetro', 'describe shape', 'calculate area'.",
            sceneBuilder =>
        {
            sceneBuilder
                .WithDescriptionFromTools()
                .WithActors(actorBuilder =>
                {
                    actorBuilder
                        .AddActor("Describe shapes clearly and accurately.")
                        .AddActor("Calculate areas using the correct formula for each shape.")
                        .AddActor("When the user provides a shape identifier it can be either a string name or a numeric code (1=circle, 2=rectangle, 3=triangle).");
                })
                .WithService<IShapeService>(serviceBuilder =>
                {
                    serviceBuilder
                        .WithMethod<string>(x => x.DescribeShape(default!), "DescribeShape",
                            "Describes a shape by its name (e.g. 'circle') or numeric code (1=circle, 2=rectangle, 3=triangle).")
                        .WithMethod<double>(x => x.CalculateArea(default!), "CalculateArea",
                            "Calculates the area of a shape. Provide either a CircleArgs (with Radius) or a RectangleArgs (with Width and Height).")
                        .WithMethod<string>(x => x.GetShapeInfo(default!, default), "GetShapeInfo",
                            "Returns a full description of a shape plus optional area override. Shape identifier can be a name or numeric code; area override can be a number or a label string.");
                });
        })
        .AddScene("User Lookup",
            "Use this scene to retrieve user data or contract information. " +
            "Keywords: 'user', 'utente', 'chi sono', 'contratto', 'contract', 'dati utente', 'user data', 'user info', " +
            "'trova utente', 'find user', 'mostra contratto', 'show contract'.",
            sceneBuilder =>
        {
            sceneBuilder
                .WithDescriptionFromTools()
                .WithActors(actorBuilder =>
                {
                    actorBuilder
                        .AddActor("Look up user data and contract info accurately using the available tools.")
                        .AddActor(
                            "Both userId and contractId parameters are optional Guids. " +
                            "When the user explicitly asks for anonymous/default data, call the method with null (pass null explicitly). " +
                            "When the user provides a Guid string, pass it as the userId or contractId. " +
                            "Known test users: '11111111-1111-1111-1111-111111111111' (Alice Rossi, Admin), '22222222-2222-2222-2222-222222222222' (Bob Bianchi, User). " +
                            "Known test contracts: 'aaaa0000-0000-0000-0000-000000000001' (Alice), 'bbbb0000-0000-0000-0000-000000000002' (Bob).");
                })
                .WithService<IUserLookupService>(serviceBuilder =>
                {
                    serviceBuilder
                        .WithMethod<UserData>(
                            x => x.GetUserData(null),
                            "GetUserData",
                            "Returns data for a user given an optional userId (Guid). If not provided, returns anonymous session data.")
                        .WithMethod<ContractInfo>(
                            x => x.GetUserContract(null, null),
                            "GetUserContract",
                            "Returns the contract for a user. Both userId and contractId are optional Guids. If omitted, the default public contract is returned.");
                });
        })
        .AddScene("Technical Documentation Estimator",
            "Use this scene when the user asks for a work estimate, effort estimation, project sizing, man-days calculation, " +
            "or sends technical documentation (requirements, specs, architecture docs, RFP, functional docs) and asks to evaluate " +
            "the effort, time, or cost. Keywords: 'stima', 'estimate', 'giornate uomo', 'man-days', 'effort', 'quanto ci vuole', " +
            "'sizing', 'valutazione tecnica', 'analisi requisiti'.",
            sceneBuilder =>
        {
            sceneBuilder
                .WithActors(actorBuilder =>
                {
                    actorBuilder
                        .AddActor(
                            """
                            You are a Senior Microsoft Technology Consultant & Project Estimator with 20+ years of experience
                            in estimating software projects based on Microsoft technologies (Azure, .NET, C#, SQL Server,
                            Power Platform, Dynamics 365, Microsoft 365, Teams, SharePoint, Entra ID, Blazor, MAUI, etc.).

                            When you receive technical documentation, you MUST:

                            1. **Analyze the document** — Identify all functional requirements, technical requirements,
                               integrations, and non-functional requirements (security, performance, scalability).

                            2. **Break down into work items** — Decompose each requirement into concrete development tasks
                               (analysis, design, implementation, testing, deployment, documentation).

                            3. **Estimate each item** — Provide estimates in man-days (1 man-day = 8 hours of work by 1 person).
                               Use a range (min–max) for each item to account for uncertainty.

                            4. **Produce the output as a Markdown table** with these columns:
                               | # | Area | Activity | Description | Min (days) | Max (days) | Notes |

                            5. **Add a summary section** at the bottom with:
                               - Total min and max man-days
                               - Recommended team composition (roles and seniority)
                               - Key risks and assumptions
                               - Suggested timeline (sprints/weeks)

                            Always assume the technology stack is Microsoft-based unless explicitly stated otherwise.
                            Be conservative in estimates — it's better to overestimate than underestimate.
                            Include time for: code review, testing (unit + integration), CI/CD setup, documentation.
                            If the document is vague, state your assumptions explicitly.
                            """)
                        .AddActor(
                            """
                            When presenting the estimation table, use clear Markdown formatting.
                            Group activities by functional area (e.g., "Authentication", "Data Layer", "API", "Frontend", "DevOps").
                            Always include these cross-cutting activities:
                            - Project setup & architecture design
                            - CI/CD pipeline configuration
                            - Environment setup (Dev, Staging, Production)
                            - Security review & hardening
                            - Performance testing
                            - Documentation & knowledge transfer
                            - Project management overhead (10-15% of total)
                            - Buffer for unknowns (15-20% of total)
                            """);
                });
        });
});

// Configure PlayFramework with Foundry Local (local model)
builder.Services.AddPlayFramework("foundry", frameworkBuilder =>
{
    frameworkBuilder
        .UseDefaultGuardrails()
        .AddCache(cacheBuilder =>
        {
            cacheBuilder
                .WithMemory()
                .WithExpiration(TimeSpan.FromMinutes(30));
        })
        .UseRepository()
        .WithPlanning(planningSettings =>
        {
            planningSettings.MaxRecursionDepth = 5;
        })
        .WithRetry(maxAttempts: 3, baseDelaySeconds: 1.0)
        .WithTelemetry(telemetryBuilder =>
        {
            telemetryBuilder.EnableMetrics = true;
            telemetryBuilder.TraceSummarization = true;
            telemetryBuilder.TraceDirector = true;
            telemetryBuilder.TraceLlmCalls = true;
        })
        .AddMainActor("You are a helpful AI assistant running on a local model via Foundry Local. You help users with their questions and tasks in a friendly and professional manner.")
        .AddScene("General Requests", "Use this scene for every request. General conversation and question answering.", sceneBuilder =>
        {
            sceneBuilder
                .WithDescriptionFromTools()
                .WithActors(actorBuilder =>
                {
                    actorBuilder
                        .AddActor("Provide clear, concise, and accurate answers.")
                        .AddActor("Be friendly and engaging in conversation.")
                        .AddActor("If you don't know something, admit it honestly.");
                });
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Servers = [new ScalarServer("http://localhost:5158", "Local Development")];
        options.Title = "PlayFramework API";
        options.Theme = ScalarTheme.BluePlanet;
    });
}

app.UseCors();
app.UseHttpsRedirection();

// Map PlayFramework HTTP endpoints
// POST /api/ai/{factoryName} - Step-by-step streaming (each PlayFramework step)
// POST /api/ai/{factoryName}/streaming - Token-level streaming (each text chunk)
app.MapPlayFramework("default", settings =>
{
    settings.BasePath = "/api/ai";
    settings.RequireAuthentication = false; // Set to true for production
    settings.EnableCompression = true;
    settings.EnableConversationEndpoints = true; // Enable conversation management endpoints
    settings.EnableVoiceEndpoints = true; // Enable voice pipeline endpoints (audio → STT → AI → TTS)
});

// Map Foundry Local PlayFramework endpoints (same base path, different factory name)
app.MapPlayFramework("foundry", settings =>
{
    settings.BasePath = "/api/ai";
    settings.RequireAuthentication = false;
    settings.EnableCompression = true;
    settings.EnableConversationEndpoints = true;
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck");

app.Run();

