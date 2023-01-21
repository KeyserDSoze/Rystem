using System;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Test.Models
{
    public class CatBeforeInsertBusiness2 : IRepositoryBusinessBeforeInsert<Cat, Guid>
    {
        public int Priority => 1;
        public async Task<State<Cat, Guid>> BeforeInsertAsync(Entity<Cat, Guid> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Value!.Paws == 120)
                throw new UnauthorizedAccessException("you don't have to stay here.");
            return true;
        }
    }
}
