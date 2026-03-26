using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for PlayFrameworkBuilder to enable repository-based conversation persistence.
/// </summary>
public static class PlayFrameworkBuilder_Repository
{
    /// <summary>
    /// Enables repository-based persistence for conversations.
    /// Requires <see cref="IRepository{StoredConversation, StoredConversationKey}"/> to be registered separately
    /// with the same factory name as the PlayFramework instance.
    /// </summary>
    public static PlayFrameworkBuilder UseRepository(this PlayFrameworkBuilder builder)
    {
        builder.HasRepository = true;
        return builder;
    }

    /// <summary>
    /// Enables repository-based persistence for conversations and configures the underlying
    /// <see cref="IRepository{StoredConversation, StoredConversationKey}"/> inline.
    /// The PlayFramework factory name is automatically injected as the first argument of the
    /// configure action so backends (e.g. <c>WithInMemory(name: name)</c>) resolve correctly.
    /// </summary>
    /// <param name="builder">PlayFramework builder.</param>
    /// <param name="configure">
    /// Action that receives <c>(string? factoryName, IRepositoryBuilder&lt;StoredConversation, StoredConversationKey&gt;)</c>.
    /// Pass <paramref name="factoryName"/> to the backend registration, e.g.:
    /// <code>
    /// .UseRepository((name, repo) => repo.WithInMemory(name: name))
    /// </code>
    /// </param>
    public static PlayFrameworkBuilder UseRepository(
        this PlayFrameworkBuilder builder,
        Action<string?, IRepositoryBuilder<StoredConversation, StoredConversationKey>> configure)
    {
        builder.HasRepository = true;
        var name = builder.Name?.ToString();
        builder.Services.AddRepository<StoredConversation, StoredConversationKey>(
            repoBuilder => configure(name, repoBuilder));
        return builder;
    }
}

