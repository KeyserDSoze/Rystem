using System;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Test.Models
{
    public class CalamityUniverseUserBeforeInsertBusiness2 : IRepositoryBusinessBeforeInsert<CalamityUniverseUser, string>
    {
        public int Priority => 1;
        public async Task<State<CalamityUniverseUser, string>> BeforeInsertAsync(Entity<CalamityUniverseUser, string> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Value!.Port == 120)
                throw new UnauthorizedAccessException("you don't have to stay here.");
            return true;
        }
    }
}
