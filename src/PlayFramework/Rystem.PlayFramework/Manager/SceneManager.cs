using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework;

namespace Rystem.PlayFramework
{
    internal sealed class SceneManager : ISceneManager
    {
        private readonly HttpContext? _httpContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly IFactory<IChatClient> _openAiFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SceneManagerSettings? _settings;

        public SceneManager(IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor, IFactory<IChatClient> openAiFactory, IHttpClientFactory httpClientFactory, SceneManagerSettings? settings = null)
        {
            _httpContext = httpContextAccessor?.HttpContext;
            _serviceProvider = serviceProvider;
            _openAiFactory = openAiFactory;
            _httpClientFactory = httpClientFactory;
            _settings = settings;
        }
        public async IAsyncEnumerable<AiSceneResponse> ExecuteAsync(string message, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var openAi = _openAiFactory.Create(_settings?.OpenAi.Name)!;
            var request = openAi.AddSystemMessage($"Oggi è {DateTime.UtcNow}.");
            foreach (var function in ScenesBuilderHelper.ScenesAsFunctions)
            {
                function.Invoke(request);
            }
            request.AddUserMessage(message);
            var response = await request.ExecuteAsync(cancellationToken);
            if (response.NeedFunctionCallOrToolCall)
            {
                foreach (var toolCall in response.ToolCalls!)
                {
                    yield return new AiSceneResponse
                    {
                        Name = toolCall.FunctionName,
                        Message = "Starting"
                    };
                    var scene = _serviceProvider.GetKeyedService<IScene>(toolCall.FunctionName);
                    if (scene != null)
                    {
                        await foreach (var sceneResponse in GetResponseFromSceneAsync(scene, message))
                            yield return sceneResponse;
                    }
                }
            }
            else
            {
                yield return new AiSceneResponse
                {
                    Message = response.FullText
                };
            }
        }
        private async IAsyncEnumerable<AiSceneResponse> GetResponseFromSceneAsync(IScene scene, string message, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var openAi = _openAiFactory.Create(scene.OpenAiFactoryName)!;
            var request = openAi.AddSystemMessage($"Oggi è {DateTime.UtcNow}.");
            var actorBuilder = new StringBuilder();
            if (scene is Scene internalScene)
                actorBuilder.Append(internalScene.SimpleActors);
            foreach (var actor in _serviceProvider.GetKeyedServices<IActor>(scene.Name))
            {
                actorBuilder.Append($"{await actor.GetMessageAsync()}.\n");
            }
            foreach (var function in ScenesBuilderHelper.FunctionsForEachScene[scene.Name].Functions)
            {
                function.Invoke(request);
            }
            request.AddUserMessage($"{actorBuilder}\n{message}");
            var response = await request.ExecuteAsync(cancellationToken);
            await foreach (var result in GetResponseAsync(scene.Name, scene.HttpClientName, request, response))
            {
                yield return result;
            }
        }
        private async IAsyncEnumerable<AiSceneResponse> GetResponseAsync(string sceneName, string? clientName, IChatClient chatClient, ChatResponse chatResponse)
        {
            if (chatResponse.NeedFunctionCallOrToolCall)
            {
                foreach (var toolCall in chatResponse!.ToolCalls!)
                {
                    using var streamReader = new StreamReader(toolCall.Entity.ToStream());
                    var json = await streamReader.ReadToEndAsync();
                    var functionName = toolCall.FunctionName;
                    var responseAsJson = string.Empty;
                    if (ScenesBuilderHelper.HttpCalls.ContainsKey(functionName))
                    {
                        responseAsJson = await ExecuteHttpClientAsync(clientName, functionName, json);
                    }
                    else if (ScenesBuilderHelper.ServiceCalls.ContainsKey(functionName))
                    {
                        responseAsJson = await ExecuteServiceAsync(functionName, json);
                    }
                    yield return new AiSceneResponse
                    {
                        Name = sceneName,
                        FunctionName = functionName,
                        Arguments = json.ToJson(),
                        Response = responseAsJson,
                    };
                    chatClient.AddSystemMessage($"Response for function {functionName}: {responseAsJson}");
                }
            }
            chatResponse = await chatClient.ExecuteAsync(default);
            if (chatResponse.NeedFunctionCallOrToolCall)
            {
                await foreach (var result in GetResponseAsync(sceneName, clientName, chatClient, chatResponse))
                {
                    yield return result;
                }
            }
            else
                yield return new AiSceneResponse
                {
                    Name = sceneName,
                    Message = chatResponse.FullText
                };
        }
        private async Task<string?> ExecuteServiceAsync(string functionName, string argumentAsJson)
        {
            var json = ParseJson(argumentAsJson);
            var currentActions = ScenesBuilderHelper.ServiceActions[functionName];
            var serviceBringer = new ServiceBringer() { Parameters = [] };
            foreach (var input in currentActions)
            {
                await input.Value(json, serviceBringer);
            }
            var response = await ScenesBuilderHelper.ServiceCalls[functionName](_serviceProvider, serviceBringer);
            return response.ToJson();
        }
        private async Task<string?> ExecuteHttpClientAsync(string? clientName, string functionName, string argumentAsJson)
        {
            var uri = functionName.Replace("_", "/");
            var json = ParseJson(argumentAsJson);
            var httpBringer = new HttpBringer();
            await ScenesBuilderHelper.HttpCalls[functionName](httpBringer);
            var currentActions = ScenesBuilderHelper.HttpActions[functionName];
            foreach (var actions in currentActions)
            {
                await actions.Value(json, httpBringer);
            }
            using var httpClient = clientName == null ? _httpClientFactory.CreateClient() : _httpClientFactory.CreateClient(clientName);
            var message = new HttpRequestMessage
            {
                Content = httpBringer.BodyAsJson != null ? new StringContent(httpBringer.BodyAsJson, Encoding.UTF8, "application/json") : null,
                Headers = { { "Accept", "application/json" } },
                RequestUri = new Uri($"{httpClient.BaseAddress}{uri}{(httpBringer.Query != null ? (uri.Contains('?') ? $"&{httpBringer.Query}" : $"?{httpBringer.Query}") : string.Empty)}"),
                Method = new HttpMethod(httpBringer.Method)
            };
            var authorization = _httpContext?.Request?.Headers?.Authorization.ToString();
            if (authorization != null)
            {
                var bearer = authorization.Split(' ');
                if (bearer.Length > 1)
                    message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(bearer[0], bearer[1]);
            }
            var request = await httpClient.SendAsync(message);
            var responseString = await request.Content.ReadAsStringAsync();
            return responseString;
        }
        private static Dictionary<string, string> ParseJson(string json)
        {
            var result = new Dictionary<string, string>();
            using (var document = JsonDocument.Parse(json))
            {
                foreach (var element in document.RootElement.EnumerateObject())
                {
                    if (element.Value.ValueKind == JsonValueKind.Object || element.Value.ValueKind == JsonValueKind.Array)
                    {
                        result.Add(element.Name, element.Value.GetRawText());
                    }
                    else
                    {
                        result.Add(element.Name, element.Value.ToString());
                    }
                }
            }
            return result;
        }
    }
}
