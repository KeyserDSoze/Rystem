namespace Rystem.PlayFramework.Api.Services;

/// <summary>
/// In-memory stub implementation of IUserLookupService used for testing the
/// nullable-Guid deserialization path in PlayFramework ServiceMethodTool.
/// </summary>
public sealed class UserLookupService : IUserLookupService
{
    // Fake users indexed by Guid
    private static readonly Dictionary<Guid, UserData> s_users = new()
    {
        [Guid.Parse("11111111-1111-1111-1111-111111111111")] = new UserData(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Alice Rossi",
            "alice@example.com",
            "Admin"),

        [Guid.Parse("22222222-2222-2222-2222-222222222222")] = new UserData(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Bob Bianchi",
            "bob@example.com",
            "User"),
    };

    // Fake contracts indexed by (userId, contractId)
    private static readonly Dictionary<Guid, ContractInfo> s_contracts = new()
    {
        [Guid.Parse("aaaa0000-0000-0000-0000-000000000001")] = new ContractInfo(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("aaaa0000-0000-0000-0000-000000000001"),
            "Premium Support Agreement",
            new DateTime(2025, 1, 1),
            new DateTime(2026, 12, 31)),

        [Guid.Parse("bbbb0000-0000-0000-0000-000000000002")] = new ContractInfo(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("bbbb0000-0000-0000-0000-000000000002"),
            "Standard License",
            new DateTime(2024, 6, 1),
            null),
    };

    public UserData GetUserData(Guid? userId = null)
    {
        if (userId.HasValue && s_users.TryGetValue(userId.Value, out var user))
            return user;

        // Anonymous / not found → return generic session data
        return new UserData(null, "Anonymous", "guest@example.com", "Guest");
    }

    public ContractInfo GetUserContract(Guid? userId = null, Guid? contractId = null)
    {
        // If a specific contractId is supplied, look that up directly
        if (contractId.HasValue && s_contracts.TryGetValue(contractId.Value, out var contract))
            return contract;

        // Otherwise find the first contract for the given user
        if (userId.HasValue)
        {
            var byUser = s_contracts.Values.FirstOrDefault(c => c.UserId == userId);
            if (byUser is not null)
                return byUser;
        }

        // Default public contract
        return new ContractInfo(null, null, "Public Free Tier", new DateTime(2024, 1, 1), null);
    }
}
