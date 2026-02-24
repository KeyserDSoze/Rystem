using System.ComponentModel;

namespace Rystem.PlayFramework.Api.Models;

/// <summary>
/// Arguments model for the saveToLocalStorage command.
/// </summary>
public sealed class SaveDataArgs
{
    /// <summary>
    /// The storage key.
    /// </summary>
    [Description("The key to store data under in localStorage")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The value to save.
    /// </summary>
    [Description("The value to save (will be stored as string)")]
    public string Value { get; set; } = string.Empty;
}
