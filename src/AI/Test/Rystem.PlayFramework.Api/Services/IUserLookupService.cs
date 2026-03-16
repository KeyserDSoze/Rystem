using System.ComponentModel;

namespace Rystem.PlayFramework.Api.Services;

/// <summary>
/// Service for looking up user data.
/// Intentionally uses nullable Guid parameters (Guid? = null) to reproduce
/// the PlayFramework deserialization bug where the LLM sends a Guid as a
/// plain string and System.Text.Json fails to parse it.
/// </summary>
public interface IUserLookupService
{
    /// <summary>
    /// Retrieves data for a user.
    /// Pass a known userId to get that user's data, or pass null / omit the parameter to get anonymous session data.
    /// </summary>
    UserData GetUserData(
        [Description("Optional unique identifier of the user (e.g. '11111111-1111-1111-1111-111111111111'). Pass null or omit to get anonymous session data.")]
        Guid? userId = null);

    /// <summary>
    /// Retrieves the contract associated with a user.
    /// Pass a known userId / contractId to get a specific contract, or pass null / omit to get the default public contract.
    /// </summary>
    ContractInfo GetUserContract(
        [Description("Optional unique identifier of the user (e.g. '11111111-1111-1111-1111-111111111111'). Pass null or omit for the default public contract.")]
        Guid? userId = null,
        [Description("Optional unique identifier of the contract (e.g. 'aaaa0000-0000-0000-0000-000000000001'). Pass null or omit to get the most recent contract for the user.")]
        Guid? contractId = null);
}

/// <summary>User data record returned by IUserLookupService.</summary>
public sealed record UserData(
    Guid? UserId,
    string DisplayName,
    string Email,
    string Role);

/// <summary>Contract info record returned by IUserLookupService.</summary>
public sealed record ContractInfo(
    Guid? UserId,
    Guid? ContractId,
    string ContractTitle,
    DateTime StartDate,
    DateTime? EndDate);
