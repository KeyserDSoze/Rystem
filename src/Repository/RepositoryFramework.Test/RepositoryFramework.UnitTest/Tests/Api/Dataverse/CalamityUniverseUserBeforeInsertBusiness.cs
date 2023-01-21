using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Test.Models
{
    public class CalamityUniverseUserBeforeInsertBusiness : IRepositoryBusinessBeforeInsert<CalamityUniverseUser, string>
    {
        public int Priority => 0;
        public async Task<State<CalamityUniverseUser, string>> BeforeInsertAsync(Entity<CalamityUniverseUser, string> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Value!.Port == 120)
                return 100;
            return true;
        }
    }
}
