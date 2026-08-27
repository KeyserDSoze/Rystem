namespace Rystem.PlayFramework.Test;

/// <summary>
/// <see cref="TheoryAttribute"/> counterpart of <see cref="AzureLiveFactAttribute"/>, for live gates
/// that need <c>[InlineData]</c> (e.g. exercising the same scenario against multiple inputs to rule out
/// a model producing a fixed/hallucinated answer regardless of the actual content).
/// See <see cref="AzureLiveFactAttribute"/> for the skip semantics; the logic is shared via
/// <see cref="AzureLiveTestConfiguration.ComputeSkipReason"/>.
/// </summary>
internal sealed class AzureLiveTheoryAttribute : TheoryAttribute
{
    private readonly string[] _requiredEnvironmentVariables;
    private bool _skipComputed;
    private string? _computedSkip;

    /// <inheritdoc cref="AzureLiveFactAttribute.RequiresApiKey"/>
    public bool RequiresApiKey { get; set; } = true;

    /// <inheritdoc cref="AzureLiveFactAttribute.RequiresDefaultDeployment"/>
    public bool RequiresDefaultDeployment { get; set; } = true;

    public AzureLiveTheoryAttribute(params string[] requiredEnvironmentVariables)
    {
        _requiredEnvironmentVariables = requiredEnvironmentVariables;
    }

    public override string? Skip
    {
        get
        {
            if (!_skipComputed)
            {
                _computedSkip = AzureLiveTestConfiguration.ComputeSkipReason(
                    RequiresApiKey,
                    RequiresDefaultDeployment,
                    _requiredEnvironmentVariables);
                _skipComputed = true;
            }
            return _computedSkip;
        }
        set => _computedSkip = value;
    }
}
