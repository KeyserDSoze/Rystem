using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for configuring voice pipeline on <see cref="PlayFrameworkBuilder"/>.
/// </summary>
public static class PlayFrameworkBuilder_Voice
{
    /// <summary>
    /// Enables the voice pipeline with default settings.
    /// Requires an <see cref="IVoiceAdapter"/> to be registered via
    /// <c>AddVoiceAdapterForAzureOpenAI</c> or similar.
    /// </summary>
    /// <param name="builder">PlayFramework builder.</param>
    /// <param name="voiceAdapterFactoryName">
    /// Factory name of the <see cref="IVoiceAdapter"/> to use.
    /// Must match the name used when registering the adapter
    /// (e.g., <c>AddVoiceAdapterForAzureOpenAI("voice", ...)</c>).
    /// If null, uses the default (unnamed) voice adapter.
    /// </param>
    /// <returns>Builder for chaining.</returns>
    public static PlayFrameworkBuilder WithVoice(
        this PlayFrameworkBuilder builder,
        AnyOf<string?, Enum>? voiceAdapterFactoryName = null)
    {
        builder.Settings.Voice.Enabled = true;
        builder.VoiceAdapterFactoryName = voiceAdapterFactoryName;
        return builder;
    }

    /// <summary>
    /// Enables the voice pipeline with custom settings.
    /// Requires an <see cref="IVoiceAdapter"/> to be registered via
    /// <c>AddVoiceAdapterForAzureOpenAI</c> or similar.
    /// </summary>
    /// <param name="builder">PlayFramework builder.</param>
    /// <param name="voiceAdapterFactoryName">
    /// Factory name of the <see cref="IVoiceAdapter"/> to use.
    /// </param>
    /// <param name="configure">Action to configure voice settings.</param>
    /// <returns>Builder for chaining.</returns>
    public static PlayFrameworkBuilder WithVoice(
        this PlayFrameworkBuilder builder,
        AnyOf<string?, Enum>? voiceAdapterFactoryName,
        Action<VoiceSettings> configure)
    {
        builder.Settings.Voice.Enabled = true;
        builder.VoiceAdapterFactoryName = voiceAdapterFactoryName;
        configure(builder.Settings.Voice);
        return builder;
    }
}
