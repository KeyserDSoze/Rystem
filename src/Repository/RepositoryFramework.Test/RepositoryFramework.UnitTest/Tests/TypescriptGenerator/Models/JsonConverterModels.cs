using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RepositoryFramework.UnitTest.TypescriptGenerator.Models;

/// <summary>
/// Simulates a type with [JsonConverter] that serializes as a plain string.
/// The indexer (this[]) and implicit operator are irrelevant for TypeScript generation;
/// what matters is that the JsonConverter writes/reads a plain string.
/// </summary>
[JsonConverter(typeof(LocalizedFormatStringJsonConverter))]
public sealed class LocalizedFormatString
{
    public required string Value { get; init; }

    public string this[params object[] parameters]
    {
        get => string.Format(Value, parameters);
    }

    public static implicit operator LocalizedFormatString(string formattableString)
        => new() { Value = formattableString };
}

public sealed class LocalizedFormatStringJsonConverter : JsonConverter<LocalizedFormatString>
{
    public override LocalizedFormatString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string.");

        var stringValue = reader.GetString();
        return new LocalizedFormatString { Value = stringValue! };
    }

    public override void Write(Utf8JsonWriter writer, LocalizedFormatString value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

/// <summary>
/// Model that uses LocalizedFormatString as a property type.
/// </summary>
public class ChapterLocalization
{
    [JsonPropertyName("l")]
    public string? Label { get; set; }

    [JsonPropertyName("d")]
    public LocalizedFormatString? Description { get; set; }

    [JsonPropertyName("e")]
    public string? Evaluation { get; set; }
}

/// <summary>
/// Simulates a type with [JsonConverter] wrapping a numeric value.
/// </summary>
[JsonConverter(typeof(WrappedIntJsonConverter))]
public sealed class WrappedInt
{
    public required int Value { get; init; }
}

public sealed class WrappedIntJsonConverter : JsonConverter<WrappedInt>
{
    public override WrappedInt Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new WrappedInt { Value = reader.GetInt32() };
    }

    public override void Write(Utf8JsonWriter writer, WrappedInt value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

/// <summary>
/// Type with [JsonConverter] but multiple properties — should NOT be treated as primitive.
/// The converter calls WriteStartObject → IL analysis detects complex output.
/// </summary>
[JsonConverter(typeof(ComplexConverterType))]
public class MultiPropertyWithConverter
{
    public string? Name { get; set; }
    public int Age { get; set; }
}

public sealed class ComplexConverterType : JsonConverter<MultiPropertyWithConverter>
{
    public override MultiPropertyWithConverter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotImplementedException();

    public override void Write(Utf8JsonWriter writer, MultiPropertyWithConverter value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("n", value?.Name);
        writer.WriteNumber("a", value?.Age ?? 0);
        writer.WriteEndObject();
    }
}

/// <summary>
/// Type with [JsonConverter] that writes a string but has ZERO public properties.
/// Only detectable via IL analysis, not by property heuristic.
/// Proves the IL strategy works where the old heuristic would fail.
/// </summary>
[JsonConverter(typeof(OpaqueTokenJsonConverter))]
public sealed class OpaqueToken
{
    internal string _token = string.Empty;

    public OpaqueToken(string token) => _token = token;
}

public sealed class OpaqueTokenJsonConverter : JsonConverter<OpaqueToken>
{
    public override OpaqueToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetString() ?? string.Empty);

    public override void Write(Utf8JsonWriter writer, OpaqueToken value, JsonSerializerOptions options)
        => writer.WriteStringValue(value._token);
}

/// <summary>
/// Type with [JsonConverter] that writes a number but has multiple properties.
/// Only detectable via IL analysis.
/// </summary>
[JsonConverter(typeof(ScoreValueJsonConverter))]
public sealed class ScoreValue
{
    public int Points { get; init; }
    public string? Category { get; init; }
    public DateTime EarnedAt { get; init; }
}

public sealed class ScoreValueJsonConverter : JsonConverter<ScoreValue>
{
    public override ScoreValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new() { Points = reader.GetInt32() };

    public override void Write(Utf8JsonWriter writer, ScoreValue value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.Points);
}

/// <summary>
/// Type with [JsonConverter] that writes a boolean.
/// </summary>
[JsonConverter(typeof(FlagJsonConverter))]
public sealed class Flag
{
    public bool IsSet { get; init; }
    public string? Reason { get; init; }
}

public sealed class FlagJsonConverter : JsonConverter<Flag>
{
    public override Flag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new() { IsSet = reader.GetBoolean() };

    public override void Write(Utf8JsonWriter writer, Flag value, JsonSerializerOptions options)
        => writer.WriteBooleanValue(value.IsSet);
}
