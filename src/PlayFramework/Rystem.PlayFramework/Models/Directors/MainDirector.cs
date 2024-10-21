namespace Rystem.PlayFramework
{
    internal sealed class MainDirector : IDirector
    {
        public async Task<DirectorResponse> DirectAsync(SceneContext context, CancellationToken cancellationToken)
        {
            if (context.CreateNewDefaultChatClient != null)
            {
                var lastMessage = context.Responses.Where(x => x.Message != null).LastOrDefault()?.Message;
                if (lastMessage != null)
                {
                    var chatClient = context.CreateNewDefaultChatClient();
                    chatClient.AddSystemMessage("I'm putting in assistant message the last message you provide for a user request. You need only understand if assistant has responded to the user or not. If it does respond with the word 'Yes' otherwise with the word 'No'. Do not provide further information, only these two words are allowed responses.")
                        .AddAssistantMessage(lastMessage)
                        .AddUserMessage(context.InputMessage);
                    var response = await chatClient.ExecuteAsync(cancellationToken);
                    return new DirectorResponse
                    {
                        CutScenes = context.Responses.Where(x => x.Name != null).Select(x => x.Name!).Distinct().ToList(),
                        ExecuteAgain = response.FullText?.ToLower() == No
                    };
                }
            }
            return new DirectorResponse
            {
                ExecuteAgain = false
            };
        }
        private const string No = "no";
    }
}
