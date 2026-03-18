namespace Rystem.PlayFramework;

/// <summary>
/// Service for calculating costs for audio operations (STT and TTS).
/// Pricing model differs from LLM tokens: STT is billed per minute, TTS per character.
/// </summary>
public interface IAudioCostCalculator
{
    /// <summary>Calculate STT cost based on audio duration in seconds.</summary>
    decimal CalculateStt(double durationSeconds);

    /// <summary>Calculate TTS cost based on the number of characters synthesized.</summary>
    decimal CalculateTts(int charCount);

    /// <summary>Whether cost tracking is enabled.</summary>
    bool IsEnabled { get; }
}
