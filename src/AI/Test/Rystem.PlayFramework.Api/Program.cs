using Rystem.PlayFramework;
using Rystem.PlayFramework.Api;
using Rystem.PlayFramework.Api.Infrastructure;
using Rystem.PlayFramework.Api.Models;
using Rystem.PlayFramework.Api.Services;
using Microsoft.Extensions.AI;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configure detailed logging to console
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

builder.Services.AddOpenApi();

// Register Calculator Service (used by Calculator scene)
builder.Services.AddSingleton<ICalculatorService, CalculatorService>();

// Configure Azure OpenAI (from user secrets or appsettings)
var azureOpenAIEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var azureOpenAIKey = builder.Configuration["AzureOpenAI:Key"];
var azureOpenAIDeployment = builder.Configuration["AzureOpenAI:Deployment"] ?? "gpt-4o";

if (!string.IsNullOrEmpty(azureOpenAIEndpoint) && !string.IsNullOrEmpty(azureOpenAIKey))
{
    builder.Services.AddSingleton<IChatClient>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<AzureOpenAIChatClientAdapter>>();
        return new AzureOpenAIChatClientAdapter(azureOpenAIEndpoint, azureOpenAIKey, azureOpenAIDeployment, logger);
    });
}
else
{
    // Fallback to mock implementation for demo purposes
    builder.Services.AddSingleton<IChatClient>(new MockChatClient());
}

// Configure PlayFramework with Chat scene
builder.Services.AddPlayFramework("default", frameworkBuilder =>
{
    frameworkBuilder
        .AddCache(cacheBuilder =>
        {
            cacheBuilder
                .WithMemory()
                .WithExpiration(TimeSpan.FromMinutes(30));
        })
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
                        clientBuilder
                            .AddTool("getCurrentLocation",
                                "Gets the user's current geographic location (latitude, longitude) from the browser. Use when the user asks about nearby places, weather, or anything location-dependent.",
                                timeoutSeconds: 15)
                            .AddTool<UserConfirmationArgs>("getUserConfirmation",
                                "Asks the user for explicit confirmation before performing a sensitive or irreversible action. Use when the user requests something that needs a yes/no decision.",
                                timeoutSeconds: 60);
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
                        .WithMethod<double>(x => x.Add(default, default), "Add", "Adds two numbers together.")
                        .WithMethod<double>(x => x.Subtract(default, default), "Subtract", "Subtracts the second number from the first.")
                        .WithMethod<double>(x => x.Multiply(default, default), "Multiply", "Multiplies two numbers together.")
                        .WithMethod<double>(x => x.Divide(default, default), "Divide", "Divides the first number by the second.");
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
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck");

app.Run();
