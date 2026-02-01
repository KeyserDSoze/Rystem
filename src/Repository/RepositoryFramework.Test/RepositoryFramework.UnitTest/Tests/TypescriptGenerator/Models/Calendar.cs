using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RepositoryFramework.UnitTest.TypescriptGenerator.Models;

/// <summary>
/// Calendar model with JsonPropertyName attributes for testing TypeScript generation.
/// Simulates a real-world model with abbreviated JSON property names.
/// </summary>
public class Calendar
{
    [JsonPropertyName("y")]
    public int Year { get; set; }

    [JsonPropertyName("r")]
    public Dictionary<string, CalendarDay[]>? Rounds { get; set; }

    [JsonPropertyName("n")]
    public string? Name { get; set; }

    public bool IsActive { get; set; } // No JsonPropertyName - should use "IsActive"
}

public class CalendarDay
{
    [JsonPropertyName("a")]
    public int SerieADay { get; set; }

    [JsonPropertyName("n")]
    public int Number { get; set; }

    [JsonPropertyName("g")]
    public List<CalendarGame>? Games { get; set; }
}

public class CalendarGame
{
    [JsonPropertyName("i")]
    public string? Id { get; set; }

    [JsonPropertyName("n")]
    public int Number { get; set; }

    [JsonPropertyName("h")]
    public string? Home { get; set; }

    [JsonPropertyName("a")]
    public string? Away { get; set; }

    [JsonPropertyName("r")]
    public GameResult? Result { get; set; }
}

public class GameResult
{
    [JsonPropertyName("h")]
    public Point? HomePoints { get; set; }

    [JsonPropertyName("a")]
    public Point? AwayPoints { get; set; }

    [JsonPropertyName("i")]
    public bool IsCancelled { get; set; }

    [JsonPropertyName("g")]
    public int HomeGoals { get; set; }

    [JsonPropertyName("l")]
    public int AwayGoals { get; set; }
}

public class Point
{
    [JsonPropertyName("v")]
    public decimal Value { get; set; }

    [JsonPropertyName("d")]
    public bool DefensiveBonus { get; set; }

    [JsonPropertyName("g")]
    public bool GoodPeople { get; set; }

    [JsonPropertyName("o")]
    public bool OwnGoal { get; set; }
}
