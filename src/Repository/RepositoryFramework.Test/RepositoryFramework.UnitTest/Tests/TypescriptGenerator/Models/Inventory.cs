using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RepositoryFramework.UnitTest.TypescriptGenerator.Models;

/// <summary>
/// Model with various Dictionary and collection properties for testing
/// that built-in collection types map to TypeScript Record/Array
/// and never generate their own interfaces.
/// </summary>
public class Inventory
{
    [JsonPropertyName("i")]
    public string? Id { get; set; }

    /// <summary>Dictionary&lt;string, string&gt; → Record&lt;string, string&gt;</summary>
    [JsonPropertyName("t")]
    public Dictionary<string, string>? Tags { get; set; }

    /// <summary>Dictionary&lt;string, int&gt; → Record&lt;string, number&gt;</summary>
    [JsonPropertyName("q")]
    public Dictionary<string, int>? Quantities { get; set; }

    /// <summary>Dictionary&lt;string, InventoryItem&gt; → Record&lt;string, InventoryItem&gt; (value is complex)</summary>
    [JsonPropertyName("m")]
    public Dictionary<string, InventoryItem>? Items { get; set; }

    /// <summary>Dictionary&lt;int, string&gt; → Record&lt;number, string&gt;</summary>
    [JsonPropertyName("c")]
    public Dictionary<int, string>? Codes { get; set; }

    /// <summary>List&lt;string&gt; → string[] (no interface for List)</summary>
    [JsonPropertyName("l")]
    public List<string>? Labels { get; set; }

    /// <summary>IDictionary&lt;string, bool&gt; → Record&lt;string, boolean&gt;</summary>
    [JsonPropertyName("f")]
    public IDictionary<string, bool>? Flags { get; set; }

    /// <summary>IReadOnlyDictionary&lt;string, InventoryItem&gt; → Record&lt;string, InventoryItem&gt;</summary>
    [JsonPropertyName("r")]
    public IReadOnlyDictionary<string, InventoryItem>? ReadOnlyItems { get; set; }

    /// <summary>IEnumerable&lt;string&gt; → string[]</summary>
    [JsonPropertyName("e")]
    public IEnumerable<string>? Emails { get; set; }

    /// <summary>IList&lt;int&gt; → number[]</summary>
    [JsonPropertyName("x")]
    public IList<int>? Scores { get; set; }

    /// <summary>HashSet&lt;string&gt; → string[]</summary>
    [JsonPropertyName("h")]
    public HashSet<string>? UniqueNames { get; set; }

    /// <summary>IReadOnlyList&lt;InventoryItem&gt; → InventoryItem[] (discovers nested complex type)</summary>
    [JsonPropertyName("z")]
    public IReadOnlyList<InventoryItem>? ArchivedItems { get; set; }
}

public class InventoryItem
{
    [JsonPropertyName("n")]
    public string? Name { get; set; }

    [JsonPropertyName("p")]
    public decimal Price { get; set; }

    [JsonPropertyName("s")]
    public int Stock { get; set; }
}
