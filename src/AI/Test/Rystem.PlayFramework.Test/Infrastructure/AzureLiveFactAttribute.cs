namespace Rystem.PlayFramework.Test;

/// <summary>
/// Skips the decorated live test unless <c>RYSTEM_RUN_AZURE_OPENAI_INTEGRATION=1</c> is set and every
/// required configuration value is present, so an incomplete live setup produces a skipped test instead
/// of a failed one (the <see cref="PlayFrameworkTestBase"/> constructor throws <see cref="InvalidOperationException"/>
/// on missing configuration and is not itself skippable by xUnit).
/// </summary>
internal sealed class AzureLiveFactAttribute : FactAttribute
{
    private readonly string[] _requiredEnvironmentVariables;
    private bool _skipComputed;
    private string? _computedSkip;

    /// <summary>
    /// Whether this suite requires <c>AZURE_OPENAI_API_KEY</c> (or the <c>OpenAi:ApiKey</c> user secret).
    /// Set to <c>false</c> for Entra-only suites, whose test class is constructed with
    /// <c>useAzureCredential: true</c> and never needs an API key. Defaults to <c>true</c>.
    /// </summary>
    public bool RequiresApiKey { get; set; } = true;

    /// <summary>
    /// Whether the test class uses the default Responses deployment. Set to <c>false</c> only when
    /// the class passes a specialized deployment variable to <see cref="PlayFrameworkTestBase"/>.
    /// </summary>
    public bool RequiresDefaultDeployment { get; set; } = true;

    public AzureLiveFactAttribute(params string[] requiredEnvironmentVariables)
    {
        _requiredEnvironmentVariables = requiredEnvironmentVariables;
    }

    /// <remarks>
    /// Overridden (rather than computed in the constructor) so that the named configuration
    /// properties, applied by the compiler after the constructor runs, are already set when evaluated.
    /// </remarks>
    public override string? Skip
    {
        get
        {
            if (!_skipComputed)
            {
                _computedSkip = ComputeSkip();
                _skipComputed = true;
            }
            return _computedSkip;
        }
        set => _computedSkip = value;
    }

    private string? ComputeSkip() =>
        AzureLiveTestConfiguration.ComputeSkipReason(
            RequiresApiKey,
            RequiresDefaultDeployment,
            _requiredEnvironmentVariables);
}
