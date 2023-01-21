using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Test.Models
{
    public class IperRepositoryBeforeInsertBusiness : IRepositoryBusinessBeforeInsert<IperUser, string>
    {
        public int Priority => 0;
        public async Task<State<IperUser, string>> BeforeInsertAsync(Entity<IperUser, string> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Value!.Port == 120)
                return new State<IperUser, string>(false, default, default, 100);
            return true;
        }
    }
}
