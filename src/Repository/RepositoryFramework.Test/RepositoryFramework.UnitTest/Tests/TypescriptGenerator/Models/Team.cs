using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RepositoryFramework.UnitTest.TypescriptGenerator.Models;

/// <summary>
/// Team model with nested types and enums.
/// </summary>
public class Team
{
    [JsonPropertyName("i")]
    public string? Id { get; set; }

    [JsonPropertyName("n")]
    public string? Name { get; set; }

    [JsonPropertyName("o")]
    public string? Owner { get; set; }

    [JsonPropertyName("p")]
    public List<Player>? Players { get; set; }

    [JsonPropertyName("s")]
    public TeamStatus Status { get; set; }

    [JsonPropertyName("f")]
    public Formation? CurrentFormation { get; set; }

    // Property shared with Calendar (uses same LeagueKey type)
    [JsonPropertyName("k")]
    public LeagueKey? LeagueKey { get; set; }
}

public class Player
{
    [JsonPropertyName("i")]
    public int Id { get; set; }

    [JsonPropertyName("n")]
    public string? Name { get; set; }

    [JsonPropertyName("r")]
    public PlayerRole Role { get; set; }

    [JsonPropertyName("v")]
    public decimal Value { get; set; }

    [JsonPropertyName("t")]
    public string? RealTeam { get; set; }
}

public enum PlayerRole
{
    Goalkeeper = 1,
    Defender = 2,
    Midfielder = 3,
    Forward = 4
}

public enum TeamStatus
{
    Active = 0,
    Inactive = 1,
    Suspended = 2
}

public class Formation
{
    [JsonPropertyName("n")]
    public string? Name { get; set; }

    [JsonPropertyName("d")]
    public int Defenders { get; set; }

    [JsonPropertyName("m")]
    public int Midfielders { get; set; }

    [JsonPropertyName("f")]
    public int Forwards { get; set; }
}
