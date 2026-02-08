using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RepositoryFramework.UnitTest.TypescriptGenerator.Models;

/// <summary>
/// Simulates TimelinePreview with AnyOf, enums, flags, and nested types.
/// Tests that AnyOf becomes TypeScript union and all nested types are discovered.
/// </summary>
public class TimelinePreview
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("bid")]
    public Guid BookId { get; set; }

    [JsonPropertyName("nm")]
    public string? Name { get; set; }

    [JsonPropertyName("lvl")]
    public TimelineImportanceLevel Type { get; set; }

    [JsonPropertyName("cals")]
    public SupportedCalendars Calendars { get; set; } = SupportedCalendars.Gregorian;

    [JsonPropertyName("f")]
    public AnyOf<string, long>? From { get; set; }

    [JsonPropertyName("t")]
    public AnyOf<string, long>? To { get; set; }

    [JsonPropertyName("evnt")]
    public List<TimelineEventPreview> Events { get; set; } = [];

    [JsonPropertyName("del")]
    public bool Deleted { get; set; }
}

public class TimelineEventPreview
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("bid")]
    public Guid BookId { get; set; }

    [JsonPropertyName("nm")]
    public string? Name { get; set; }

    [JsonPropertyName("lvl")]
    public TimelineImportanceLevel Type { get; set; }

    [JsonPropertyName("vis")]
    public TimelineEventVisibility Visibility { get; set; } = TimelineEventVisibility.Public;

    [JsonPropertyName("del")]
    public bool Deleted { get; set; }
}

public enum TimelineImportanceLevel
{
    Level0 = 0,
    Level1 = 1,
    Level2 = 2,
    Level3 = 3,
    Level4 = 4,
    Level5 = 5
}

[Flags]
public enum SupportedCalendars
{
    None = 0,
    Gregorian = 1 << 0,
    Julian = 1 << 1,
    Hebrew = 1 << 2,
    Hijri = 1 << 3,
    ChineseLunisolar = 1 << 4,
    Japanese = 1 << 5
}

public enum TimelineEventVisibility
{
    Public = 0,
    Private = 1,
    Hidden = 2
}

/// <summary>
/// Model with AnyOf containing complex types (not just primitives).
/// </summary>
public class SearchResult
{
    [JsonPropertyName("q")]
    public string? Query { get; set; }

    /// <summary>AnyOf&lt;TimelineEventPreview, string&gt; → TimelineEventPreview | string</summary>
    [JsonPropertyName("r")]
    public AnyOf<TimelineEventPreview, string>? Result { get; set; }

    /// <summary>AnyOf&lt;TimelineImportanceLevel, int&gt; → TimelineImportanceLevel | number</summary>
    [JsonPropertyName("p")]
    public AnyOf<TimelineImportanceLevel, int>? Priority { get; set; }
}

/// <summary>
/// Simulates a GhostWriter-like scenario: root model with a nested class
/// whose properties reference enums owned by another root model (TimelinePreview).
/// Used to test that ImportResolver collects imports from nested types' properties.
/// </summary>
public class EventReport
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("evnt")]
    public ReportedEvent? Event { get; set; }
}

public class ReportedEvent
{
    [JsonPropertyName("nm")]
    public string? Name { get; set; }

    [JsonPropertyName("vis")]
    public TimelineEventVisibility Visibility { get; set; }

    [JsonPropertyName("lvl")]
    public TimelineImportanceLevel Level { get; set; }
}

[Flags]
public enum ParagraphType
{
    None = 0,
    Descriptive = 1,
    Dialog = 2,
    Action = 4,
    Thought = 8,
    Transition = 16,
    Narration = 32,
}

public enum LocationType
{
    Main = 0,
    Supporting = 1,
    Minor = 2,
    Mentioned = 3,
    Imaginary = 4,
    Other = 255
}

/// <summary>
/// Simulates ParagraphPreview from GhostWriter — root model with direct enum property.
/// </summary>
public class ParagraphPreview
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("tp")]
    public ParagraphType Type { get; set; }

    [JsonPropertyName("del")]
    public bool Deleted { get; set; }
}

/// <summary>
/// Simulates LocationPreview from GhostWriter — root model with direct enum property.
/// </summary>
public class LocationPreview
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("tp")]
    public LocationType Type { get; set; }

    [JsonPropertyName("del")]
    public bool Deleted { get; set; }
}
