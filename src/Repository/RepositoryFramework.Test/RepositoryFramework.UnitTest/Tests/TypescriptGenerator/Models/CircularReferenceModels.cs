using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RepositoryFramework.UnitTest.TypescriptGenerator.Models;

/// <summary>
/// Test model with circular reference through generic type.
/// This tests the fix for stack overflow when Book references EntityVersions<Book>.
/// </summary>
public class Book
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    // Circular reference: Book -> EntityVersions<Book>
    [JsonPropertyName("history")]
    public EntityVersions<Book>? History { get; set; }
}

/// <summary>
/// Another model with circular self-reference.
/// </summary>
public class TreeNode
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    // Direct self-reference
    [JsonPropertyName("children")]
    public List<TreeNode>? Children { get; set; }

    // Indirect self-reference through parent
    [JsonPropertyName("parent")]
    public TreeNode? Parent { get; set; }
}

/// <summary>
/// Mutual circular reference: A -> B -> A
/// </summary>
public class PersonA
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("friend")]
    public PersonB? Friend { get; set; }
}

public class PersonB
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("friend")]
    public PersonA? Friend { get; set; }
}
