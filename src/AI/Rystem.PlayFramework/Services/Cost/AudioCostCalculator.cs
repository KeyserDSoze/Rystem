namespace Rystem.PlayFramework;

/// <summary>
/// Default implementation of <see cref="IAudioCostCalculator"/>.
/// STT is billed per minute, TTS per 1000 characters.
/// </summary>
internal sealed class AudioCostCalculator : IAudioCostCalculator
{
    private readonly AudioCostSettings _settings;

    public AudioCostCalculator(AudioCostSettings settings) => _settings = settings;

    public bool IsEnabled => _settings.Enabled;

    public decimal CalculateStt(double durationSeconds)
    {
        if (!_settings.Enabled || durationSeconds <= 0) return 0;
        return (decimal)(durationSeconds / 60.0) * _settings.SttCostPerMinute;
    }

    public decimal CalculateTts(int charCount)
    {
        if (!_settings.Enabled || charCount <= 0) return 0;
        return charCount / 1000m * _settings.TtsCostPerThousandChars;
    }
}
