using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Test.Models
{
    public class SuperUserBeforeInsertBusiness : IRepositoryBusinessBeforeInsert<SuperUser, string>
    {
        public int Priority => 0;
        public async Task<State<SuperUser, string>> BeforeInsertAsync(Entity<SuperUser, string> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Value!.Port == 120)
                return 100;
            return true;
        }
    }
}
