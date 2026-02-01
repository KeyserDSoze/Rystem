using System.Text.Json.Serialization;

namespace RepositoryFramework.UnitTest.TypescriptGenerator.Models;

/// <summary>
/// League key for Calendar repository.
/// </summary>
public record LeagueKey
{
    [JsonPropertyName("g")]
    public string? Group { get; init; }

    [JsonPropertyName("l")]
    public string? League { get; init; }

    [JsonPropertyName("y")]
    public int Year { get; init; }

    public LeagueKey() { }

    public LeagueKey(string group, string league, int year)
    {
        Group = group;
        League = league;
        Year = year;
    }
}
