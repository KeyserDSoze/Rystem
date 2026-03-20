namespace Rystem.PlayFramework;

/// <summary>
/// Internal metadata attached to scene tools for discovery and forced-tool filtering.
/// </summary>
internal interface ISceneToolMetadata
{
    PlayFrameworkToolSourceType SourceType { get; }
    string? SourceName { get; }
    string? MemberName { get; }
    bool IsCommand { get; }
    string? JsonSchema { get; }
}
