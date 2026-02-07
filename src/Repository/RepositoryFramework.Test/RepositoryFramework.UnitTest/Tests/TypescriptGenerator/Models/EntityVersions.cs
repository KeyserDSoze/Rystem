using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RepositoryFramework.UnitTest.TypescriptGenerator.Models;

/// <summary>
/// Generic model for testing generic type generation.
/// </summary>
public class EntityVersions<T>
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("entity_id")]
    public string? EntityId { get; set; }

    [JsonPropertyName("versions")]
    public List<VersionEntry<T>>? Versions { get; set; }

    [JsonPropertyName("current")]
    public T? Current { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Nested generic type.
/// </summary>
public class VersionEntry<T>
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }
}

/// <summary>
/// Sample type to be used with EntityVersions.
/// </summary>
public class Timeline
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("events")]
    public List<TimelineEvent>? Events { get; set; }
}

public class TimelineEvent
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }
}
