using System;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Test.Models
{
    public class CatBeforeInsertBusiness : IRepositoryBusinessBeforeInsert<Cat, Guid>
    {
        public int Priority => 0;
        public async Task<State<Cat, Guid>> BeforeInsertAsync(Entity<Cat, Guid> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Value!.Paws == 120)
                return 100;
            return true;
        }
    }
}
