namespace Rystem.PlayFramework;

/// <summary>
/// Service for JSON serialization and deserialization.
/// </summary>
public interface IJsonService
{
    /// <summary>
    /// Serializes an object to JSON string.
    /// </summary>
    /// <typeparam name="T">Type of the object.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <returns>JSON string representation.</returns>
    string Serialize<T>(T value);

    /// <summary>
    /// Serializes an object to JSON string.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="type">Type of the object.</param>
    /// <returns>JSON string representation.</returns>
    string Serialize(object? value, Type type);

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    /// <typeparam name="T">Type of the object.</typeparam>
    /// <param name="json">JSON string.</param>
    /// <returns>Deserialized object.</returns>
    T? Deserialize<T>(string json);

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    /// <param name="json">JSON string.</param>
    /// <param name="type">Type of the object.</param>
    /// <returns>Deserialized object.</returns>
    object? Deserialize(string json, Type type);
}
