namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for PlayFrameworkBuilder to enable repository-based conversation persistence.
/// </summary>
public static class PlayFrameworkBuilder_Repository
{
    /// <summary>
    /// Enables repository-based persistence for conversations.
    /// Requires IRepository&lt;StoredConversation, string&gt; to be registered with the same factory name.
    /// </summary>
    /// <param name="builder">PlayFramework builder.</param>
    /// <returns>Builder for chaining.</returns>
    /// <remarks>
    /// Repository must be registered separately using:
    /// <code>
    /// services.AddRepository&lt;StoredConversation, string, MyRepository&gt;(
    ///     repositoryBuilder => { /* configure */ },
    ///     name: "myFactoryName"
    /// );
    /// </code>
    /// The factory name must match the PlayFramework instance name.
    /// </remarks>
    public static PlayFrameworkBuilder UseRepository(this PlayFrameworkBuilder builder)
    {
        builder.HasRepository = true;
        return builder;
    }
}
