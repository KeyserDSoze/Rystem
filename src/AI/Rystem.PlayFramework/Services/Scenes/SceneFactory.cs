using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework;

/// <summary>
/// Factory for creating scenes.
/// </summary>
internal sealed class SceneFactory : ISceneFactory, IFactoryName
{
    internal List<SceneConfiguration> _configurations = null!;
    private readonly IFactory<List<SceneConfiguration>> _sceneConfigurationFactory;

    public SceneFactory(IFactory<List<SceneConfiguration>> sceneConfigurationFactory)
    {
        _sceneConfigurationFactory = sceneConfigurationFactory;
    }
    public bool FactoryNameAlreadySetup { get; set; }
    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        _configurations = _sceneConfigurationFactory.Create(name) ?? [];
        var sceneNames = new List<string>();
        var scenes = new List<IScene>();
        var tools = new List<AITool>();
        if (_configurations != null)
        {
            foreach (var configuration in _configurations)
            {
                sceneNames.Add(configuration.Name);
                var currentScene = new Scene(configuration);
                scenes.Add(currentScene);
                tools.Add(currentScene.AiTool);
            }
        }
        SceneNames = sceneNames;
        Scenes = scenes;
        ScenesAsAiTool = tools;
    }
    public IReadOnlyList<string> SceneNames { get; private set; }
    public IReadOnlyList<IScene> Scenes { get; private set; }
    public IReadOnlyList<AITool> ScenesAsAiTool { get; private set; }
    public IScene? TryGetScene(string name)
        => Scenes.FirstOrDefault(x => x.Name == name);
}
