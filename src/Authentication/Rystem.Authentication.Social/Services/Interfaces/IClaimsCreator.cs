using System.Security.Claims;

namespace Rystem.Authentication.Social
{
    public interface IClaimsCreator
    {
        Task<IEnumerable<Claim>> GetClaimsAsync(string username, CancellationToken cancellationToken);
    }
}
