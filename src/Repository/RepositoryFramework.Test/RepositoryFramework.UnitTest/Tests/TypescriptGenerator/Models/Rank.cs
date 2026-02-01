using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RepositoryFramework.UnitTest.TypescriptGenerator.Models;

/// <summary>
/// Rank model - shares LeagueKey with Calendar and Team.
/// Used to test dependency resolution.
/// </summary>
public class Rank
{
    [JsonPropertyName("k")]
    public LeagueKey? Key { get; set; }

    [JsonPropertyName("t")]
    public List<TeamRanking>? Teams { get; set; }

    [JsonPropertyName("u")]
    public DateTime UpdatedAt { get; set; }
}

public class TeamRanking
{
    [JsonPropertyName("p")]
    public int Position { get; set; }

    [JsonPropertyName("n")]
    public string? TeamName { get; set; }

    [JsonPropertyName("o")]
    public string? Owner { get; set; }

    [JsonPropertyName("w")]
    public int Wins { get; set; }

    [JsonPropertyName("d")]
    public int Draws { get; set; }

    [JsonPropertyName("l")]
    public int Losses { get; set; }

    [JsonPropertyName("gf")]
    public int GoalsFor { get; set; }

    [JsonPropertyName("ga")]
    public int GoalsAgainst { get; set; }

    [JsonPropertyName("pts")]
    public int Points { get; set; }
}
