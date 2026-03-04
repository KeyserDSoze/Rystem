using System.Text;

namespace Rystem.PlayFramework.Services;

/// <summary>
/// Accumulates streaming text chunks and yields complete sentences
/// suitable for TTS. Splits on sentence delimiters (. ! ? \n) while
/// respecting a minimum character threshold to avoid sending tiny fragments.
/// Forces a flush when the buffer exceeds a maximum size.
/// </summary>
internal sealed class SentenceAccumulator
{
    private readonly StringBuilder _buffer = new();
    private readonly HashSet<char> _delimiters;
    private readonly int _minChars;
    private readonly int _maxChars;

    public SentenceAccumulator(VoiceSettings settings)
    {
        _delimiters = [.. settings.SentenceDelimiters];
        _minChars = settings.MinCharsBeforeTts;
        _maxChars = settings.MaxCharsBeforeTts;
    }

    /// <summary>
    /// Appends a text chunk. Returns a sentence if the buffer contains enough
    /// text and ends with a delimiter, or if maximum size is exceeded.
    /// Returns null if more tokens are needed.
    /// </summary>
    public string? Append(string chunk)
    {
        _buffer.Append(chunk);

        // Force flush if buffer exceeds max size
        if (_buffer.Length >= _maxChars)
        {
            return Flush();
        }

        // Check if the buffer ends with a delimiter and is long enough
        if (_buffer.Length >= _minChars)
        {
            // Look for the last delimiter in the buffer
            for (var i = _buffer.Length - 1; i >= _minChars - 1; i--)
            {
                if (_delimiters.Contains(_buffer[i]))
                {
                    // Extract up to and including the delimiter
                    var sentence = _buffer.ToString(0, i + 1).Trim();
                    _buffer.Remove(0, i + 1);
                    if (sentence.Length > 0)
                        return sentence;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns any remaining text in the buffer without requiring a delimiter.
    /// Call this when the streaming response is complete.
    /// </summary>
    public string? Flush()
    {
        if (_buffer.Length == 0)
            return null;

        var remaining = _buffer.ToString().Trim();
        _buffer.Clear();
        return remaining.Length > 0 ? remaining : null;
    }
}
